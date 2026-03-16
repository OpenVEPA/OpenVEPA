using OpenVEPA.Cli.Setup;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Runs the OpenVEPA setup wizard to create or update the configuration
/// without installing the system service.
/// </summary>
internal sealed class SetupCommand : AsyncCommand
{
    private const string TuiChoice = "Terminal (TUI) - Interactive terminal wizard";
    private const string WebUiChoice = "Browser (WebUI) - Setup in your web browser";
    private const string SkipChoice = "Skip setup - Use default configuration";

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var config = await PromptAndRunSetupAsync().ConfigureAwait(false);

        if (config is null)
        {
            AnsiConsole.MarkupLine("[dim]Setup cancelled.[/]");
            return 1;
        }

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None)
            .ConfigureAwait(false);

        AnsiConsole.Write(new Panel(Markup.Escape($"Configuration saved to {config.HomePath}"))
            .Header("[bold green]Success[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green));

        return 0;
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
}
