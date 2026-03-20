using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenVEPA.Core.Sessions;
using OpenVEPA.Storage;
using OpenVEPA.Storage.Entities;

namespace OpenVEPA.Server.Api;

/// <summary>Maps authenticated REST API endpoints for system status and LLM configuration.</summary>
internal static class SystemApiExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>Maps provider names (lowercase) to their configuration section names.</summary>
    private static readonly Dictionary<string, string> KnownProviderSections =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ollama"] = "Ollama",
            ["openai"] = "OpenAi",
            ["google"] = "Google",
            ["anthropic"] = "Anthropic",
            ["mistral"] = "Mistral",
            ["groq"] = "Groq",
            ["azure"] = "Azure",
            ["cohere"] = "Cohere",
            ["together"] = "Together",
            ["perplexity"] = "Perplexity",
        };

    /// <summary>Ordered list of known provider section names and their canonical provider keys.</summary>
    private static readonly (string SectionName, string ProviderName)[] KnownProviderMappings =
    [
        ("Ollama", "ollama"),
        ("OpenAi", "openai"),
        ("Google", "google"),
        ("Anthropic", "anthropic"),
        ("Mistral", "mistral"),
        ("Groq", "groq"),
        ("Azure", "azure"),
        ("Cohere", "cohere"),
        ("Together", "together"),
        ("Perplexity", "perplexity"),
    ];

    /// <summary>
    /// Maps authenticated REST endpoints under <c>/api/system</c>.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapSystemApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder system = app.MapGroup("/api/system");

        system.MapGet("/status", async (
            SetupCompletionService setupService,
            ISessionStore sessionStore,
            CancellationToken ct) =>
        {
            var sessions = await sessionStore.ListSessionsAsync(ct).ConfigureAwait(false);
            var activeSessionsCount = sessions.Count(static session => session.Status == SessionStatus.Active);
            var llmConfig = await ReadUserLlmConfigAsync(setupService.ConfigPath, ct).ConfigureAwait(false);
            var defaultInstance = llmConfig.Providers.FirstOrDefault(
                p => string.Equals(p.Name, llmConfig.DefaultProvider, StringComparison.OrdinalIgnoreCase));

            return Results.Ok(new SystemStatusResponse(
                Version: GetApplicationVersion(),
                Uptime: GetUptime(),
                Provider: defaultInstance?.Type ?? llmConfig.DefaultProvider,
                Model: defaultInstance?.ModelId ?? "unknown",
                SetupComplete: setupService.IsSetupComplete,
                DatabasePath: ResolveDatabasePath(setupService.ConfigPath),
                ActiveSessionsCount: activeSessionsCount));
        }).RequireAuthorization();

        system.MapGet("/llm-config", async (
            SetupCompletionService setupService,
            CancellationToken ct) =>
        {
            var config = await ReadUserLlmConfigAsync(setupService.ConfigPath, ct).ConfigureAwait(false);
            return Results.Ok(config);
        }).RequireAuthorization();

        system.MapPut("/llm-config", async (
            UpdateLlmConfigRequest? request,
            SetupCompletionService setupService,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new { error = "Request body is required." });
            }

            var defaultProvider = NormalizeProvider(request.DefaultProvider);
            if (defaultProvider is null)
            {
                return Results.BadRequest(new { error = "DefaultProvider is required." });
            }

            if (request.Providers is null || request.Providers.Count == 0)
            {
                return Results.BadRequest(new { error = "At least one provider configuration is required." });
            }

            var validated = new List<ValidatedProviderInstance>(request.Providers.Count);
            foreach (var entry in request.Providers)
            {
                var name = NormalizeProvider(entry.Name);
                if (name is null)
                {
                    return Results.BadRequest(new { error = "Each provider must have a name." });
                }

                var modelId = NormalizeModelId(entry.ModelId);
                if (modelId is null)
                {
                    return Results.BadRequest(new { error = $"ModelId is required for provider '{name}'." });
                }

                var endpoint = NormalizeEndpoint(entry.Endpoint);
                if (endpoint is null)
                {
                    return Results.BadRequest(new { error = $"Endpoint is required for provider '{name}'." });
                }

                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
                {
                    return Results.BadRequest(new { error = $"Endpoint for provider '{name}' must be a valid absolute URL." });
                }

                var type = NormalizeProvider(entry.Type) ?? name;
                var displayName = string.IsNullOrWhiteSpace(entry.DisplayName)
                    ? GetProviderSectionName(type)
                    : entry.DisplayName.Trim();

                var apiKey = string.IsNullOrWhiteSpace(entry.ApiKey) ? null : entry.ApiKey.Trim();
                validated.Add(new ValidatedProviderInstance(name, displayName, type, modelId, endpoint, apiKey));
            }

            if (!validated.Exists(p => string.Equals(p.Name, defaultProvider, StringComparison.OrdinalIgnoreCase)))
            {
                return Results.BadRequest(new { error = $"DefaultProvider '{defaultProvider}' must be included in the providers list." });
            }

            try
            {
                await WriteMultiProviderConfigurationAsync(setupService.ConfigPath, defaultProvider, validated, ct)
                    .ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                return Results.Json(
                    new { error = $"Failed to write configuration: {ex.Message}" },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(
                    new { error = $"Failed to update configuration: {ex.Message}" },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(
                    new { error = $"Failed to update configuration: {ex.Message}" },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (JsonException ex)
            {
                return Results.Json(
                    new { error = $"Failed to parse configuration: {ex.Message}" },
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            setupService.MarkComplete();

            if (configuration is IConfigurationRoot configurationRoot)
            {
                configurationRoot.Reload();
            }

            var updatedConfiguration = await ReadUserLlmConfigAsync(setupService.ConfigPath, ct).ConfigureAwait(false);
            return Results.Ok(new UpdateLlmConfigResponse(
                Updated: true,
                RequiresRestart: true,
                Message: "LLM configuration was written to appsettings.json. Restart the application for all services to pick up the new settings.",
                Configuration: updatedConfiguration));
        }).RequireAuthorization();

        system.MapGet("/llm-usage", async (
            IDbContextFactory<OpenVepaDbContext> dbFactory,
            DateTimeOffset? since,
            string? provider,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            IQueryable<LlmAuditLogEntity> query = db.LlmAuditLogEntries.AsNoTracking();

            if (since is not null)
            {
                query = query.Where(entry => entry.Timestamp >= since.Value.UtcDateTime);
            }

            var normalizedProvider = NormalizeProvider(provider);
            if (normalizedProvider is not null)
            {
                query = query.Where(entry =>
                    EF.Functions.Collate(entry.Provider, "NOCASE") == normalizedProvider);
            }

            var summary = await query
                .GroupBy(static _ => 1)
                .Select(group => new
                {
                    CallCount = group.Count(),
                    InputTokens = group.Sum(static entry => (long)entry.InputTokens),
                    OutputTokens = group.Sum(static entry => (long)entry.OutputTokens),
                    TotalTokens = group.Sum(static entry => (long)entry.InputTokens + entry.OutputTokens),
                    TotalCostUsd = group.Sum(static entry => entry.EstimatedCostUsd ?? 0m),
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            return Results.Ok(new LlmUsageSummaryResponse(
                Since: since,
                Provider: normalizedProvider,
                CallCount: summary?.CallCount ?? 0,
                InputTokens: summary?.InputTokens ?? 0,
                OutputTokens: summary?.OutputTokens ?? 0,
                TotalTokens: summary?.TotalTokens ?? 0,
                TotalCostUsd: summary?.TotalCostUsd ?? 0m));
        }).RequireAuthorization();

        return app;
    }

    private static async Task<LlmMultiProviderResponse> ReadUserLlmConfigAsync(
        string configPath,
        CancellationToken ct)
    {
        if (!File.Exists(configPath))
        {
            return new LlmMultiProviderResponse("unknown", []);
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(configPath, ct).ConfigureAwait(false);
        }
        catch (IOException)
        {
            return new LlmMultiProviderResponse("unknown", []);
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return new LlmMultiProviderResponse("unknown", []);
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return new LlmMultiProviderResponse("unknown", []);
        }

        if (root is null)
        {
            return new LlmMultiProviderResponse("unknown", []);
        }

        if (root["Providers"] is not JsonObject providersNode)
        {
            return new LlmMultiProviderResponse("unknown", []);
        }

        var defaultProvider = NormalizeProvider(providersNode["DefaultProvider"]?.GetValue<string>()) ?? "unknown";

        // New instance-based format.
        if (providersNode["Instances"] is JsonArray instancesArray)
        {
            return ReadInstanceFormat(defaultProvider, instancesArray);
        }

        // Legacy section-based format.
        return ReadLegacySectionFormat(defaultProvider, providersNode);
    }

    private static LlmMultiProviderResponse ReadInstanceFormat(
        string defaultProvider,
        JsonArray instancesArray)
    {
        var providers = new List<LlmProviderEntry>();
        foreach (var node in instancesArray)
        {
            if (node is not JsonObject instance)
            {
                continue;
            }

            var name = instance["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var type = instance["type"]?.GetValue<string>() ?? name;
            var displayName = instance["displayName"]?.GetValue<string>() ?? name;
            var modelId = instance["modelId"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(modelId))
            {
                continue;
            }

            var endpoint = instance["endpoint"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = ModelListProxy.GetDefaultEndpoint(type);
            }

            providers.Add(new LlmProviderEntry(name, displayName, type, modelId, endpoint));
        }

        return new LlmMultiProviderResponse(defaultProvider, providers);
    }

    private static LlmMultiProviderResponse ReadLegacySectionFormat(
        string defaultProvider,
        JsonObject providersNode)
    {
        var providers = new List<LlmProviderEntry>();

        foreach (var (sectionName, providerName) in KnownProviderMappings)
        {
            if (providersNode[sectionName] is not JsonObject section)
            {
                continue;
            }

            var modelId = section["ModelId"]?.GetValue<string>() ?? section["Model"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(modelId))
            {
                continue;
            }

            var endpoint = section["Endpoint"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = ModelListProxy.GetDefaultEndpoint(providerName);
            }

            providers.Add(new LlmProviderEntry(providerName, sectionName, providerName, modelId, endpoint));
        }

        return new LlmMultiProviderResponse(defaultProvider, providers);
    }

    /// <summary>Validated provider instance for writing configuration.</summary>
    private readonly record struct ValidatedProviderInstance(
        string Name,
        string DisplayName,
        string Type,
        string ModelId,
        string Endpoint,
        string? ApiKey);

    private static async Task WriteMultiProviderConfigurationAsync(
        string configPath,
        string defaultProvider,
        List<ValidatedProviderInstance> providers,
        CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        JsonObject root = await LoadConfigurationRootAsync(configPath, ct).ConfigureAwait(false);
        JsonObject providersSection = EnsureChildObject(root, "Providers");
        providersSection["DefaultProvider"] = defaultProvider;

        // Remove legacy section-based keys.
        foreach (var (sectionName, _) in KnownProviderMappings)
        {
            providersSection.Remove(sectionName);
        }

        // Write new instance-based format.
        var instancesArray = new JsonArray();
        foreach (var p in providers)
        {
            var instance = new JsonObject
            {
                ["name"] = p.Name,
                ["displayName"] = p.DisplayName,
                ["type"] = p.Type,
                ["endpoint"] = p.Endpoint,
                ["modelId"] = p.ModelId,
            };

            if (!string.IsNullOrWhiteSpace(p.ApiKey))
            {
                instance["apiKey"] = p.ApiKey;
            }

            instancesArray.Add(instance);
        }

        providersSection["Instances"] = instancesArray;

        var json = root.ToJsonString(JsonOptions);
        await File.WriteAllTextAsync(configPath, json, ct).ConfigureAwait(false);
    }

    private static async Task<JsonObject> LoadConfigurationRootAsync(string configPath, CancellationToken ct)
    {
        if (!File.Exists(configPath))
        {
            return new JsonObject();
        }

        var json = await File.ReadAllTextAsync(configPath, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        var root = JsonNode.Parse(json) as JsonObject;
        return root ?? throw new InvalidOperationException("The configuration file must contain a JSON object at the root.");
    }

    private static JsonObject EnsureChildObject(JsonObject parent, string propertyName)
    {
        if (parent[propertyName] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        parent[propertyName] = created;
        return created;
    }

    private static string GetApplicationVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(SystemApiExtensions).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    private static string GetUptime()
    {
        using var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
        return uptime.ToString("c", CultureInfo.InvariantCulture);
    }

    private static string ResolveDatabasePath(string configPath)
    {
        var homeDirectory = Path.GetDirectoryName(configPath);
        return string.IsNullOrWhiteSpace(homeDirectory)
            ? Path.Combine("data", "openvepa.db")
            : Path.Combine(homeDirectory, "data", "openvepa.db");
    }

    private static string GetProviderSectionName(string provider)
    {
        if (KnownProviderSections.TryGetValue(provider, out var sectionName))
        {
            return sectionName;
        }

        return string.IsNullOrEmpty(provider)
            ? provider
            : string.Concat(provider[..1].ToUpperInvariant(), provider.AsSpan(1));
    }

    private static string? NormalizeProvider(string? provider) =>
        string.IsNullOrWhiteSpace(provider) ? null : provider.Trim().ToLowerInvariant();

    private static string? NormalizeModelId(string? modelId) =>
        string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();

    private static string? NormalizeEndpoint(string? endpoint) =>
        string.IsNullOrWhiteSpace(endpoint) ? null : endpoint.Trim().TrimEnd('/');
}

/// <summary>Represents a single provider instance in the multi-provider response.</summary>
internal sealed record LlmProviderEntry(string Name, string DisplayName, string Type, string ModelId, string? Endpoint);

/// <summary>Represents the multi-provider LLM configuration response.</summary>
internal sealed record LlmMultiProviderResponse(
    string DefaultProvider,
    IReadOnlyList<LlmProviderEntry> Providers);

/// <summary>Represents a single provider's input in an update request.</summary>
internal sealed record ProviderConfigInput
{
    /// <summary>Gets the instance name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the user-friendly display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the provider type (e.g., "ollama", "openai", "google").</summary>
    public string? Type { get; init; }

    /// <summary>Gets the model identifier.</summary>
    public string? ModelId { get; init; }

    /// <summary>Gets the provider endpoint URL.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Gets the API key (optional).</summary>
    public string? ApiKey { get; init; }
}

/// <summary>Represents a request to update LLM runtime configuration.</summary>
internal sealed record UpdateLlmConfigRequest
{
    /// <summary>Gets the default provider key.</summary>
    public string? DefaultProvider { get; init; }

    /// <summary>Gets the provider configurations to add or update.</summary>
    public List<ProviderConfigInput>? Providers { get; init; }
}

/// <summary>Represents the result of updating LLM configuration.</summary>
internal sealed record UpdateLlmConfigResponse(
    bool Updated,
    bool RequiresRestart,
    string Message,
    LlmMultiProviderResponse Configuration);

/// <summary>Represents the enhanced system status response.</summary>
internal sealed record SystemStatusResponse(
    string Version,
    string Uptime,
    string Provider,
    string Model,
    bool SetupComplete,
    string DatabasePath,
    int ActiveSessionsCount);

/// <summary>Represents aggregated LLM usage statistics.</summary>
internal sealed record LlmUsageSummaryResponse(
    DateTimeOffset? Since,
    string? Provider,
    int CallCount,
    long InputTokens,
    long OutputTokens,
    long TotalTokens,
    decimal TotalCostUsd);
