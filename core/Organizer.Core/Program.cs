using Microsoft.EntityFrameworkCore;
using Organizer.Core;

var builder = WebApplication.CreateBuilder(args);

var configPath = CoreConfigLoader.ResolvePath(builder.Configuration["config"]);
var configCreated = CoreConfigLoader.EnsureFile(configPath);

// Layers: appsettings.json (defaults) < core.json (user overrides) < environment < command line.
builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

var config = CoreConfigLoader.Finalize(builder.Configuration.Get<CoreConfig>() ?? new CoreConfig(), configPath);
builder.Services.AddSingleton(config);

if (builder.Configuration["urls"] is null)
{
    builder.WebHost.UseUrls($"http://{config.Address}:{config.Port}");
}
builder.Services.AddDbContext<OrganizerDb>(o => 
    o.UseSqlite($"Data Source={config.DatabasePath}").AddInterceptors(new UnicodeFunctions()));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevApiDocs();
}

var app = builder.Build();

if (configCreated)
{
    app.Logger.LogInformation("Created config file with a new secret: {ConfigPath}", configPath);
}
else
{
    app.Logger.LogInformation("Using config file: {ConfigPath}", configPath);
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(config.DatabasePath))!);
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<OrganizerDb>().Database.Migrate();
}

app.UseWhen(
    context => !(app.Environment.IsDevelopment() && DevApiDocs.IsDocsPath(context.Request.Path)),
    branch => branch.UseTokenAuth(config.Secret));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapItemEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapDevApiDocs(config.Secret);
}

app.Run();

public partial class Program;
