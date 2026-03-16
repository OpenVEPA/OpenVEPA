using System.ComponentModel;
using OpenVEPA.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Revokes an API token by its identifier.
/// </summary>
internal sealed class TokenRevokeCommand : AsyncCommand<TokenRevokeCommand.Settings>
{
    private readonly SqliteTokenStore _tokenStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRevokeCommand"/> class.
    /// </summary>
    /// <param name="tokenStore">The token store for managing API tokens.</param>
    public TokenRevokeCommand(SqliteTokenStore tokenStore)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await _tokenStore.RevokeTokenAsync(settings.Id, CancellationToken.None)
            .ConfigureAwait(false);

        AnsiConsole.MarkupLine($"[green]Token revoked:[/] {Markup.Escape(settings.Id)}");
        return 0;
    }

    /// <summary>
    /// Settings for the token revoke command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the token identifier to revoke.
        /// </summary>
        [CommandArgument(0, "<id>")]
        [Description("The token ID to revoke")]
        public string Id { get; init; } = "";
    }
}
