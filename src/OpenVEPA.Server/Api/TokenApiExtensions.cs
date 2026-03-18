using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenVEPA.Storage;

namespace OpenVEPA.Server.Api;

/// <summary>Maps REST API endpoints for access token management.</summary>
internal static class TokenApiExtensions
{
    private const string CreateTokenMessage = "Store this token securely. It will not be shown again.";
    private const string BootstrapForbiddenMessage = "Bootstrap token creation is disabled because tokens already exist.";
    private const string CurrentTokenRevokeMessage = "Cannot revoke your current token.";
    private const string TokenIdRequiredMessage = "Token id is required.";

    /// <summary>
    /// Maps token management endpoints under <c>/api/tokens</c>.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    internal static WebApplication MapTokenApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/tokens/bootstrap", async (
            TokenCreateRequest? request,
            SqliteTokenStore tokenStore,
            CancellationToken ct) =>
        {
            if (!TryNormalizeCreateRequest(request, out var name, out var error))
            {
                return Results.BadRequest(new TokenErrorResponse(error));
            }

            if (!await CanBootstrapAsync(tokenStore, ct).ConfigureAwait(false))
            {
                return Results.Json(
                    new TokenErrorResponse(BootstrapForbiddenMessage),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var response = await CreateTokenResponseAsync(tokenStore, name, ct).ConfigureAwait(false);
            return Results.Created($"/api/tokens/{response.TokenId}", response);
        });

        RouteGroupBuilder tokens = app.MapGroup("/api/tokens")
            .RequireAuthorization();

        tokens.MapPost(string.Empty, async (
            TokenCreateRequest? request,
            SqliteTokenStore tokenStore,
            CancellationToken ct) =>
        {
            if (!TryNormalizeCreateRequest(request, out var name, out var error))
            {
                return Results.BadRequest(new TokenErrorResponse(error));
            }

            var response = await CreateTokenResponseAsync(tokenStore, name, ct).ConfigureAwait(false);
            return Results.Created($"/api/tokens/{response.TokenId}", response);
        });

        tokens.MapGet(string.Empty, async (SqliteTokenStore tokenStore, CancellationToken ct) =>
        {
            var tokenItems = await tokenStore.ListTokensAsync(ct).ConfigureAwait(false);
            var response = new TokenListResponse(tokenItems
                .Select(static token => new TokenSummaryResponse(
                    token.Id,
                    token.Name,
                    token.CreatedAt,
                    token.LastUsedAt,
                    token.IsRevoked))
                .ToArray());

            return Results.Ok(response);
        });

        tokens.MapDelete("/{id}", async (
            string id,
            HttpContext context,
            SqliteTokenStore tokenStore,
            CancellationToken ct) =>
        {
            var normalizedId = NormalizeRequiredValue(id);
            if (normalizedId is null)
            {
                return Results.BadRequest(new TokenErrorResponse(TokenIdRequiredMessage));
            }

            var currentTokenId = GetCurrentTokenId(context.User);
            if (string.Equals(currentTokenId, normalizedId, StringComparison.Ordinal))
            {
                return Results.BadRequest(new TokenErrorResponse(CurrentTokenRevokeMessage));
            }

            await tokenStore.RevokeTokenAsync(normalizedId, ct).ConfigureAwait(false);
            return Results.Ok(new TokenMessageResponse("Token revoked."));
        });

        return app;
    }

    private static async Task<bool> CanBootstrapAsync(SqliteTokenStore tokenStore, CancellationToken ct)
    {
        var tokens = await tokenStore.ListTokensAsync(ct).ConfigureAwait(false);
        return tokens.Count == 0;
    }

    private static async Task<TokenCreateResponse> CreateTokenResponseAsync(
        SqliteTokenStore tokenStore,
        string name,
        CancellationToken ct)
    {
        var result = await tokenStore.CreateTokenAsync(name, ct).ConfigureAwait(false);
        return new TokenCreateResponse(result.TokenId, result.PlaintextToken, name, CreateTokenMessage);
    }

    private static string? GetCurrentTokenId(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst("token_id")?.Value;
    }

    private static string? NormalizeRequiredValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool TryNormalizeCreateRequest(
        TokenCreateRequest? request,
        out string name,
        out string error)
    {
        name = string.Empty;

        if (request is null)
        {
            error = "Request body is required.";
            return false;
        }

        var normalizedName = NormalizeRequiredValue(request.Name);
        if (normalizedName is null)
        {
            error = "Name is required.";
            return false;
        }

        name = normalizedName;
        error = string.Empty;
        return true;
    }
}

/// <summary>Represents a request to create an access token.</summary>
internal sealed record TokenCreateRequest
{
    /// <summary>Gets the descriptive name for the new token.</summary>
    public string? Name { get; init; }
}

/// <summary>Represents the response body returned after creating an access token.</summary>
/// <param name="TokenId">The server-generated token identifier.</param>
/// <param name="Token">The plaintext bearer token shown only once.</param>
/// <param name="Name">The descriptive token name.</param>
/// <param name="Message">The warning message shown to clients.</param>
internal sealed record TokenCreateResponse(
    string TokenId,
    string Token,
    string Name,
    string Message);

/// <summary>Represents the response body returned when listing access tokens.</summary>
/// <param name="Tokens">The stored access tokens as metadata-only summaries.</param>
internal sealed record TokenListResponse(IReadOnlyList<TokenSummaryResponse> Tokens);

/// <summary>Represents token metadata returned by the list endpoint.</summary>
/// <param name="Id">The token identifier.</param>
/// <param name="Name">The descriptive token name.</param>
/// <param name="CreatedAt">The UTC time when the token was created.</param>
/// <param name="LastUsedAt">The UTC time when the token was last used, if any.</param>
/// <param name="IsRevoked">A value indicating whether the token has been revoked.</param>
internal sealed record TokenSummaryResponse(
    string Id,
    string Name,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    bool IsRevoked);

/// <summary>Represents a message-only token API response.</summary>
/// <param name="Message">The response message.</param>
internal sealed record TokenMessageResponse(string Message);

/// <summary>Represents an error returned by the token API.</summary>
/// <param name="Error">The error message.</param>
internal sealed record TokenErrorResponse(string Error);
