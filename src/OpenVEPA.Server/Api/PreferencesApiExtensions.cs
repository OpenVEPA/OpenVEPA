using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenVEPA.Core.Preferences;

namespace OpenVEPA.Server.Api;

/// <summary>Maps REST API endpoints for user preferences.</summary>
internal static class PreferencesApiExtensions
{
    /// <summary>
    /// Maps authenticated REST endpoints under <c>/api/preferences</c>.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapPreferencesApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder preferences = app.MapGroup("/api/preferences")
            .RequireAuthorization();

        preferences.MapGet(string.Empty, async (IUserProfileService userProfileService, CancellationToken ct) =>
        {
            var response = await GetPreferencesByCategoryAsync(userProfileService, ct).ConfigureAwait(false);
            return Results.Ok(response);
        });

        preferences.MapGet("/{category}", async (
            string category,
            IUserProfileService userProfileService,
            CancellationToken ct) =>
        {
            var normalizedCategory = NormalizeRequiredValue(category);
            if (normalizedCategory is null)
            {
                return Results.BadRequest(new { error = "Category is required." });
            }

            var allPreferences = await userProfileService.GetAllPreferencesAsync(ct).ConfigureAwait(false);
            var response = allPreferences
                .Where(preference => string.Equals(
                    preference.Category,
                    normalizedCategory,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(preference => preference.Key, StringComparer.Ordinal)
                .ToDictionary(
                    preference => preference.Key,
                    static preference => ToResponse(preference),
                    StringComparer.Ordinal);

            return Results.Ok(response);
        });

        preferences.MapPost(string.Empty, async (
            SetPreferenceRequest? request,
            IUserProfileService userProfileService,
            CancellationToken ct) =>
        {
            if (!TryNormalizeRequest(request, out var normalizedRequest, out var error))
            {
                return Results.BadRequest(new { error });
            }

            await userProfileService.SetExplicitPreferenceAsync(
                    normalizedRequest.Key,
                    normalizedRequest.Value,
                    normalizedRequest.Category,
                    ct)
                .ConfigureAwait(false);

            return Results.Created(
                $"/api/preferences/{Uri.EscapeDataString(normalizedRequest.Key)}",
                new PreferenceWriteResponse(
                    normalizedRequest.Key,
                    normalizedRequest.Value,
                    normalizedRequest.Category));
        });

        preferences.MapPut("/{key}", async (
            string key,
            UpdatePreferenceRequest? request,
            IUserProfileService userProfileService,
            CancellationToken ct) =>
        {
            var normalizedKey = NormalizeRequiredValue(key);
            if (normalizedKey is null)
            {
                return Results.BadRequest(new { error = "Key is required." });
            }

            if (!TryNormalizeRequest(normalizedKey, request, out var normalizedRequest, out var error))
            {
                return Results.BadRequest(new { error });
            }

            var existingPreference = await GetPreferenceAsync(userProfileService, normalizedKey, ct)
                .ConfigureAwait(false);
            if (existingPreference is null)
            {
                return Results.NotFound();
            }

            await userProfileService.SetExplicitPreferenceAsync(
                    normalizedKey,
                    normalizedRequest.Value,
                    normalizedRequest.Category,
                    ct)
                .ConfigureAwait(false);

            return Results.Ok(new PreferenceWriteResponse(
                normalizedKey,
                normalizedRequest.Value,
                normalizedRequest.Category));
        });

        preferences.MapDelete("/{key}", async (
            string key,
            IUserProfileService userProfileService,
            CancellationToken ct) =>
        {
            var normalizedKey = NormalizeRequiredValue(key);
            if (normalizedKey is null)
            {
                return Results.BadRequest(new { error = "Key is required." });
            }

            var existingPreference = await GetPreferenceAsync(userProfileService, normalizedKey, ct)
                .ConfigureAwait(false);
            if (existingPreference is null)
            {
                return Results.NotFound();
            }

            await userProfileService.DeletePreferenceAsync(normalizedKey, ct).ConfigureAwait(false);
            return Results.NoContent();
        });

        return app;
    }

    private static async Task<Dictionary<string, Dictionary<string, PreferenceApiEntryResponse>>> GetPreferencesByCategoryAsync(
        IUserProfileService userProfileService,
        CancellationToken ct)
    {
        var allPreferences = await userProfileService.GetAllPreferencesAsync(ct).ConfigureAwait(false);

        return allPreferences
            .GroupBy(preference => preference.Category, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(preference => preference.Key, StringComparer.Ordinal)
                    .ToDictionary(
                        preference => preference.Key,
                        static preference => ToResponse(preference),
                        StringComparer.Ordinal),
                StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<PreferenceEntry?> GetPreferenceAsync(
        IUserProfileService userProfileService,
        string key,
        CancellationToken ct)
    {
        var allPreferences = await userProfileService.GetAllPreferencesAsync(ct).ConfigureAwait(false);
        return allPreferences.FirstOrDefault(preference => string.Equals(preference.Key, key, StringComparison.Ordinal));
    }

    private static PreferenceApiEntryResponse ToResponse(PreferenceEntry preference)
    {
        return new PreferenceApiEntryResponse(
            preference.Key,
            preference.Value,
            preference.Category,
            preference.Source,
            preference.Confidence,
            preference.UpdatedAt);
    }

    private static string? NormalizeRequiredValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool TryNormalizeRequest(
        SetPreferenceRequest? request,
        out NormalizedPreferenceRequest normalizedRequest,
        out string error)
    {
        normalizedRequest = default;

        if (request is null)
        {
            error = "Request body is required.";
            return false;
        }

        var key = NormalizeRequiredValue(request.Key);
        if (key is null)
        {
            error = "Key is required.";
            return false;
        }

        var value = NormalizeRequiredValue(request.Value);
        if (value is null)
        {
            error = "Value is required.";
            return false;
        }

        var category = NormalizeRequiredValue(request.Category);
        if (category is null)
        {
            error = "Category is required.";
            return false;
        }

        normalizedRequest = new NormalizedPreferenceRequest(key, value, category);
        error = string.Empty;
        return true;
    }

    private static bool TryNormalizeRequest(
        string key,
        UpdatePreferenceRequest? request,
        out NormalizedPreferenceRequest normalizedRequest,
        out string error)
    {
        normalizedRequest = default;

        if (request is null)
        {
            error = "Request body is required.";
            return false;
        }

        var value = NormalizeRequiredValue(request.Value);
        if (value is null)
        {
            error = "Value is required.";
            return false;
        }

        var category = NormalizeRequiredValue(request.Category);
        if (category is null)
        {
            error = "Category is required.";
            return false;
        }

        normalizedRequest = new NormalizedPreferenceRequest(key, value, category);
        error = string.Empty;
        return true;
    }
}

/// <summary>Represents a request to create a user preference.</summary>
internal sealed record SetPreferenceRequest
{
    /// <summary>Gets the preference key.</summary>
    public string? Key { get; init; }

    /// <summary>Gets the preference value.</summary>
    public string? Value { get; init; }

    /// <summary>Gets the preference category.</summary>
    public string? Category { get; init; }
}

/// <summary>Represents a request to update a user preference.</summary>
internal sealed record UpdatePreferenceRequest
{
    /// <summary>Gets the updated preference value.</summary>
    public string? Value { get; init; }

    /// <summary>Gets the updated preference category.</summary>
    public string? Category { get; init; }
}

/// <summary>Represents a serialized user preference entry returned by the API.</summary>
internal sealed record PreferenceApiEntryResponse(
    string Key,
    string Value,
    string Category,
    PreferenceSource Source,
    double Confidence,
    DateTime UpdatedAt);

/// <summary>Represents the response body returned after creating or updating a preference.</summary>
internal sealed record PreferenceWriteResponse(
    string Key,
    string Value,
    string Category);

/// <summary>Represents a normalized preference write request.</summary>
internal readonly record struct NormalizedPreferenceRequest(
    string Key,
    string Value,
    string Category);
