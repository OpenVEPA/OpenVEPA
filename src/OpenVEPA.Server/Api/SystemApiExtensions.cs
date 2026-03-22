using System.ClientModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenVEPA.Agents.Runtime;
using OpenVEPA.Core.Sessions;
using OpenVEPA.Providers;
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

        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("OpenVEPA.Api.System");

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
                Model: defaultInstance?.DefaultModel ?? "unknown",
                SetupComplete: setupService.IsSetupComplete,
                DatabasePath: ResolveDatabasePath(setupService.ConfigPath),
                ActiveSessionsCount: activeSessionsCount));
        }).RequireAuthorization();

        system.MapGet("/llm-config", async (
            SetupCompletionService setupService,
            CancellationToken ct) =>
        {
            logger.LogDebug("Loading LLM configuration");
            var config = await ReadUserLlmConfigAsync(setupService.ConfigPath, ct).ConfigureAwait(false);
            logger.LogDebug("Loaded LLM configuration: {ProviderCount} providers, default={DefaultProvider}",
                config.Providers.Count, config.DefaultProvider);
            return Results.Ok(config);
        }).RequireAuthorization();

        system.MapPut("/llm-config", async (
            UpdateLlmConfigRequest? request,
            SetupCompletionService setupService,
            IConfiguration configuration,
            AgentDirectory agentDirectory,
            CancellationToken ct) =>
        {
            if (request is null)
            {
                logger.LogWarning("LLM config update rejected: request body is null");
                return Results.BadRequest(new { error = "Request body is required." });
            }

            logger.LogInformation("Updating LLM configuration: {ProviderCount} providers, default={DefaultProvider}",
                request.Providers?.Count ?? 0, request.DefaultProvider);

            var defaultProvider = NormalizeProvider(request.DefaultProvider);
            if (defaultProvider is null)
            {
                logger.LogWarning("LLM config update rejected: DefaultProvider is required");
                return Results.BadRequest(new { error = "DefaultProvider is required." });
            }

            if (request.Providers is null || request.Providers.Count == 0)
            {
                logger.LogWarning("LLM config update rejected: no providers supplied");
                return Results.BadRequest(new { error = "At least one provider configuration is required." });
            }

            var validated = new List<ValidatedProviderInstance>(request.Providers.Count);

            // Read existing instances so we can preserve API keys when the frontend omits them.
            var existingInstances = await ReadUserLlmConfigInstancesAsync(setupService.ConfigPath, ct)
                .ConfigureAwait(false);

            foreach (var entry in request.Providers)
            {
                var name = NormalizeProvider(entry.Name);
                if (name is null)
                {
                    return Results.BadRequest(new { error = "Each provider must have a name." });
                }

#pragma warning disable CS0618 // Obsolete ModelId -> DefaultModel compat
                var defaultModel = NormalizeModelId(entry.DefaultModel ?? entry.ModelId);
#pragma warning restore CS0618
                if (defaultModel is null)
                {
                    return Results.BadRequest(new { error = $"DefaultModel is required for provider '{name}'." });
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

                var availableModels = entry.AvailableModels;
                var apiKey = string.IsNullOrWhiteSpace(entry.ApiKey) ? null : entry.ApiKey.Trim();

                // When the frontend doesn't supply an API key, preserve the one already on disk.
                if (apiKey is null)
                {
                    var existingInstance = existingInstances.Find(i =>
                        string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (existingInstance is not null && !string.IsNullOrWhiteSpace(existingInstance.ApiKey))
                    {
                        apiKey = existingInstance.ApiKey;
                    }
                }

                validated.Add(new ValidatedProviderInstance(name, displayName, type, defaultModel, endpoint, apiKey, availableModels));
            }

            if (!validated.Exists(p => string.Equals(p.Name, defaultProvider, StringComparison.OrdinalIgnoreCase)))
            {
                return Results.BadRequest(new { error = $"DefaultProvider '{defaultProvider}' must be included in the providers list." });
            }

            // Prevent removal of providers that are in use by agents.
            var currentConfig = await ReadUserLlmConfigAsync(setupService.ConfigPath, ct).ConfigureAwait(false);
            var currentNames = new HashSet<string>(
                currentConfig.Providers.Select(p => p.Name),
                StringComparer.OrdinalIgnoreCase);
            var newNames = new HashSet<string>(
                validated.Select(p => p.Name),
                StringComparer.OrdinalIgnoreCase);
            var removedNames = currentNames.Except(newNames).ToList();

            if (removedNames.Count > 0)
            {
                var agents = agentDirectory.DiscoverAgents();
                var inUseProviders = new List<string>();

                foreach (var removed in removedNames)
                {
                    var usedBy = agents
                        .Where(a => a.LlmConfig?.Provider is not null &&
                                    string.Equals(a.LlmConfig.Provider, removed, StringComparison.OrdinalIgnoreCase))
                        .Select(a => a.Name)
                        .ToList();

                    if (usedBy.Count > 0)
                    {
                        inUseProviders.Add($"'{removed}' is used by agent(s): {string.Join(", ", usedBy)}");
                    }
                }

                if (inUseProviders.Count > 0)
                {
                    return Results.BadRequest(new
                    {
                        error = "Cannot remove providers that are in use by agents. " + string.Join(" ", inUseProviders),
                        details = inUseProviders,
                    });
                }
            }

            try
            {
                await WriteMultiProviderConfigurationAsync(setupService.ConfigPath, defaultProvider, validated, ct)
                    .ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Failed to write LLM configuration (I/O error)");
                return Results.Json(
                    new { error = $"Failed to write configuration: {ex.Message}" },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, "Failed to write LLM configuration (access denied)");
                return Results.Json(
                    new { error = $"Failed to update configuration: {ex.Message}" },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Failed to update LLM configuration (invalid operation)");
                return Results.Json(
                    new { error = $"Failed to update configuration: {ex.Message}" },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to parse LLM configuration file");
                return Results.Json(
                    new { error = $"Failed to parse configuration: {ex.Message}" },
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            setupService.MarkComplete();
            logger.LogInformation("LLM configuration updated successfully: {ProviderCount} providers, default={DefaultProvider}",
                validated.Count, defaultProvider);

            if (configuration is IConfigurationRoot configurationRoot)
            {
                configurationRoot.Reload();
            }

            var updatedConfiguration = await ReadUserLlmConfigAsync(setupService.ConfigPath, ct).ConfigureAwait(false);
            return Results.Ok(new UpdateLlmConfigResponse(
                Updated: true,
                RequiresRestart: false,
                Message: "LLM configuration updated successfully. Changes are active immediately.",
                Configuration: updatedConfiguration));
        }).RequireAuthorization();

        system.MapGet("/llm-usage", async (
            IDbContextFactory<OpenVepaDbContext> dbFactory,
            DateTimeOffset? since,
            string? provider,
            CancellationToken ct) =>
        {
            logger.LogDebug("Fetching LLM usage stats: since={Since}, provider={Provider}", since, provider);
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

        system.MapGet("/models", async (HttpContext context, SetupCompletionService setupService, CancellationToken ct) =>
        {
            var provider = context.Request.Query["provider"].ToString();
            if (string.IsNullOrWhiteSpace(provider))
            {
                return Results.BadRequest(new { error = "Missing required parameter: provider" });
            }

            logger.LogDebug("Fetching models for provider={Provider}", provider);

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

            // If no API key provided, try to read from configuration file.
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var instances = await ReadUserLlmConfigInstancesAsync(setupService.ConfigPath, ct).ConfigureAwait(false);
                var instance = instances.Find(i =>
                    string.Equals(i.Type, provider, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(i.Name, provider, StringComparison.OrdinalIgnoreCase));
                if (instance is not null)
                {
                    apiKey = instance.ApiKey ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(endpoint))
                    {
                        endpoint = instance.Endpoint ?? string.Empty;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = ModelListProxy.GetDefaultEndpoint(provider);
            }

            endpoint = endpoint.TrimEnd('/');

            var result = await ModelListProxy.FetchModelsWithStatusAsync(
                provider, apiKey, endpoint, context.RequestAborted).ConfigureAwait(false);

            if (result.Success)
            {
                logger.LogDebug("Fetched {ModelCount} models for provider={Provider}", result.Models.Count, provider);
            }
            else
            {
                logger.LogWarning("Failed to fetch models for provider={Provider}: {Error}", provider, result.Error);
            }

            return Results.Ok(new
            {
                success = result.Success,
                models = result.Models,
                provider = result.Provider,
                endpoint = result.Endpoint,
                error = result.Error,
                diagnostics = result.Diagnostics,
            });
        }).RequireAuthorization();

        system.MapPost("/restart", async (
            IHostApplicationLifetime lifetime,
            CancellationToken ct) =>
        {
            logger.LogWarning("Application restart requested");

            // Return response first, then trigger shutdown after a short delay
            // so the HTTP response can be sent before the app stops.
            _ = Task.Run(async () =>
            {
                await Task.Delay(1500, CancellationToken.None).ConfigureAwait(false);
                lifetime.StopApplication();
            }, CancellationToken.None);

            return Results.Ok(new
            {
                message = "Service is restarting. Please wait a moment...",
                restarting = true,
            });
        }).RequireAuthorization();

        system.MapGet("/diagnostics", (
            IOptions<ProviderOptions> providerOptions,
            IOptions<AgentOptions> agentOptions,
            AgentDirectory agentDirectory,
            SetupCompletionService setupService) =>
        {
            const string googleDefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta";

            var provider = providerOptions.Value;
            var instances = provider.Instances?.Select(i => new
            {
                name = i.Name,
                type = i.Type,
                defaultModel = i.DefaultModel,
                endpoint = i.Endpoint,
                hasApiKey = !string.IsNullOrWhiteSpace(i.ApiKey),
                availableModelsCount = i.AvailableModels?.Count ?? 0,
            }).ToList();

            // Resolve Google endpoint exactly as CreateGoogleClient does.
            object? resolvedGoogle = null;
            var googleInstance = provider.Instances?.FirstOrDefault(
                i => string.Equals(i.Type, "google", StringComparison.OrdinalIgnoreCase));

            if (googleInstance is not null)
            {
                var rawEndpoint = googleInstance.Endpoint;
                var isNullOrWhiteSpace = string.IsNullOrWhiteSpace(rawEndpoint);

                var resolved = isNullOrWhiteSpace
                    ? googleDefaultEndpoint
                    : rawEndpoint!.TrimEnd('/');

                var openAiCompatEndpoint = resolved.EndsWith("/openai", StringComparison.OrdinalIgnoreCase)
                    ? resolved
                    : resolved + "/openai";

                resolvedGoogle = new
                {
                    rawEndpoint,
                    isNullOrWhiteSpace,
                    resolvedEndpoint = resolved,
                    openAiCompatEndpoint,
                    expectedChatUrl = openAiCompatEndpoint + "/chat/completions",
                };
            }

            // Agent summaries.
            var agents = agentDirectory.DiscoverAgents()
                .Select(a => new
                {
                    name = a.Name,
                    provider = a.LlmConfig?.Provider,
                    model = a.LlmConfig?.Model,
                })
                .ToList();

            var configPath = setupService.ConfigPath;

            return Results.Ok(new
            {
                providerConfig = new
                {
                    defaultProvider = provider.DefaultProvider,
                    defaultModel = provider.DefaultModel,
                    instanceCount = provider.Instances?.Count ?? 0,
                    instances = instances ?? [],
                    legacyOpenAi = provider.OpenAi is not null,
                    legacyOllama = provider.Ollama is not null,
                },
                resolvedGoogle,
                agentConfig = new
                {
                    agentsDirectory = agentOptions.Value.AgentsDirectory,
                    defaultAgent = agentOptions.Value.DefaultAgent,
                    agents,
                },
                systemInfo = new
                {
                    configFilePath = configPath,
                    configFileExists = File.Exists(configPath),
                },
            });
        }).RequireAuthorization();

        // Test-chat endpoint: uses the exact production SDK path to send a minimal request.
        system.MapPost("/test-chat", async (
            IOptions<ProviderOptions> providerOptions,
            ILoggerFactory loggerFactory,
            string? provider,
            CancellationToken ct) =>
        {
            var testLogger = loggerFactory.CreateLogger("TestChat");
            var options = providerOptions.Value;

            var providerName = !string.IsNullOrWhiteSpace(provider)
                ? provider
                : options.DefaultProvider?.ToLowerInvariant() ?? "ollama";
            var instance = options.Instances?.Find(
                i => string.Equals(i.Name, providerName, StringComparison.OrdinalIgnoreCase));

            if (instance is null)
            {
                return Results.Ok(new
                {
                    success = false,
                    error = $"Default provider '{providerName}' not found in configured instances.",
                    availableInstances = options.Instances?.Select(i => i.Name).ToList() ?? [],
                });
            }

#pragma warning disable CS0618
            var effectiveModel = instance.DefaultModel;
#pragma warning restore CS0618

            testLogger.LogInformation(
                "Test-chat: provider={Provider}, type={Type}, model={Model}, endpoint={Endpoint}",
                instance.Name, instance.Type, effectiveModel ?? "(null)", instance.Endpoint ?? "(null)");

            try
            {
                var auditLogger = default(Func<Core.Audit.LlmAuditLogEntry, Task>);
                var client = ProviderServiceExtensions.CreateFromInstance(instance, effectiveModel, auditLogger, testLogger);

                testLogger.LogInformation("Test-chat: client created ({ClientType}), sending test message...", client.GetType().Name);

                var response = await client.GetResponseAsync("Say hello in one word.", cancellationToken: ct)
                    .ConfigureAwait(false);

                testLogger.LogInformation("Test-chat: success! Response length={Length}", response.Text?.Length ?? 0);

                return Results.Ok(new
                {
                    success = true,
                    provider = instance.Name,
                    type = instance.Type,
                    model = effectiveModel,
                    endpoint = instance.Endpoint,
                    responsePreview = response.Text?.Length > 200 ? response.Text[..200] + "..." : response.Text,
                    inputTokens = response.Usage?.InputTokenCount,
                    outputTokens = response.Usage?.OutputTokenCount,
                });
            }
            catch (ClientResultException cre)
            {
                testLogger.LogError(cre, "Test-chat failed with status {Status}", cre.Status);

                return Results.Ok(new
                {
                    success = false,
                    provider = instance.Name,
                    type = instance.Type,
                    model = effectiveModel,
                    endpoint = instance.Endpoint,
                    httpStatus = cre.Status,
                    error = cre.Message,
                    hint = cre.Status switch
                    {
                        401 or 403 => "API key is invalid or missing permissions.",
                        404 => "The endpoint URL or model name is wrong. Check that the model exists at this provider.",
                        429 => "Rate limited. Wait and try again.",
                        _ => $"HTTP {cre.Status} from the LLM provider.",
                    },
                });
            }
            catch (Exception ex)
            {
                testLogger.LogError(ex, "Test-chat failed");

                return Results.Ok(new
                {
                    success = false,
                    provider = instance.Name,
                    type = instance.Type,
                    model = effectiveModel,
                    endpoint = instance.Endpoint,
                    error = ex.Message,
                    exceptionType = ex.GetType().Name,
                });
            }
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
            var defaultModel = instance["defaultModel"]?.GetValue<string>()
                ?? instance["modelId"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(defaultModel))
            {
                continue;
            }

            var endpoint = instance["endpoint"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = ModelListProxy.GetDefaultEndpoint(type);
            }

            var availableModels = new List<string>();
            if (instance["availableModels"] is JsonArray modelsArray)
            {
                foreach (var m in modelsArray)
                {
                    var modelName = m?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(modelName))
                    {
                        availableModels.Add(modelName);
                    }
                }
            }

            var hasApiKey = !string.IsNullOrWhiteSpace(instance["apiKey"]?.GetValue<string>());
            providers.Add(new LlmProviderEntry(name, displayName, type, defaultModel, endpoint, availableModels, hasApiKey));
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

            var hasApiKey = !string.IsNullOrWhiteSpace(section["ApiKey"]?.GetValue<string>());
            providers.Add(new LlmProviderEntry(providerName, sectionName, providerName, modelId, endpoint, [], hasApiKey));
        }

        return new LlmMultiProviderResponse(defaultProvider, providers);
    }

    /// <summary>
    /// Reads raw LLM config instances including API keys from the configuration file.
    /// Used internally when the models endpoint needs to resolve credentials.
    /// </summary>
    private static async Task<List<LlmConfigInstanceWithKey>> ReadUserLlmConfigInstancesAsync(
        string configPath,
        CancellationToken ct)
    {
        var result = new List<LlmConfigInstanceWithKey>();

        if (!File.Exists(configPath))
        {
            return result;
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(configPath, ct).ConfigureAwait(false);
        }
        catch (IOException)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return result;
        }

        if (root?["Providers"] is not JsonObject providersNode)
        {
            return result;
        }

        if (providersNode["Instances"] is JsonArray instancesArray)
        {
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
                var endpoint = instance["endpoint"]?.GetValue<string>();
                var apiKey = instance["apiKey"]?.GetValue<string>();

                result.Add(new LlmConfigInstanceWithKey(name, type, endpoint, apiKey));
            }
        }
        else
        {
            // Legacy format: read API keys from known sections.
            foreach (var (sectionName, providerName) in KnownProviderMappings)
            {
                if (providersNode[sectionName] is not JsonObject section)
                {
                    continue;
                }

                var endpoint = section["Endpoint"]?.GetValue<string>();
                var apiKey = section["ApiKey"]?.GetValue<string>();
                result.Add(new LlmConfigInstanceWithKey(providerName, providerName, endpoint, apiKey));
            }
        }

        return result;
    }

    /// <summary>Internal record for config instances that includes the API key.</summary>
    private sealed record LlmConfigInstanceWithKey(string Name, string Type, string? Endpoint, string? ApiKey);

    /// <summary>Validated provider instance for writing configuration.</summary>
    private readonly record struct ValidatedProviderInstance(
        string Name,
        string DisplayName,
        string Type,
        string DefaultModel,
        string Endpoint,
        string? ApiKey,
        IReadOnlyList<string>? AvailableModels);

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
                ["defaultModel"] = p.DefaultModel,
            };

            if (!string.IsNullOrWhiteSpace(p.ApiKey))
            {
                instance["apiKey"] = p.ApiKey;
            }

            if (p.AvailableModels is { Count: > 0 })
            {
                var modelsArray = new JsonArray();
                foreach (var m in p.AvailableModels)
                {
                    modelsArray.Add(m);
                }

                instance["availableModels"] = modelsArray;
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
internal sealed record LlmProviderEntry(
    string Name,
    string DisplayName,
    string Type,
    string DefaultModel,
    string? Endpoint,
    IReadOnlyList<string> AvailableModels,
    bool HasApiKey = false)
{
    /// <summary>Backward compatibility alias for <see cref="DefaultModel"/>.</summary>
    [Obsolete("Use DefaultModel instead.")]
    public string ModelId => DefaultModel;
}

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

    /// <summary>Gets the default model for this provider.</summary>
    public string? DefaultModel { get; init; }

    /// <summary>Gets the legacy model identifier. Use <see cref="DefaultModel"/> instead.</summary>
    [Obsolete("Use DefaultModel instead.")]
    public string? ModelId { get; init; }

    /// <summary>Gets the provider endpoint URL.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Gets the API key (optional).</summary>
    public string? ApiKey { get; init; }

    /// <summary>Gets the available models for this provider instance.</summary>
    public IReadOnlyList<string>? AvailableModels { get; init; }
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
