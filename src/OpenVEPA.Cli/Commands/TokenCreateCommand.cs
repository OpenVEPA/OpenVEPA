using System.ComponentModel;
using OpenVEPA.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Creates a new API token and displays the plaintext value once.
/// </summary>
internal sealed class TokenCreateCommand : AsyncCommand<TokenCreateCommand.Settings>
{
    private readonly SqliteTokenStore _tokenStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenCreateCommand"/> class.
    /// </summary>
    /// <param name="tokenStore">The token store for managing API tokens.</param>
    public TokenCreateCommand(SqliteTokenStore tokenStore)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var result = await _tokenStore.CreateTokenAsync(settings.Name, CancellationToken.None)
            .ConfigureAwait(false);

        AnsiConsole.MarkupLine(
            "[bold yellow]WARNING:[/] Store this token securely. It will not be shown again.");
        AnsiConsole.WriteLine();

        var panel = new Panel(Markup.Escape(result.PlaintextToken))
            .Header("[bold]API Token[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);

        AnsiConsole.Write(panel);
        AnsiConsole.MarkupLine($"[dim]Token ID:[/] {Markup.Escape(result.TokenId)}");
        AnsiConsole.MarkupLine($"[dim]Name:[/] {Markup.Escape(settings.Name)}");
        return 0;
    }

    /// <summary>
    /// Settings for the token create command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the descriptive name for the new token.
        /// </summary>
        [CommandArgument(0, "<name>")]
        [Description("A descriptive name for the token")]
        public string Name { get; init; } = "";
    }
}
