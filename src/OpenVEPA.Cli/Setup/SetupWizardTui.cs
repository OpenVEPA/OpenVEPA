using Spectre.Console;

namespace OpenVEPA.Cli.Setup;

/// <summary>
/// Interactive terminal-based setup wizard using Spectre.Console.
/// Guides the user through initial OpenVEPA configuration.
/// </summary>
internal static class SetupWizardTui
{
    private const string TelegramChannel = "Telegram";
    private const string WhatsAppChannel = "WhatsApp (Coming soon)";
    private const string SkipChannel = "Skip";

    /// <summary>Describes an LLM provider with its display label, key, default model, and default endpoint.</summary>
    private sealed record ProviderInfo(
        string Label,
        string Key,
        string DefaultModel,
        string DefaultEndpoint,
        bool RequiresApiKey);

    private static readonly ProviderInfo[] Providers =
    [
        new("Ollama (Local, free - recommended)", "ollama", "llama3.2", "http://localhost:11434", false),
        new("OpenAI (Cloud, requires API key)", "openai", "gpt-4o", "https://api.openai.com/v1", true),
        new("Google Gemini (Cloud, requires API key)", "google", "gemini-2.5-flash", "https://generativelanguage.googleapis.com/v1beta", true),
        new("Anthropic Claude (Cloud, requires API key)", "anthropic", "claude-sonnet-4-20250514", "https://api.anthropic.com/v1", true),
        new("Mistral AI (Cloud, requires API key)", "mistral", "mistral-large-latest", "https://api.mistral.ai/v1", true),
        new("Groq (Cloud, requires API key)", "groq", "llama-3.3-70b-versatile", "https://api.groq.com/openai/v1", true),
        new("Azure OpenAI (Cloud, requires API key)", "azure", "gpt-4o", string.Empty, true),
        new("Cohere (Cloud, requires API key)", "cohere", "command-r-plus", "https://api.cohere.com/v2", true),
        new("Together AI (Cloud, requires API key)", "together", "meta-llama/Llama-3.3-70B-Instruct-Turbo", "https://api.together.xyz/v1", true),
        new("Perplexity (Cloud, requires API key)", "perplexity", "sonar-pro", "https://api.perplexity.ai", true),
    ];

    /// <summary>Providers where the user must supply an endpoint (not a fixed public URL).</summary>
    private static readonly HashSet<string> UserEndpointProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "ollama",
        "azure",
    };

    /// <summary>Runs the interactive setup wizard and returns the configuration.</summary>
    /// <returns>The setup configuration, or null if the user cancelled.</returns>
    public static SetupConfiguration? Run()
    {
        WriteWelcomePanel();

        var providerInfo = PromptProvider();
        string? apiKey = null;
        string endpoint = providerInfo.DefaultEndpoint;

        if (providerInfo.RequiresApiKey)
        {
            apiKey = PromptApiKey(providerInfo.Key);
        }

        if (UserEndpointProviders.Contains(providerInfo.Key))
        {
            var label = string.Equals(providerInfo.Key, "ollama", StringComparison.OrdinalIgnoreCase)
                ? "Ollama endpoint:"
                : $"{providerInfo.Label.Split(' ')[0]} endpoint:";
            endpoint = PromptEndpoint(label, providerInfo.DefaultEndpoint);
        }

        var modelId = PromptModel(providerInfo.DefaultModel);
        var homePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".openvepa");

        string? telegramToken = null;
        var whatsAppEnabled = false;

        var channelChoice = PromptMessagingChannel();

        if (channelChoice == TelegramChannel)
        {
            telegramToken = PromptTelegramToken();
        }
        else if (channelChoice == WhatsAppChannel)
        {
            whatsAppEnabled = true;
        }

        var config = new SetupConfiguration
        {
            Provider = providerInfo.Key,
            ApiKey = apiKey,
            ModelId = modelId,
            HomePath = homePath,
            Endpoint = endpoint,
            TelegramBotToken = telegramToken,
            WhatsAppEnabled = whatsAppEnabled,
        };

        WriteSummaryTable(config);

        var confirmed = AnsiConsole.Confirm("Apply this configuration?");
        return confirmed ? config : null;
    }

    /// <summary>Displays the welcome panel.</summary>
    private static void WriteWelcomePanel()
    {
        var panel = new Panel(
                "This wizard will guide you through initial OpenVEPA configuration.\n" +
                "You will choose an LLM provider and model.")
            .Header("[bold]OpenVEPA Setup[/]")
            .Border(BoxBorder.Rounded);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>Prompts the user to select an LLM provider.</summary>
    /// <returns>The selected provider information.</returns>
    private static ProviderInfo PromptProvider()
    {
        var labels = Providers.Select(p => p.Label).ToArray();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which LLM provider would you like to use?")
                .AddChoices(labels));

        return Providers.First(p => p.Label == choice);
    }

    /// <summary>Prompts for a provider API key using a secret input.</summary>
    /// <param name="providerKey">The provider key for display purposes.</param>
    /// <returns>The API key string.</returns>
    private static string PromptApiKey(string providerKey)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>($"Enter your {providerKey} API key:")
                .Secret());
    }

    /// <summary>Prompts for an endpoint URL with a default value.</summary>
    /// <param name="label">The prompt label text.</param>
    /// <param name="defaultValue">The default endpoint URL.</param>
    /// <returns>The endpoint URL.</returns>
    private static string PromptEndpoint(string label, string defaultValue)
    {
        if (string.IsNullOrEmpty(defaultValue))
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>(label));
        }

        return AnsiConsole.Prompt(
            new TextPrompt<string>(label)
                .DefaultValue(defaultValue));
    }

    /// <summary>Prompts for the LLM model identifier.</summary>
    /// <param name="defaultModel">The default model based on the selected provider.</param>
    /// <returns>The chosen model identifier.</returns>
    private static string PromptModel(string defaultModel)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("Which model should be used?")
                .DefaultValue(defaultModel));
    }

    /// <summary>Prompts the user to select a messaging channel for remote control.</summary>
    /// <returns>The selected channel label.</returns>
    private static string PromptMessagingChannel()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Would you like to connect a messaging channel for remote control?")
                .AddChoices(TelegramChannel, WhatsAppChannel, SkipChannel));
    }

    /// <summary>Prompts for a Telegram bot token.</summary>
    /// <returns>The Telegram bot token string.</returns>
    private static string PromptTelegramToken()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your Telegram Bot Token (from @BotFather):")
                .Secret());
    }

    /// <summary>Displays a summary table of the chosen configuration.</summary>
    /// <param name="config">The configuration to display.</param>
    private static void WriteSummaryTable(SetupConfiguration config)
    {
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Configuration Summary[/]")
            .AddColumn("[bold]Setting[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Provider", Markup.Escape(config.Provider));
        table.AddRow("Model", Markup.Escape(config.ModelId));
        table.AddRow("Endpoint", Markup.Escape(config.Endpoint ?? string.Empty));

        if (!string.IsNullOrEmpty(config.ApiKey))
        {
            table.AddRow("API Key", "********");
        }

        table.AddRow(
            "Telegram",
            string.IsNullOrEmpty(config.TelegramBotToken) ? "-" : "Enabled");
        table.AddRow(
            "WhatsApp",
            config.WhatsAppEnabled ? "Coming Soon" : "-");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
