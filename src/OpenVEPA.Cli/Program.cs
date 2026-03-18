using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
var homeConfigPath = Path.Combine(openvepaHome, "appsettings.json");
builder.Configuration.AddJsonFile(homeConfigPath, optional: true, reloadOnChange: true);

builder.Host.UseSerilog((_, configuration) => configuration
    .MinimumLevel.Information()
    .WriteTo.Console());

builder.Services.AddSingleton<WebApplicationHolder>();
builder.Services.AddOpenVepaStorage(connectionString, documentsPath);
builder.Services.AddOpenVepaProviders(builder.Configuration);
builder.Services.AddOpenVepaSkills(builder.Configuration);
builder.Services.AddOpenVepaAgents(builder.Configuration);
builder.Services.AddOpenVepaServer(builder.Configuration, openvepaHome);
builder.Services.AddOpenVepaScheduler(builder.Configuration);

var app = builder.Build();

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
