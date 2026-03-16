using OpenVEPA.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Lists all API tokens in a formatted table.
/// </summary>
internal sealed class TokenListCommand : AsyncCommand
{
    private readonly SqliteTokenStore _tokenStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenListCommand"/> class.
    /// </summary>
    /// <param name="tokenStore">The token store for querying API tokens.</param>
    public TokenListCommand(SqliteTokenStore tokenStore)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var tokens = await _tokenStore.ListTokensAsync(CancellationToken.None)
            .ConfigureAwait(false);

        if (tokens.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No tokens created.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]API Tokens[/]")
            .AddColumn("[bold]ID[/]")
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Created[/]")
            .AddColumn("[bold]Last Used[/]")
            .AddColumn("[bold]Revoked[/]");

        foreach (var token in tokens)
        {
            var revokedText = token.IsRevoked ? "[red]Yes[/]" : "[green]No[/]";
            var lastUsed = token.LastUsedAt?.ToString("yyyy-MM-dd HH:mm") ?? "[dim]Never[/]";

            table.AddRow(
                Markup.Escape(token.Id),
                Markup.Escape(token.Name),
                token.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                lastUsed,
                revokedText);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
