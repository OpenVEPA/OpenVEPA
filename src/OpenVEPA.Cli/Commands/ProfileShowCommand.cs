using OpenVEPA.Core.Preferences;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Displays all user profile preferences in a formatted table.
/// </summary>
internal sealed class ProfileShowCommand : AsyncCommand
{
    private readonly IUserProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileShowCommand"/> class.
    /// </summary>
    /// <param name="profileService">The user profile service for querying preferences.</param>
    public ProfileShowCommand(IUserProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var preferences = await _profileService.GetAllPreferencesAsync(CancellationToken.None)
            .ConfigureAwait(false);

        if (preferences.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No preferences set.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]User Preferences[/]")
            .AddColumn("[bold]Key[/]")
            .AddColumn("[bold]Value[/]")
            .AddColumn("[bold]Category[/]")
            .AddColumn("[bold]Source[/]")
            .AddColumn("[bold]Updated[/]");

        foreach (var pref in preferences)
        {
            table.AddRow(
                Markup.Escape(pref.Key),
                Markup.Escape(pref.Value),
                Markup.Escape(pref.Category),
                pref.Source.ToString(),
                pref.UpdatedAt.ToString("yyyy-MM-dd HH:mm"));
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
