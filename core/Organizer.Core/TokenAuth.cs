using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Organizer.Core;

/// <summary>
/// What a request must carry to be authenticated: the secret (as `Bearer`) or the session cookie derived from it.
/// The cookie value is an HMAC of a fixed label keyed with the secret, so there is no session store and
/// changing the secret invalidates every cookie (ADR 0003).
/// </summary>
public sealed class SessionAuth
{
    private const string Label = "organizer-session-v1";

    private readonly byte[] _secret;
    private readonly byte[] _cookieValue;

    public SessionAuth(CoreConfig config)
    {
        _secret = Encoding.UTF8.GetBytes(config.Secret);
        CookieName = $"organizer_session_{config.Port}";
        _cookieValue = Encoding.ASCII.GetBytes(
            WebEncoders.Base64UrlEncode(HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(Label))));
    }

    public string CookieName { get; }

    public string CookieValue => Encoding.ASCII.GetString(_cookieValue);

    public bool SecretMatches(string? given) =>
        given is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), _secret);

    public bool HasValidBearer(HttpRequest request)
    {
        const string prefix = "Bearer ";
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && SecretMatches(header[prefix.Length..].Trim());
    }

    public bool HasValidCookie(HttpRequest request) =>
        request.Cookies.TryGetValue(CookieName, out var value)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(value), _cookieValue);
}

public static class TokenAuth
{
    /// <summary>
    /// Rejects with 401 any request without a valid `Authorization: Bearer secret` or session cookie.
    /// POST and DELETE on /api/session are open (login and logout). A write authenticated by the cookie
    /// alone must come from the Core's own origin (403 otherwise); bearer requests are never checked.
    /// </summary>
    public static IApplicationBuilder UseTokenAuth(this IApplicationBuilder app, SessionAuth auth) =>
        app.Use(async (context, next) =>
        {
            var request = context.Request;
            if (IsLoginOrLogout(request) || auth.HasValidBearer(request))
            {
                await next(context);
                return;
            }
            if (!auth.HasValidCookie(request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Bearer";
                return;
            }
            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !OriginAllowed(request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await next(context);
        });

    private static bool IsLoginOrLogout(HttpRequest request) =>
        (HttpMethods.IsPost(request.Method) || HttpMethods.IsDelete(request.Method))
        && request.Path.Equals("/api/session", StringComparison.OrdinalIgnoreCase);

    /// <summary>No Origin header passes. Otherwise it must be an absolute URL with the request's scheme, host and port.</summary>
    private static bool OriginAllowed(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Origin", out var values) || values.Count == 0)
        {
            return true;
        }
        if (values.Count != 1 || !Uri.TryCreate(values[0], UriKind.Absolute, out var origin))
        {
            return false;
        }
        var defaultPort = request.IsHttps ? 443 : 80;
        return string.Equals(origin.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(origin.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase)
            && origin.Port == (request.Host.Port ?? defaultPort);
    }
}
