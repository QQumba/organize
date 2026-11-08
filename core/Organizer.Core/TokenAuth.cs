using System.Security.Cryptography;
using System.Text;

namespace Organizer.Core;

public static class TokenAuth
{
    /// <summary>Rejects with 401 any request without `Authorization: Bearer secret`.</summary>
    public static IApplicationBuilder UseTokenAuth(this IApplicationBuilder app, string secret)
    {
        var expected = Encoding.UTF8.GetBytes(secret);
        return app.Use(async (context, next) =>
        {
            if (!IsValid(context.Request.Headers.Authorization.ToString(), expected))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Bearer";
                return;
            }
            await next(context);
        });
    }

    private static bool IsValid(string header, byte[] expected)
    {
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        var given = Encoding.UTF8.GetBytes(header[prefix.Length..].Trim());
        return CryptographicOperations.FixedTimeEquals(given, expected);
    }
}
