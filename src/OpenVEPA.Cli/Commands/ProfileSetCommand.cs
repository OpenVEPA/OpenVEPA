using System.ComponentModel;
using OpenVEPA.Core.Preferences;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Sets an explicit user preference with a key, value, and optional category.
/// </summary>
internal sealed class ProfileSetCommand : AsyncCommand<ProfileSetCommand.Settings>
{
    private readonly IUserProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileSetCommand"/> class.
    /// </summary>
    /// <param name="profileService">The user profile service for managing preferences.</param>
    public ProfileSetCommand(IUserProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await _profileService.SetExplicitPreferenceAsync(
            settings.Key,
            settings.Value,
            settings.Category ?? "general",
            CancellationToken.None).ConfigureAwait(false);

        AnsiConsole.MarkupLine(
            $"[green]Preference set:[/] {Markup.Escape(settings.Key)} = {Markup.Escape(settings.Value)}");
        return 0;
    }

    /// <summary>
    /// Settings for the profile set command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the preference key.
        /// </summary>
        [CommandArgument(0, "<key>")]
        [Description("The preference key")]
        public string Key { get; init; } = "";

        /// <summary>
        /// Gets the preference value.
        /// </summary>
        [CommandArgument(1, "<value>")]
        [Description("The preference value")]
        public string Value { get; init; } = "";

        /// <summary>
        /// Gets the optional preference category.
        /// </summary>
        [CommandOption("--category")]
        [Description("The preference category")]
        public string? Category { get; init; }
    }
}
