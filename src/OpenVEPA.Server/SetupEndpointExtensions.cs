using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using OpenVEPA.Storage;

namespace OpenVEPA.Server;

/// <summary>
/// Maps setup wizard API endpoints under the <c>/init/</c> prefix for first-run configuration.
/// The HTML page endpoint is mapped separately from the Cli assembly (which owns the HTML content).
/// </summary>
internal static class SetupEndpointExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Maps the setup wizard API endpoints for model listing, config submission, and cancellation.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapSetupApiEndpoints(this WebApplication app)
    {
        // Redirect /init and /init/ to the wizard page.
        app.MapGet("/init", () => Results.Redirect("/init/setupwizard"));

        // Model list proxy.
        app.MapGet("/init/api/models", async (HttpContext context) =>
        {
            var provider = context.Request.Query["provider"].ToString();
            if (string.IsNullOrWhiteSpace(provider))
            {
                return Results.BadRequest(new { error = "Missing required parameter: provider" });
            }

            if (!ModelListProxy.ProviderSupportsListing(provider))
            {
                return Results.Ok(new
                {
                    success = false,
                    models = Array.Empty<string>(),
                    provider,
                    endpoint = string.Empty,
                    error = "Provider does not support dynamic model listing.",
                    diagnostics = $"The provider '{provider}' does not expose a model list API.",
                });
            }

            var apiKey = context.Request.Query["apiKey"].ToString();
            var endpoint = context.Request.Query["endpoint"].ToString();

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = ModelListProxy.GetDefaultEndpoint(provider);
            }

            endpoint = endpoint.TrimEnd('/');

            var result = await ModelListProxy.FetchModelsWithStatusAsync(
                provider, apiKey, endpoint, context.RequestAborted).ConfigureAwait(false);

            return Results.Ok(new
            {
                success = result.Success,
                models = result.Models,
                provider = result.Provider,
                endpoint = result.Endpoint,
                error = result.Error,
                diagnostics = result.Diagnostics,
            });
        });

        // Setup submission.
        app.MapPost("/init/api/setup", async (HttpContext context) =>
        {
            var setupService = context.RequestServices.GetRequiredService<SetupCompletionService>();

            SetupSubmission? submission;
            try
            {
                submission = await JsonSerializer.DeserializeAsync<SetupSubmission>(
                    context.Request.Body, JsonOptions, context.RequestAborted)
                    .ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return Results.BadRequest("Invalid configuration JSON.");
            }

            if (submission is null)
            {
                return Results.BadRequest("Empty configuration body.");
            }

            // Default to the SetupCompletionService's home directory so config writes to
            // the volume-mounted path instead of the application directory.
            var homePath = string.IsNullOrWhiteSpace(submission.HomePath)
                ? Path.GetDirectoryName(setupService.ConfigPath)!
                : ExpandHomePath(submission.HomePath);

            await WriteSetupConfigAsync(submission, homePath, context.RequestAborted)
                .ConfigureAwait(false);

            setupService.MarkComplete();

            var tokenStore = context.RequestServices.GetRequiredService<SqliteTokenStore>();
            var result = await tokenStore.CreateTokenAsync("web-default", context.RequestAborted)
                .ConfigureAwait(false);

            return Results.Ok(new
            {
                redirectUrl = "/",
                token = result.PlaintextToken,
                tokenId = result.TokenId,
            });
        });

        // Cancel.
        app.MapPost("/init/api/cancel", () => Results.Ok());

        return app;
    }

    /// <summary>Writes the setup configuration as openvepa.conf to the home directory.</summary>
    /// <param name="submission">The submitted configuration values.</param>
    /// <param name="homePath">The resolved home directory path.</param>
    /// <param name="ct">A cancellation token.</param>
    private static async Task WriteSetupConfigAsync(
        SetupSubmission submission,
        string homePath,
        CancellationToken ct)
    {
        // Delegate to the Cli SetupConfigurationWriter via a dynamically built config.
        // Since Server cannot reference Cli, we replicate the minimal write logic here.
        var subdirectories = new[]
        {
            "data",
            Path.Combine("data", "documents"),
            "skills",
            "agents",
            "logs",
        };

        Directory.CreateDirectory(homePath);
        foreach (var sub in subdirectories)
        {
            Directory.CreateDirectory(Path.Combine(homePath, sub));
        }

        var json = BuildSettingsJson(submission, homePath);
        var path = Path.Combine(homePath, "openvepa.conf");
        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
    }

    /// <summary>Builds the openvepa.conf content as an indented JSON string.</summary>
    /// <param name="s">The submitted configuration.</param>
    /// <param name="homePath">The resolved home directory.</param>
    /// <returns>The formatted JSON string.</returns>
    private static string BuildSettingsJson(SetupSubmission s, string homePath)
    {
        var provider = s.Provider ?? "ollama";
        var modelId = s.ModelId ?? string.Empty;
        var apiKey = s.ApiKey ?? string.Empty;
        var port = s.Port > 0 ? s.Port : 8371;

        var root = new System.Text.Json.Nodes.JsonObject
        {
            ["Logging"] = new System.Text.Json.Nodes.JsonObject
            {
                ["LogLevel"] = new System.Text.Json.Nodes.JsonObject
                {
                    ["Default"] = "Information",
                },
            },
            ["Providers"] = BuildProvidersNode(provider, modelId, apiKey, s),
            ["Skills"] = new System.Text.Json.Nodes.JsonObject
            {
                ["SkillsDirectory"] = NormalizePath(Path.Combine(homePath, "skills")),
            },
            ["OpenVEPA"] = new System.Text.Json.Nodes.JsonObject
            {
                ["Agents"] = new System.Text.Json.Nodes.JsonObject
                {
                    ["AgentsDirectory"] = NormalizePath(Path.Combine(homePath, "agents")),
                },
            },
            ["Scheduler"] = new System.Text.Json.Nodes.JsonObject
            {
                ["Enabled"] = s.SchedulerEnabled,
                ["DashboardEnabled"] = false,
                ["Schedules"] = new System.Text.Json.Nodes.JsonArray(),
            },
            ["Channels"] = BuildChannelsNode(s),
            ["Kestrel"] = new System.Text.Json.Nodes.JsonObject
            {
                ["Endpoints"] = new System.Text.Json.Nodes.JsonObject
                {
                    ["Http"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["Url"] = $"http://localhost:{port}",
                    },
                },
            },
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        return root.ToJsonString(options);
    }

    private static readonly HashSet<string> OpenAiCompatibleProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "openai", "groq", "together", "perplexity", "azure",
        };

    /// <summary>
    /// Default endpoints for providers that don't use the generic OpenAI-compatible endpoint.
    /// Used during setup to persist the correct base URL into the config file.
    /// </summary>
    private static readonly Dictionary<string, string> ProviderDefaultEndpoints =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["google"] = "https://generativelanguage.googleapis.com/v1beta",
            ["anthropic"] = "https://api.anthropic.com",
            ["mistral"] = "https://api.mistral.ai/v1",
            ["cohere"] = "https://api.cohere.ai/v1",
        };

    /// <summary>Builds the Providers configuration node using the instance-based format.</summary>
    private static System.Text.Json.Nodes.JsonObject BuildProvidersNode(
        string provider,
        string modelId,
        string apiKey,
        SetupSubmission s)
    {
        var endpoint = ResolveSetupEndpoint(provider, s);
        var displayName = string.IsNullOrEmpty(provider)
            ? provider
            : string.Concat(provider[..1].ToUpperInvariant(), provider.AsSpan(1));

        var instance = new System.Text.Json.Nodes.JsonObject
        {
            ["name"] = provider,
            ["displayName"] = displayName,
            ["type"] = provider,
            ["endpoint"] = endpoint,
            ["modelId"] = modelId, // backward compat: keep as "modelId" for setup wizard
            ["defaultModel"] = modelId,
        };

        if (!string.IsNullOrEmpty(apiKey))
        {
            instance["apiKey"] = apiKey;
        }

        var providers = new System.Text.Json.Nodes.JsonObject
        {
            ["DefaultProvider"] = provider,
            ["Instances"] = new System.Text.Json.Nodes.JsonArray(instance),
        };

        return providers;
    }

    private static string ResolveSetupEndpoint(string provider, SetupSubmission s)
    {
        if (!string.IsNullOrWhiteSpace(s.Endpoint))
        {
            return s.Endpoint;
        }

        if (string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            return s.OllamaEndpoint ?? "http://localhost:11434";
        }

        if (OpenAiCompatibleProviders.Contains(provider))
        {
            return s.OpenAiEndpoint ?? "https://api.openai.com/v1";
        }

        // Return provider-specific default endpoint (Google, Anthropic, Mistral, etc.).
        if (ProviderDefaultEndpoints.TryGetValue(provider, out var defaultEndpoint))
        {
            return defaultEndpoint;
        }

        return string.Empty;
    }

    /// <summary>Builds the Channels configuration node.</summary>
    private static System.Text.Json.Nodes.JsonObject BuildChannelsNode(SetupSubmission s)
    {
        var channels = new System.Text.Json.Nodes.JsonObject();

        if (!string.IsNullOrEmpty(s.TelegramBotToken))
        {
            channels["Telegram"] = new System.Text.Json.Nodes.JsonObject
            {
                ["Enabled"] = true,
                ["BotToken"] = s.TelegramBotToken,
            };
        }

        if (s.WhatsAppEnabled)
        {
            channels["WhatsApp"] = new System.Text.Json.Nodes.JsonObject
            {
                ["Enabled"] = true,
            };
        }

        return channels;
    }

    /// <summary>Expands a leading tilde to the user home directory.</summary>
    private static string ExpandHomePath(string path)
    {
        if (!path.StartsWith('~'))
        {
            return path;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(
            home,
            path[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    /// <summary>Normalizes a file path to use forward slashes for JSON portability.</summary>
    private static string NormalizePath(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Resolves the <see cref="SetupCompletionService"/> from the service provider.
    /// </summary>
    private static T GetRequiredService<T>(this IServiceProvider sp) where T : notnull =>
        (T)(sp.GetService(typeof(T))
            ?? throw new InvalidOperationException($"Service {typeof(T).Name} not registered."));
}

/// <summary>JSON model for the setup form submission from the browser.</summary>
internal sealed record SetupSubmission
{
    /// <summary>LLM provider key (e.g. "ollama", "openai").</summary>
    public string? Provider { get; init; }

    /// <summary>API key for cloud providers.</summary>
    public string? ApiKey { get; init; }

    /// <summary>LLM model identifier.</summary>
    public string? ModelId { get; init; }

    /// <summary>OpenVEPA home directory path.</summary>
    public string? HomePath { get; init; }

    /// <summary>Server listen port.</summary>
    public int Port { get; init; } = 8371;

    /// <summary>Provider endpoint URL.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Ollama endpoint URL.</summary>
    public string? OllamaEndpoint { get; init; }

    /// <summary>OpenAI endpoint URL.</summary>
    public string? OpenAiEndpoint { get; init; }

    /// <summary>Whether the scheduler is enabled.</summary>
    public bool SchedulerEnabled { get; init; } = true;

    /// <summary>Telegram bot token.</summary>
    public string? TelegramBotToken { get; init; }

    /// <summary>Whether WhatsApp integration is enabled.</summary>
    public bool WhatsAppEnabled { get; init; }
}
