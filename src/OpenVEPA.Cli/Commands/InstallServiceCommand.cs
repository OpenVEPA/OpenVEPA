using System.Diagnostics;
using OpenVEPA.Cli.Services;
using OpenVEPA.Cli.Setup;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Installs OpenVEPA as a platform-native system service,
/// optionally running the setup wizard first.
/// </summary>
internal sealed class InstallServiceCommand : AsyncCommand
{
    private const string TuiChoice = "Terminal (TUI) - Interactive terminal wizard";
    private const string WebUiChoice = "Browser (WebUI) - Setup in your web browser";
    private const string SkipChoice = "Skip setup - Use default configuration";

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        if (PlatformServiceInstaller.IsInstalled())
        {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] OpenVEPA service is already installed.");

            if (!AnsiConsole.Confirm("Do you want to reinstall?"))
            {
                return 0;
            }
        }

        AnsiConsole.MarkupLine(
            $"[bold]Detected platform:[/] {Markup.Escape(PlatformServiceInstaller.GetPlatformName())}");
        AnsiConsole.WriteLine();

        var config = await PromptAndRunSetupAsync().ConfigureAwait(false);

        if (config is null)
        {
            AnsiConsole.MarkupLine("[dim]Setup cancelled.[/]");
            return 1;
        }

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None)
            .ConfigureAwait(false);

        var executablePath = ResolveExecutablePath();

        if (string.IsNullOrEmpty(executablePath))
        {
            WritePanel("Could not determine the executable path.", success: false);
            return 1;
        }

        var result = await PlatformServiceInstaller.InstallAsync(
            executablePath, config.Port, CancellationToken.None).ConfigureAwait(false);

        WritePanel(result.Message, result.Success);
        return result.Success ? 0 : 1;
    }

    /// <summary>
    /// Prompts the user for a setup mode and runs the corresponding wizard.
    /// </summary>
    /// <returns>The configuration, or <c>null</c> if the user cancelled.</returns>
    private static async Task<SetupConfiguration?> PromptAndRunSetupAsync()
    {
        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How would you like to be guided through the initial setup?")
                .AddChoices(TuiChoice, WebUiChoice, SkipChoice));

        if (mode == TuiChoice)
        {
            return SetupWizardTui.Run();
        }

        if (mode == WebUiChoice)
        {
            return await SetupWizardWebUi.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return CreateDefaultConfiguration();
    }

    /// <summary>
    /// Resolves the path to the currently executing binary.
    /// </summary>
    /// <returns>The executable path, or <c>null</c> if it cannot be determined.</returns>
    private static string? ResolveExecutablePath()
    {
        var path = Environment.ProcessPath;

        if (!string.IsNullOrEmpty(path))
        {
            return path;
        }

        using var current = Process.GetCurrentProcess();
        return current.MainModule?.FileName;
    }

    /// <summary>
    /// Creates a default configuration using Ollama defaults.
    /// </summary>
    /// <returns>A new configuration with Ollama provider defaults.</returns>
    private static SetupConfiguration CreateDefaultConfiguration()
    {
        return new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".openvepa"),
        };
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
