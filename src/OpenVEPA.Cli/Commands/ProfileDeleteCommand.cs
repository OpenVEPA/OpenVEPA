using System.ComponentModel;
using OpenVEPA.Core.Preferences;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Deletes a user preference by its key.
/// </summary>
internal sealed class ProfileDeleteCommand : AsyncCommand<ProfileDeleteCommand.Settings>
{
    private readonly IUserProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileDeleteCommand"/> class.
    /// </summary>
    /// <param name="profileService">The user profile service for managing preferences.</param>
    public ProfileDeleteCommand(IUserProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await _profileService.DeletePreferenceAsync(settings.Key, CancellationToken.None)
            .ConfigureAwait(false);

        AnsiConsole.MarkupLine($"[green]Deleted preference:[/] {Markup.Escape(settings.Key)}");
        return 0;
    }

    /// <summary>
    /// Settings for the profile delete command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the preference key to delete.
        /// </summary>
        [CommandArgument(0, "<key>")]
        [Description("The preference key to delete")]
        public string Key { get; init; } = "";
    }
}
