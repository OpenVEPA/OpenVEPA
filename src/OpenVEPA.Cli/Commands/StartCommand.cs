using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using OpenVEPA.Cli.Infrastructure;
using OpenVEPA.Cli.Setup;
using OpenVEPA.Scheduler;
using OpenVEPA.Server;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Default command that starts the OpenVEPA server with Kestrel, SignalR, and Hangfire.
/// </summary>
internal sealed class StartCommand : AsyncCommand
{
    private readonly WebApplicationHolder _holder;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartCommand"/> class.
    /// </summary>
    /// <param name="holder">Holder containing the configured web application.</param>
    public StartCommand(WebApplicationHolder holder)
    {
        _holder = holder ?? throw new ArgumentNullException(nameof(holder));
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var app = _holder.App;
        app.MapOpenVepaEndpoints();
        app.MapSchedulerEndpoints();

        // Serve the setup wizard HTML page from the Cli assembly.
        app.MapGet("/init/setupwizard", () =>
            Results.Content(SetupPageHtml.Content, "text/html"));

        AnsiConsole.Write(new FigletText("OpenVEPA").Color(Color.Green));
        AnsiConsole.MarkupLine("[bold green]Server started.[/] Press [yellow]Ctrl+C[/] to stop.");

        await app.RunAsync().ConfigureAwait(false);
        return 0;
    }
}
