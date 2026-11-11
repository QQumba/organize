using Microsoft.EntityFrameworkCore;
using Organizer.Core;

var builder = WebApplication.CreateBuilder(args);

var configPath = CoreConfigLoader.ResolvePath(builder.Configuration["config"]);

// The Core runs hidden (no console), so everything it logs also goes to logs/core-*.log beside core.json.
using var logWriter = new LogFileWriter(Path.Combine(Path.GetDirectoryName(configPath)!, "logs"));
builder.Logging.AddProvider(new FileLoggerProvider(logWriter));

try
{
    return Run();
}
catch (Exception ex) when (ex is not HostAbortedException) // HostAbortedException: EF tooling and test hosts stop Main on purpose
{
    // Failures before the host exists (bad core.json, missing Secret) have no logger yet; the host's own
    // logger may be gone too, so write the reason straight to the file.
    logWriter.Write(FileLoggerProvider.Format("Crit", "Program", "The Core failed to start or crashed.", ex));
    return 1;
}

int Run()
{
    var configCreated = CoreConfigLoader.EnsureFile(configPath);

    // Layers: appsettings.json (defaults) < core.json (user overrides) < environment < command line.
    builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddCommandLine(args);

    var config = CoreConfigLoader.Finalize(builder.Configuration.Get<CoreConfig>() ?? new CoreConfig(), configPath);
    builder.Services.AddSingleton(config);
    var session = new SessionAuth(config);
    builder.Services.AddSingleton(session);

    if (builder.Configuration["urls"] is null)
    {
        builder.WebHost.UseUrls($"http://{config.Address}:{config.Port}");
    }
    builder.Services.AddDbContext<OrganizerDb>(o => 
        o.UseSqlite($"Data Source={config.DatabasePath}").AddInterceptors(new UnicodeFunctions()));
    builder.Services.AddApiDescription();

    var app = builder.Build();
    
    app.Logger.LogInformation("Using sqlite database file: {DbLocation}", config.DatabasePath);

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
        context => context.Request.Path.StartsWithSegments("/api")
            && !(app.Environment.IsDevelopment() && DevApiDocs.IsDocsPath(context.Request.Path)),
        branch => branch.UseTokenAuth(session));
    var api = app.MapGroup("/api");
    api.MapSessionEndpoints();
    api.MapGet("/health", () => Results.Ok(new HealthDto("ok")))
        .Produces<HealthDto>();
    api.MapItemEndpoints();
    app.MapApiDescription();
    if (app.Environment.IsDevelopment())
    {
        app.MapDevScalar(config.Secret);
    }

    app.Run();
    return 0;
}

public partial class Program;
