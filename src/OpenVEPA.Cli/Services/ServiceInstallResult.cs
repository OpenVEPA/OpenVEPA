namespace OpenVEPA.Cli.Services;

/// <summary>
/// Result of a service installation operation.
/// </summary>
/// <param name="Success">Whether the operation completed successfully.</param>
/// <param name="Message">Human-readable description of the outcome.</param>
/// <param name="Platform">Operating system where the operation ran (Windows, Linux, or macOS).</param>
internal sealed record ServiceInstallResult(bool Success, string Message, string Platform);
