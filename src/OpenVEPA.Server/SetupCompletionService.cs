namespace OpenVEPA.Server;

/// <summary>
/// Tracks whether initial setup has been completed.
/// Checks for the existence of a user-written openvepa.conf in the OpenVEPA home directory.
/// </summary>
internal sealed class SetupCompletionService
{
    private volatile bool _isComplete;
    private readonly string _configPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupCompletionService"/> class.
    /// </summary>
    /// <param name="openVepaHome">The OpenVEPA home directory path.</param>
    internal SetupCompletionService(string openVepaHome)
    {
        _configPath = Path.Combine(openVepaHome, "openvepa.conf");
        _isComplete = File.Exists(_configPath);
    }

    /// <summary>Gets the full path to the persisted application configuration file.</summary>
    internal string ConfigPath => _configPath;

    /// <summary>Whether initial setup has been completed.</summary>
    internal bool IsSetupComplete => _isComplete;

    /// <summary>Marks setup as complete (called after config is written).</summary>
    internal void MarkComplete() => _isComplete = true;

    /// <summary>Re-checks the filesystem for the config file.</summary>
    internal void Refresh() => _isComplete = File.Exists(_configPath);
}
