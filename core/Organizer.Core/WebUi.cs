namespace Organizer.Core;

/// <summary>
/// Serves the built web UI from the web root (<c>wwwroot</c> in the content root, or <c>--webroot</c>) on the
/// same origin as the API. Static files are public; only <c>/api/*</c> is protected.
/// </summary>
public static class WebUi
{
    private const string Index = "/index.html";
    private const string Immutable = "/_app/immutable";

    /// <summary>
    /// Adds static files and the SPA fallback, but only if the web root folder exists at startup;
    /// otherwise the Core stays API only and adds nothing.
    /// </summary>
    public static void UseWebUi(this WebApplication app)
    {
        var root = app.Environment.WebRootPath;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            return;
        }

        var files = app.Environment.WebRootFileProvider;

        // SPA fallback: rewrite a page-like GET/HEAD to index.html and let the static files serve it, so it
        // gets the ETag, HEAD handling and cache headers of any other file. A missing index.html means 404.
        app.Use((context, next) =>
        {
            var request = context.Request;
            if ((HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
                && IsPageLike(request.Path, app.Environment.IsDevelopment())
                && !files.GetFileInfo(request.Path.Value!).Exists)
            {
                request.Path = Index;
            }
            return next(context);
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                context.Context.Response.Headers.CacheControl =
                    context.Context.Request.Path.StartsWithSegments(Immutable)
                        ? "public, max-age=31536000, immutable"
                        : "no-cache";
            },
        });
    }

    // Not under /api (case-insensitive, /api itself included), not a Development docs page, and the last
    // segment has no extension: a missing file such as /favicon.ico must stay a 404.
    private static bool IsPageLike(PathString path, bool development)
    {
        if (path.StartsWithSegments("/api") || (development && DevApiDocs.IsDocsPath(path)))
        {
            return false;
        }
        var value = path.Value ?? "/";
        var lastSegment = value[(value.LastIndexOf('/') + 1)..];
        return !Path.HasExtension(lastSegment);
    }
}
