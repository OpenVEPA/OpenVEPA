using Microsoft.Extensions.Configuration;

namespace OpenVEPA.Providers;

/// <summary>
/// Top-level configuration for LLM provider selection and settings.
/// Binds from the "Providers" configuration section.
/// </summary>
public sealed class ProviderOptions
{
    /// <summary>The key of the default provider ("openai" or "ollama").</summary>
    public string DefaultProvider { get; set; } = "ollama";

    /// <summary>The default model name when not overridden per-provider.</summary>
    public string DefaultModel { get; set; } = "llama3";

    /// <summary>OpenAI-specific settings. Null when OpenAI is not configured.</summary>
    public OpenAiOptions? OpenAi { get; set; }

    /// <summary>Ollama-specific settings. Null when Ollama is not configured.</summary>
    public OllamaOptions? Ollama { get; set; }
}

/// <summary>Configuration for the OpenAI provider.</summary>
public sealed class OpenAiOptions
{
    /// <summary>The OpenAI API key. Required for OpenAI to be registered.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The chat model to use.</summary>
    [ConfigurationKeyName("ModelId")]
    public string Model { get; set; } = "gpt-4o";

    /// <summary>Optional custom endpoint for OpenAI-compatible APIs.</summary>
    public string? Endpoint { get; set; }
}

/// <summary>Configuration for the Ollama provider.</summary>
public sealed class OllamaOptions
{
    /// <summary>The Ollama server endpoint.</summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>The model to use.</summary>
    [ConfigurationKeyName("ModelId")]
    public string Model { get; set; } = "llama3";
}
