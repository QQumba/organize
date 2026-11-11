using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Organizer.Core;

/// <summary>
/// The OpenAPI description (served in every environment, behind token auth) and the Scalar UI
/// (Development only). The description is checked in as docs/openapi.json; the Core tests keep it current.
/// </summary>
public static class DevApiDocs
{
    private const string BearerScheme = "Bearer";
    private const string OpenApiRoute = "/api/openapi/{documentName}.json";
    private const string EditSchema = "EditItemRequest";

    // Hand-written: every field optional, and null is allowed (it clears the field).
    private static OpenApiSchema EditItemSchema()
    {
        static OpenApiSchema Nullable(JsonSchemaType type, string? format = null) =>
            new() { Type = type | JsonSchemaType.Null, Format = format };
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["text"] = Nullable(JsonSchemaType.String),
                ["notes"] = Nullable(JsonSchemaType.String),
                ["category"] = Nullable(JsonSchemaType.String),
                ["tags"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array | JsonSchemaType.Null,
                    Items = new OpenApiSchema { Type = JsonSchemaType.String },
                },
                ["dueDate"] = Nullable(JsonSchemaType.String, "date"),
                ["important"] = Nullable(JsonSchemaType.Boolean),
                ["urgent"] = Nullable(JsonSchemaType.Boolean),
            },
        };
    }

    public static void AddApiDescription(this IServiceCollection services) =>
        services.AddOpenApi(o =>
        {
            o.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] =
                    new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" };
                document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [] }];
                document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
                document.Components.Schemas[EditSchema] = EditItemSchema();
                document.Servers = null; // the generator adds the request host, which would make the file vary
                return Task.CompletedTask;
            });
            o.AddOperationTransformer(DescribeOperation);
        });

    // Documentation that endpoint metadata cannot express.
    private static Task DescribeOperation(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken _)
    {
        var path = context.Description.RelativePath;
        var parameters = operation.Parameters ?? [];
        void Describe(string name, string text, bool required = false)
        {
            if (parameters.FirstOrDefault(p => p.Name == name) is OpenApiParameter p)
            {
                p.Description = text;
                p.Required = required;
            }
        }

        switch (path)
        {
            case "api/inbox":
                Describe("category", "Only Items in this Category.");
                Describe("tag", "Only Items with this Tag. Repeat the parameter to require several Tags.");
                break;
            case "api/items/{id:int}" when operation.RequestBody?.Content is { } content:
                // The handler binds a JsonObject (opaque schema); describe the editable fields instead.
                foreach (var media in content.Values)
                {
                    media.Schema = new OpenApiSchemaReference(EditSchema);
                }
                break;
            case "api/search":
                Describe("q", "Text to find in Item text and notes (case-insensitive, Unicode-aware). Required, not blank.", required: true);
                break;
        }
        return Task.CompletedTask;
    }

    /// <summary>True for the documentation routes, which skip token auth in Development (the API itself still requires it).</summary>
    public static bool IsDocsPath(PathString path) =>
        path.StartsWithSegments("/api/openapi") || path.StartsWithSegments("/scalar");

    public static void MapApiDescription(this WebApplication app) => app.MapOpenApi(OpenApiRoute);

    public static void MapDevScalar(this WebApplication app, string secret) =>
        // Dev secret is throwaway, so prefill it: "Try it" works without pasting the token.
        app.MapScalarApiReference(o => o
            .WithTitle("Organizer Core")
            .WithOpenApiRoutePattern(OpenApiRoute)
            .AddPreferredSecuritySchemes(BearerScheme)
            .AddHttpAuthentication(BearerScheme, auth => auth.Token = secret));
}
