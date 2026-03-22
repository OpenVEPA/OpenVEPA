using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenVEPA.Cli.Setup;

/// <summary>Writes setup configuration to the OpenVEPA home directory.</summary>
internal static class SetupConfigurationWriter
{
    private static readonly string[] Subdirectories =
    [
        "data",
        Path.Combine("data", "documents"),
        "skills",
        "agents",
        "logs",
    ];

    /// <summary>
    /// Creates the directory structure and writes openvepa.conf.
    /// </summary>
    /// <param name="config">The setup configuration to persist.</param>
    /// <param name="ct">A cancellation token.</param>
    public static async Task WriteAsync(SetupConfiguration config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        CreateDirectories(config.HomePath);

        var json = BuildSettingsJson(config);
        var path = Path.Combine(config.HomePath, "openvepa.conf");

        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
    }

    /// <summary>Creates the required subdirectory structure under the home path.</summary>
    /// <param name="homePath">The root home directory.</param>
    private static void CreateDirectories(string homePath)
    {
        Directory.CreateDirectory(homePath);

        foreach (var sub in Subdirectories)
        {
            Directory.CreateDirectory(Path.Combine(homePath, sub));
        }
    }

    /// <summary>Builds the openvepa.conf content as an indented JSON string.</summary>
    /// <param name="config">The configuration to serialize.</param>
    /// <returns>The formatted JSON string.</returns>
    private static string BuildSettingsJson(SetupConfiguration config)
    {
        var root = new JsonObject
        {
            ["Logging"] = new JsonObject
            {
                ["LogLevel"] = new JsonObject
                {
                    ["Default"] = "Information",
                },
            },
            ["Providers"] = BuildProvidersNode(config),
            ["Skills"] = new JsonObject
            {
                ["SkillsDirectory"] = NormalizePath(Path.Combine(config.HomePath, "skills")),
            },
            ["OpenVEPA"] = new JsonObject
            {
                ["Agents"] = new JsonObject
                {
                    ["AgentsDirectory"] = NormalizePath(Path.Combine(config.HomePath, "agents")),
                },
            },
            ["Scheduler"] = new JsonObject
            {
                ["Enabled"] = config.SchedulerEnabled,
                ["DashboardEnabled"] = false,
                ["Schedules"] = new JsonArray(),
            },
            ["Channels"] = BuildChannelsNode(config),
            ["Kestrel"] = new JsonObject
            {
                ["Endpoints"] = new JsonObject
                {
                    ["Http"] = new JsonObject
                    {
                        ["Url"] = $"http://localhost:{config.Port}",
                    },
                },
            },
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        return root.ToJsonString(options);
    }

    /// <summary>OpenAI-compatible provider keys that map to the OpenAi configuration section.</summary>
    private static readonly HashSet<string> OpenAiCompatibleProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "openai", "groq", "together", "perplexity", "azure",
    };

    /// <summary>Builds the Providers configuration node.</summary>
    /// <param name="config">The setup configuration.</param>
    /// <returns>A JSON object for the Providers section.</returns>
    private static JsonObject BuildProvidersNode(SetupConfiguration config)
    {
        var providers = new JsonObject
        {
            ["DefaultProvider"] = config.Provider,
        };

        if (string.Equals(config.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            providers["Ollama"] = new JsonObject
            {
                ["Endpoint"] = config.Endpoint ?? config.OllamaEndpoint,
                ["ModelId"] = config.ModelId,
            };

            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                providers["OpenAi"] = BuildOpenAiNode(config);
            }
        }
        else if (OpenAiCompatibleProviders.Contains(config.Provider))
        {
            providers["OpenAi"] = BuildOpenAiNode(config);
        }
        else
        {
            providers[ProviderSectionName(config.Provider)] = BuildGenericProviderNode(config);
        }

        return providers;
    }

    /// <summary>Builds the OpenAI provider configuration node.</summary>
    /// <param name="config">The setup configuration.</param>
    /// <returns>A JSON object for the OpenAI section.</returns>
    private static JsonObject BuildOpenAiNode(SetupConfiguration config)
    {
        return new JsonObject
        {
            ["ApiKey"] = config.ApiKey ?? string.Empty,
            ["ModelId"] = config.ModelId,
            ["Endpoint"] = config.Endpoint ?? config.OpenAiEndpoint,
        };
    }

    /// <summary>Builds a generic provider configuration node for non-OpenAI-compatible providers.</summary>
    /// <param name="config">The setup configuration.</param>
    /// <returns>A JSON object for the provider section.</returns>
    private static JsonObject BuildGenericProviderNode(SetupConfiguration config)
    {
        return new JsonObject
        {
            ["ApiKey"] = config.ApiKey ?? string.Empty,
            ["Endpoint"] = config.Endpoint ?? string.Empty,
            ["ModelId"] = config.ModelId,
        };
    }

    /// <summary>Converts a provider key to its configuration section name (PascalCase).</summary>
    /// <param name="provider">The provider key (e.g., "google", "anthropic").</param>
    /// <returns>The section name with the first character uppercased.</returns>
    private static string ProviderSectionName(string provider)
    {
        if (string.IsNullOrEmpty(provider))
        {
            return provider;
        }

        return string.Concat(provider[..1].ToUpperInvariant(), provider.AsSpan(1));
    }

    /// <summary>Builds the Channels configuration node.</summary>
    /// <param name="config">The setup configuration.</param>
    /// <returns>A JSON object for the Channels section.</returns>
    private static JsonObject BuildChannelsNode(SetupConfiguration config)
    {
        var channels = new JsonObject();

        if (!string.IsNullOrEmpty(config.TelegramBotToken))
        {
            channels["Telegram"] = new JsonObject
            {
                ["Enabled"] = true,
                ["BotToken"] = config.TelegramBotToken,
            };
        }

        if (config.WhatsAppEnabled)
        {
            channels["WhatsApp"] = new JsonObject
            {
                ["Enabled"] = true,
            };
        }

        return channels;
    }

    /// <summary>Normalizes a file path to use forward slashes for JSON portability.</summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The path with forward slashes.</returns>
    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
