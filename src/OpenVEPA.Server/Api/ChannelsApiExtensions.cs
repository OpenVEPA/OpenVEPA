using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace OpenVEPA.Server.Api;

/// <summary>Maps authenticated REST API endpoints for channel configuration.</summary>
internal static class ChannelsApiExtensions
{
    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Maps authenticated REST endpoints under <c>/api/channels</c>.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapChannelsApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder channels = app.MapGroup("/api/channels")
            .RequireAuthorization();

        channels.MapGet(string.Empty, (IConfiguration configuration) =>
        {
            var result = KnownChannels
                .Select(definition => BuildChannelSummary(definition, configuration))
                .ToArray();

            return Results.Ok(new { channels = result });
        });

        channels.MapGet("/{id}", (string id, IConfiguration configuration) =>
        {
            var definition = FindChannel(id);
            if (definition is null)
            {
                return Results.NotFound(new { error = $"Unknown channel '{id}'." });
            }

            return Results.Ok(BuildChannelDetail(definition, configuration));
        });

        channels.MapPut("/{id}", async (
            string id,
            JsonElement body,
            SetupCompletionService setupService,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            var definition = FindChannel(id);
            if (definition is null)
            {
                return Results.NotFound(new { error = $"Unknown channel '{id}'." });
            }

            try
            {
                await WriteChannelConfigAsync(definition, body, setupService.ConfigPath, ct)
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

            if (configuration is IConfigurationRoot configurationRoot)
            {
                configurationRoot.Reload();
            }

            return Results.Ok(new
            {
                updated = true,
                requiresRestart = true,
                message = $"Channel '{definition.Name}' configuration saved. Restart the application for changes to take effect.",
            });
        });

        return app;
    }

    // ------------------------------------------------------------------
    // Known channel definitions
    // ------------------------------------------------------------------

    private static readonly ChannelDefinition[] KnownChannels =
    [
        new("telegram", "Telegram", "Telegram Bot API integration for messaging.",
            "\U0001F4F1", ["BotToken"]),
        new("whatsapp", "WhatsApp", "WhatsApp Business API integration (coming soon).",
            "\U0001F4AC", []),
        new("discord", "Discord", "Discord bot integration for server messaging.",
            "\U0001F3AE", ["BotToken", "GuildId"]),
        new("email", "Email", "SMTP email integration for sending and receiving messages.",
            "\U0001F4E7", ["SmtpHost", "SmtpPort", "Username", "Password"]),
    ];

    private static ChannelDefinition? FindChannel(string id) =>
        KnownChannels.FirstOrDefault(channel =>
            string.Equals(channel.Id, id, StringComparison.OrdinalIgnoreCase));

    // ------------------------------------------------------------------
    // Response builders
    // ------------------------------------------------------------------

    private static object BuildChannelSummary(ChannelDefinition definition, IConfiguration configuration)
    {
        var section = configuration.GetSection($"Channels:{definition.SectionName}");
        var enabled = string.Equals(section["Enabled"], "true", StringComparison.OrdinalIgnoreCase);
        var configured = definition.RequiredFields.Length == 0 ||
                         definition.RequiredFields.All(field => !string.IsNullOrWhiteSpace(section[field]));

        return new
        {
            id = definition.Id,
            name = definition.Name,
            description = definition.Description,
            enabled,
            configured,
            icon = definition.Icon,
        };
    }

    private static object BuildChannelDetail(ChannelDefinition definition, IConfiguration configuration)
    {
        var section = configuration.GetSection($"Channels:{definition.SectionName}");
        var enabled = string.Equals(section["Enabled"], "true", StringComparison.OrdinalIgnoreCase);

        var fields = new Dictionary<string, object?>();
        foreach (var field in definition.AllFields)
        {
            var raw = section[field];
            fields[ToCamelCase(field)] = IsSensitiveField(field) ? MaskSecret(raw) : raw ?? string.Empty;
        }

        return new
        {
            id = definition.Id,
            name = definition.Name,
            description = definition.Description,
            icon = definition.Icon,
            enabled,
            fields,
        };
    }

    // ------------------------------------------------------------------
    // Configuration persistence
    // ------------------------------------------------------------------

    private static async Task WriteChannelConfigAsync(
        ChannelDefinition definition,
        JsonElement body,
        string configPath,
        CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        JsonObject root = await LoadConfigurationRootAsync(configPath, ct).ConfigureAwait(false);
        JsonObject channelsNode = EnsureChildObject(root, "Channels");
        JsonObject channelNode = EnsureChildObject(channelsNode, definition.SectionName);

        if (body.TryGetProperty("enabled", out var enabledElement))
        {
            channelNode["Enabled"] = enabledElement.GetBoolean();
        }

        foreach (var field in definition.AllFields)
        {
            var camelName = ToCamelCase(field);
            if (body.TryGetProperty(camelName, out var element))
            {
                channelNode[field] = element.ValueKind switch
                {
                    JsonValueKind.Number => element.GetInt32(),
                    JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
                    _ => element.GetString(),
                };
            }
        }

        var json = root.ToJsonString(JsonWriteOptions);
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

        var parsed = JsonNode.Parse(json) as JsonObject;
        return parsed ?? throw new InvalidOperationException(
            "The configuration file must contain a JSON object at the root.");
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

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static bool IsSensitiveField(string field) =>
        field.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
        field.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        field.Contains("Secret", StringComparison.OrdinalIgnoreCase);

    private static string MaskSecret(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : "••••••••";

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];

    // ------------------------------------------------------------------
    // Channel definition
    // ------------------------------------------------------------------

    private sealed record ChannelDefinition(
        string Id,
        string Name,
        string Description,
        string Icon,
        string[] RequiredFields)
    {
        /// <summary>The appsettings.json section name (PascalCase).</summary>
        public string SectionName { get; } =
            char.ToUpperInvariant(Id[0]) + Id[1..];

        /// <summary>All configurable fields for this channel type.</summary>
        public string[] AllFields { get; } = Id.ToLowerInvariant() switch
        {
            "telegram" => ["BotToken", "WebhookUrl"],
            "discord" => ["BotToken", "GuildId"],
            "email" => ["SmtpHost", "SmtpPort", "Username", "Password", "FromAddress"],
            _ => RequiredFields,
        };
    }
}
