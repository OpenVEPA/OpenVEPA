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

    private static readonly HashSet<string> OpenAiCompatibleProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "openai", "groq", "together", "perplexity", "azure",
        };

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
            IConfiguration configuration,
            ISessionStore sessionStore,
            CancellationToken ct) =>
        {
            var sessions = await sessionStore.ListSessionsAsync(ct).ConfigureAwait(false);
            var activeSessionsCount = sessions.Count(static session => session.Status == SessionStatus.Active);
            var llmConfiguration = GetSafeLlmConfiguration(configuration);

            return Results.Ok(new SystemStatusResponse(
                Version: GetApplicationVersion(),
                Uptime: GetUptime(),
                Provider: llmConfiguration.Provider,
                Model: llmConfiguration.ModelId,
                SetupComplete: setupService.IsSetupComplete,
                DatabasePath: ResolveDatabasePath(setupService.ConfigPath),
                ActiveSessionsCount: activeSessionsCount));
        }).RequireAuthorization();

        system.MapGet("/llm-config", (IConfiguration configuration) =>
            Results.Ok(GetSafeLlmConfiguration(configuration))).RequireAuthorization();

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

            var provider = NormalizeProvider(request.Provider);
            if (provider is null)
            {
                return Results.BadRequest(new { error = "Provider is required." });
            }

            var modelId = NormalizeModelId(request.ModelId);
            if (modelId is null)
            {
                return Results.BadRequest(new { error = "ModelId is required." });
            }

            var endpoint = NormalizeEndpoint(request.Endpoint);
            if (endpoint is null)
            {
                return Results.BadRequest(new { error = "Endpoint is required." });
            }

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            {
                return Results.BadRequest(new { error = "Endpoint must be a valid absolute URL." });
            }

            try
            {
                await WriteLlmConfigurationAsync(setupService.ConfigPath, provider, modelId, endpoint, ct)
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

            var updatedConfiguration = GetSafeLlmConfiguration(configuration);
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

    private static LlmConfigurationResponse GetSafeLlmConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = NormalizeProvider(configuration["Providers:DefaultProvider"]) ?? "unknown";
        var sectionName = GetProviderSectionName(provider);
        var modelId = configuration[$"Providers:{sectionName}:ModelId"]
            ?? configuration[$"Providers:{sectionName}:Model"]
            ?? configuration["Providers:DefaultModel"]
            ?? "unknown";
        var endpoint = configuration[$"Providers:{sectionName}:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = ModelListProxy.GetDefaultEndpoint(provider);
        }

        return new LlmConfigurationResponse(provider, modelId, endpoint);
    }

    private static async Task WriteLlmConfigurationAsync(
        string configPath,
        string provider,
        string modelId,
        string endpoint,
        CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        JsonObject root = await LoadConfigurationRootAsync(configPath, ct).ConfigureAwait(false);
        JsonObject providers = EnsureChildObject(root, "Providers");
        providers["DefaultProvider"] = provider;

        var sectionName = GetProviderSectionName(provider);
        JsonObject providerSection = EnsureChildObject(providers, sectionName);
        providerSection["ModelId"] = modelId;
        providerSection["Endpoint"] = endpoint;

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
        if (string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            return "Ollama";
        }

        if (OpenAiCompatibleProviders.Contains(provider))
        {
            return "OpenAi";
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

/// <summary>Represents the current safe LLM configuration.</summary>
internal sealed record LlmConfigurationResponse(string Provider, string ModelId, string? Endpoint);

/// <summary>Represents a request to update LLM runtime configuration.</summary>
internal sealed record UpdateLlmConfigRequest
{
    /// <summary>Gets the provider key.</summary>
    public string? Provider { get; init; }

    /// <summary>Gets the model identifier.</summary>
    public string? ModelId { get; init; }

    /// <summary>Gets the provider endpoint URL.</summary>
    public string? Endpoint { get; init; }
}

/// <summary>Represents the result of updating LLM configuration.</summary>
internal sealed record UpdateLlmConfigResponse(
    bool Updated,
    bool RequiresRestart,
    string Message,
    LlmConfigurationResponse Configuration);

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
