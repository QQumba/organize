using Microsoft.EntityFrameworkCore;
using Organizer.Core;

var builder = WebApplication.CreateBuilder(args);

var configPath = CoreConfigLoader.ResolvePath(builder.Configuration["config"]);
var (config, configCreated) = CoreConfigLoader.LoadOrCreate(configPath);

if (builder.Configuration["urls"] is null)
{
    builder.WebHost.UseUrls($"http://{config.Address}:{config.Port}");
}
builder.Services.AddDbContext<OrganizerDb>(o => o.UseSqlite($"Data Source={config.DatabasePath}"));

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

app.UseTokenAuth(config.Secret);
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
