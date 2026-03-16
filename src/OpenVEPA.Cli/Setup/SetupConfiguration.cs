namespace OpenVEPA.Cli.Setup;

/// <summary>Holds the configuration choices made during the setup wizard.</summary>
internal sealed record SetupConfiguration
{
    /// <summary>LLM provider key (e.g., "ollama", "openai", "google", "anthropic").</summary>
    public required string Provider { get; init; }

    /// <summary>API key for cloud providers (null/empty for Ollama).</summary>
    public string? ApiKey { get; init; }

    /// <summary>LLM model identifier (e.g., "llama3.2", "gpt-4o").</summary>
    public required string ModelId { get; init; }

    /// <summary>OpenVEPA home directory path.</summary>
    public required string HomePath { get; init; }

    /// <summary>Server listen port.</summary>
    public int Port { get; init; } = 8371;

    /// <summary>Provider endpoint URL (provider-specific default if not set).</summary>
    public string? Endpoint { get; init; }

    /// <summary>Ollama endpoint URL (kept for backward compatibility).</summary>
    public string OllamaEndpoint { get; init; } = "http://localhost:11434";

    /// <summary>OpenAI endpoint URL (kept for backward compatibility).</summary>
    public string OpenAiEndpoint { get; init; } = "https://api.openai.com/v1";

    /// <summary>Whether the scheduler is enabled.</summary>
    public bool SchedulerEnabled { get; init; } = true;

    /// <summary>Telegram bot token for remote control via Telegram (null if not configured).</summary>
    public string? TelegramBotToken { get; init; }

    /// <summary>Whether WhatsApp integration is enabled (placeholder for future implementation).</summary>
    public bool WhatsAppEnabled { get; init; }
}
