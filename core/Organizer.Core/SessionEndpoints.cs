namespace Organizer.Core;

public sealed record SessionRequest(string? Secret);

public static class SessionEndpoints
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    private static CookieOptions Options(HttpRequest request) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api",
        Secure = request.IsHttps,
    };

    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/session", (SessionRequest body, HttpContext context, SessionAuth auth) =>
            {
                if (string.IsNullOrWhiteSpace(body.Secret))
                {
                    return Results.Problem(title: "Invalid request", detail: "secret is required.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                if (!auth.SecretMatches(body.Secret))
                {
                    return Results.Problem(title: "Wrong secret", statusCode: StatusCodes.Status401Unauthorized);
                }
                var options = Options(context.Request);
                options.MaxAge = Lifetime;
                options.Expires = DateTimeOffset.UtcNow.Add(Lifetime);
                context.Response.Cookies.Append(auth.CookieName, auth.CookieValue, options);
                return Results.NoContent();
            })
            .WithSummary("Log in: a right secret sets the session cookie.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapDelete("/session", (HttpContext context, SessionAuth auth) =>
            {
                context.Response.Cookies.Delete(auth.CookieName, Options(context.Request));
                return Results.NoContent();
            })
            .WithSummary("Log out: expires the session cookie.")
            .Produces(StatusCodes.Status204NoContent);

        app.MapGet("/session", () => Results.NoContent())
            .WithSummary("204 when the request carries a valid session cookie or bearer token, 401 otherwise.")
            .Produces(StatusCodes.Status204NoContent);
    }
}
