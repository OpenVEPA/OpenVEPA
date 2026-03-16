using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenVEPA.Storage;

namespace OpenVEPA.Server.Auth;

/// <summary>
/// Custom authentication handler that validates bearer tokens against the
/// <see cref="SqliteTokenStore"/>. Supports both the Authorization header
/// and the <c>access_token</c> query parameter (required for SignalR WebSocket handshake).
/// </summary>
public sealed class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Authentication scheme name used for registration.</summary>
    public const string SchemeName = "OpenVepaToken";

    private readonly SqliteTokenStore _tokenStore;

    public TokenAuthenticationHandler(
        SqliteTokenStore tokenStore,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ExtractToken();
        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        var tokenId = await _tokenStore
            .ValidateTokenAsync(token, Context.RequestAborted)
            .ConfigureAwait(false);

        if (tokenId is null)
        {
            return AuthenticateResult.Fail("Invalid or revoked token.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, tokenId),
            new Claim("token_id", tokenId),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    private string? ExtractToken()
    {
        // 1. Try Authorization: Bearer <token> header.
        var authorization = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization) &&
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization["Bearer ".Length..].Trim();
        }

        // 2. Fall back to query parameter (SignalR WebSocket handshake).
        if (Request.Query.TryGetValue("access_token", out var accessToken))
        {
            return accessToken.ToString();
        }

        return null;
    }
}
