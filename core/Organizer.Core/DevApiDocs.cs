using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Organizer.Core;

/// <summary>OpenAPI description and Scalar UI, mapped in the Development environment only.</summary>
public static class DevApiDocs
{
    private const string BearerScheme = "Bearer";

    public static void AddDevApiDocs(this IServiceCollection services) =>
        services.AddOpenApi(o => o.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[BearerScheme] =
                new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" };
            document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [] }];
            return Task.CompletedTask;
        }));

    /// <summary>True for the documentation routes, which skip token auth (the API itself still requires it).</summary>
    public static bool IsDocsPath(PathString path) =>
        path.StartsWithSegments("/openapi") || path.StartsWithSegments("/scalar");

    public static void MapDevApiDocs(this WebApplication app, string secret)
    {
        app.MapOpenApi();
        // Dev secret is throwaway, so prefill it: "Try it" works without pasting the token.
        app.MapScalarApiReference(o => o
            .WithTitle("Organizer Core")
            .AddPreferredSecuritySchemes(BearerScheme)
            .AddHttpAuthentication(BearerScheme, auth => auth.Token = secret));
    }
}
