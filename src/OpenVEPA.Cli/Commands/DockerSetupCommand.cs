using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Deprecated: the setup wizard is now integrated into the main server.
/// This command prints a deprecation notice and returns success.
/// </summary>
internal sealed class DockerSetupCommand : AsyncCommand<DockerSetupCommand.Settings>
{
    /// <inheritdoc />
    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine(
            "[yellow]The 'docker-setup' command is deprecated.[/]");
        AnsiConsole.MarkupLine(
            "[dim]The setup wizard is now integrated into the main server.[/]");
        AnsiConsole.MarkupLine(
            $"[dim]Run[/] [bold]openvepa start[/] [dim]and open[/] [bold]http://localhost:{settings.Port}/init/setupwizard[/]");

        return Task.FromResult(0);
    }

    /// <summary>
    /// Settings for the docker-setup command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the port to bind the setup wizard to.
        /// </summary>
        [CommandOption("--port")]
        [Description("Port to bind the setup wizard to")]
        [DefaultValue(8371)]
        public int Port { get; init; } = 8371;
    }
}
