using OpenVEPA.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Displays the current installation status of the OpenVEPA system service.
/// </summary>
internal sealed class ServiceStatusCommand : AsyncCommand
{
    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var platform = PlatformServiceInstaller.GetPlatformName();
        var installed = PlatformServiceInstaller.IsInstalled();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Service Status[/]")
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Platform", Markup.Escape(platform));
        table.AddRow("Installed", installed ? "[green]Yes[/]" : "[red]No[/]");

        if (installed)
        {
            var status = await PlatformServiceInstaller.GetStatusAsync(CancellationToken.None)
                .ConfigureAwait(false);
            table.AddRow("State", Markup.Escape(status.Message));
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
