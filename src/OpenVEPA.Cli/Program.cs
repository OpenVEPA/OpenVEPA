using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenVEPA.Agents.Runtime;
using OpenVEPA.Cli.Commands;
using OpenVEPA.Cli.Infrastructure;
using OpenVEPA.Providers;
using OpenVEPA.Scheduler;
using OpenVEPA.Server;
using OpenVEPA.Skills.Runtime;
using OpenVEPA.Storage;
using Serilog;
using Spectre.Console.Cli;

var openvepaHome = Environment.GetEnvironmentVariable("OPENVEPA_HOME")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".openvepa");

Directory.CreateDirectory(Path.Combine(openvepaHome, "data"));

var connectionString = $"Data Source={Path.Combine(openvepaHome, "data", "openvepa.db")}";
var documentsPath = Path.Combine(openvepaHome, "data", "documents");

var builder = WebApplication.CreateBuilder(args);

// Load persisted configuration from the OpenVEPA home directory (survives container rebuilds).
var homeConfigPath = Path.Combine(openvepaHome, "openvepa.conf");
builder.Configuration.AddJsonFile(homeConfigPath, optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, configuration) =>
{
    var logLevel = Environment.GetEnvironmentVariable("OPENVEPA_LOG_LEVEL");
    var minimumLevel = logLevel?.ToLowerInvariant() switch
    {
        "debug" => Serilog.Events.LogEventLevel.Debug,
        "verbose" or "trace" => Serilog.Events.LogEventLevel.Verbose,
        "warning" or "warn" => Serilog.Events.LogEventLevel.Warning,
        "error" => Serilog.Events.LogEventLevel.Error,
        _ => Serilog.Events.LogEventLevel.Information
    };

    var homeDir = Environment.GetEnvironmentVariable("OPENVEPA_HOME")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".openvepa");
    var logDir = Path.Combine(homeDir, "logs");
    Directory.CreateDirectory(logDir);

    configuration
        .MinimumLevel.Is(minimumLevel)
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            Path.Combine(logDir, "openvepa-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
            shared: true);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddSingleton<WebApplicationHolder>();
builder.Services.AddOpenVepaStorage(connectionString, documentsPath);
builder.Services.AddOpenVepaProviders(builder.Configuration);
builder.Services.AddOpenVepaSkills(builder.Configuration);
builder.Services.AddOpenVepaAgents(builder.Configuration);
builder.Services.AddOpenVepaServer(builder.Configuration, openvepaHome);
builder.Services.AddOpenVepaScheduler(builder.Configuration);

var app = builder.Build();

// Log provider configuration state at startup for diagnostics
using (var scope = app.Services.CreateScope())
{
    var providerOptions = scope.ServiceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("OpenVEPA.Startup");
    startupLogger.LogInformation("Provider config loaded: DefaultProvider={DefaultProvider}, Instances={InstanceCount}",
        providerOptions.DefaultProvider, providerOptions.Instances?.Count ?? 0);
    if (providerOptions.Instances is { Count: > 0 })
    {
        foreach (var instance in providerOptions.Instances)
        {
            startupLogger.LogInformation(
                "  Provider instance: name={Name}, type={Type}, model={Model}, endpoint={Endpoint}, hasApiKey={HasApiKey}",
                instance.Name, instance.Type, instance.DefaultModel ?? "(none)",
                instance.Endpoint ?? "(default)", !string.IsNullOrWhiteSpace(instance.ApiKey));
        }
    }
    else
    {
        startupLogger.LogWarning("No provider instances configured. Chat will fail until providers are added.");
    }
}

app.Services.GetRequiredService<WebApplicationHolder>().App = app;

await app.Services.InitializeStorageAsync().ConfigureAwait(false);

var registrar = new TypeRegistrar(app.Services);
var commandApp = new CommandApp<StartCommand>(registrar);

commandApp.Configure(config =>
{
    config.SetApplicationName("openvepa");

    config.AddCommand<StartCommand>("start")
        .WithDescription("Start the OpenVEPA server");

    config.AddCommand<ChatCommand>("chat")
        .WithDescription("Start an interactive chat session");

    config.AddCommand<StatusCommand>("status")
        .WithDescription("Show system status");

    config.AddBranch("skill", skill =>
    {
        skill.SetDescription("Manage skills");
        skill.AddCommand<SkillListCommand>("list")
            .WithDescription("List loaded skills");
    });

    config.AddBranch("profile", profile =>
    {
        profile.SetDescription("Manage user profile preferences");
        profile.AddCommand<ProfileShowCommand>("show")
            .WithDescription("Show all preferences");
        profile.AddCommand<ProfileSetCommand>("set")
            .WithDescription("Set a preference");
        profile.AddCommand<ProfileDeleteCommand>("delete")
            .WithDescription("Delete a preference");
    });

    config.AddBranch("token", token =>
    {
        token.SetDescription("Manage API tokens");
        token.AddCommand<TokenCreateCommand>("create")
            .WithDescription("Create a new API token");
        token.AddCommand<TokenListCommand>("list")
            .WithDescription("List all tokens");
        token.AddCommand<TokenRevokeCommand>("revoke")
            .WithDescription("Revoke a token");
    });

    config.AddBranch("schedule", schedule =>
    {
        schedule.SetDescription("Manage scheduled tasks");
        schedule.AddCommand<ScheduleListCommand>("list")
            .WithDescription("List scheduled tasks");
    });

    config.AddCommand<SetupCommand>("setup")
        .WithDescription("Run the initial setup wizard");

    config.AddCommand<DockerSetupCommand>("docker-setup")
        .WithDescription("Run setup wizard for Docker (binds to all interfaces)");

    config.AddBranch("service", service =>
    {
        service.SetDescription("Manage OpenVEPA as a system service");
        service.AddCommand<InstallServiceCommand>("install")
            .WithDescription("Install OpenVEPA as a system service");
        service.AddCommand<UninstallServiceCommand>("uninstall")
            .WithDescription("Uninstall the OpenVEPA system service");
        service.AddCommand<ServiceStatusCommand>("status")
            .WithDescription("Show service installation status");
    });
});

return await commandApp.RunAsync(args).ConfigureAwait(false);
