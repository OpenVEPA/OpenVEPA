using OpenVEPA.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Uninstalls the OpenVEPA system service from the current platform.
/// </summary>
internal sealed class UninstallServiceCommand : AsyncCommand
{
    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        if (!PlatformServiceInstaller.IsInstalled())
        {
            AnsiConsole.MarkupLine("[dim]OpenVEPA service is not installed.[/]");
            return 0;
        }

        if (!AnsiConsole.Confirm("Are you sure you want to uninstall the OpenVEPA service?"))
        {
            return 0;
        }

        var result = await PlatformServiceInstaller.UninstallAsync(CancellationToken.None)
            .ConfigureAwait(false);

        WritePanel(result.Message, result.Success);
        return result.Success ? 0 : 1;
    }

    /// <summary>
    /// Writes a success or error panel to the console.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="success">Whether to render as a success or error panel.</param>
    private static void WritePanel(string message, bool success)
    {
        var header = success ? "[bold green]Success[/]" : "[bold red]Error[/]";
        var color = success ? Color.Green : Color.Red;

        AnsiConsole.Write(new Panel(Markup.Escape(message))
            .Header(header)
            .Border(BoxBorder.Rounded)
            .BorderColor(color));
    }
}
