using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenVEPA.Cli.Services;

/// <summary>
/// Installs, uninstalls, and queries OpenVEPA as an OS-level service.
/// Supports Windows (sc.exe), Linux (systemd), and macOS (launchd).
/// </summary>
internal static class PlatformServiceInstaller
{
    private const string ServiceName = "openvepa";
    private const string ServiceDescription = "OpenVEPA Personal AI Assistant";
    private const string LaunchdLabel = "com.openvepa.assistant";

    private static readonly string SystemdUnitPath =
        $"/etc/systemd/system/{ServiceName}.service";

    private static readonly string LaunchdPlistPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library",
        "LaunchAgents",
        $"{LaunchdLabel}.plist");

    /// <summary>
    /// Installs OpenVEPA as an OS service using the platform-native mechanism.
    /// </summary>
    /// <param name="executablePath">Absolute path to the OpenVEPA executable.</param>
    /// <param name="port">Port the service listens on. Defaults to 8371.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result describing success or failure with a diagnostic message.</returns>
    internal static async Task<ServiceInstallResult> InstallAsync(
        string executablePath,
        int port = 8371,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(executablePath);

        if (!File.Exists(executablePath))
        {
            return new ServiceInstallResult(false, $"Executable not found: {executablePath}", GetPlatformName());
        }

        if (OperatingSystem.IsWindows())
        {
            return await InstallWindowsAsync(executablePath, port, ct).ConfigureAwait(false);
        }

        if (OperatingSystem.IsLinux())
        {
            return await InstallLinuxAsync(executablePath, port, ct).ConfigureAwait(false);
        }

        if (OperatingSystem.IsMacOS())
        {
            return await InstallMacOsAsync(executablePath, port, ct).ConfigureAwait(false);
        }

        return new ServiceInstallResult(false, "Unsupported operating system.", GetPlatformName());
    }

    /// <summary>
    /// Uninstalls the OpenVEPA OS service.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result describing success or failure with a diagnostic message.</returns>
    internal static async Task<ServiceInstallResult> UninstallAsync(CancellationToken ct = default)
    {
        if (OperatingSystem.IsWindows())
        {
            return await UninstallWindowsAsync(ct).ConfigureAwait(false);
        }

        if (OperatingSystem.IsLinux())
        {
            return await UninstallLinuxAsync(ct).ConfigureAwait(false);
        }

        if (OperatingSystem.IsMacOS())
        {
            return await UninstallMacOsAsync(ct).ConfigureAwait(false);
        }

        return new ServiceInstallResult(false, "Unsupported operating system.", GetPlatformName());
    }

    /// <summary>
    /// Queries the current status of the OpenVEPA OS service.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result whose <see cref="ServiceInstallResult.Message"/> contains the service state.</returns>
    internal static async Task<ServiceInstallResult> GetStatusAsync(CancellationToken ct = default)
    {
        if (OperatingSystem.IsWindows())
        {
            return await GetStatusWindowsAsync(ct).ConfigureAwait(false);
        }

        if (OperatingSystem.IsLinux())
        {
            return await GetStatusLinuxAsync(ct).ConfigureAwait(false);
        }

        if (OperatingSystem.IsMacOS())
        {
            return await GetStatusMacOsAsync(ct).ConfigureAwait(false);
        }

        return new ServiceInstallResult(false, "Unsupported operating system.", GetPlatformName());
    }

    /// <summary>
    /// Returns <see langword="true"/> when the service is registered with the OS.
    /// </summary>
    internal static bool IsInstalled()
    {
        if (OperatingSystem.IsWindows())
        {
            return IsInstalledWindows();
        }

        if (OperatingSystem.IsLinux())
        {
            return File.Exists(SystemdUnitPath);
        }

        if (OperatingSystem.IsMacOS())
        {
            return File.Exists(LaunchdPlistPath);
        }

        return false;
    }

    /// <summary>
    /// Returns a human-readable name for the current operating system.
    /// </summary>
    internal static string GetPlatformName()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsLinux()) return "Linux";
        if (OperatingSystem.IsMacOS()) return "macOS";
        return RuntimeInformation.OSDescription;
    }

    // ────────────────────────────────────────────────────────────────
    //  Windows (sc.exe)
    // ────────────────────────────────────────────────────────────────

    private static async Task<ServiceInstallResult> InstallWindowsAsync(
        string executablePath, int port, CancellationToken ct)
    {
        var binPath = $"\"{executablePath}\" start --urls=http://localhost:{port.ToString(CultureInfo.InvariantCulture)}";
        var createArgs = $"create {ServiceName} binPath=\"{binPath}\" start=delayed-auto DisplayName=\"{ServiceDescription}\"";

        var (exitCode, stdOut, stdErr) = await RunProcessAsync("sc.exe", createArgs, ct).ConfigureAwait(false);

        if (exitCode != 0)
        {
            return WindowsErrorResult("install", exitCode, stdOut, stdErr);
        }

        var (startExit, startOut, startErr) = await RunProcessAsync("sc.exe", $"start {ServiceName}", ct).ConfigureAwait(false);

        if (startExit != 0)
        {
            return new ServiceInstallResult(
                true,
                $"Service registered but failed to start. Use 'sc.exe start {ServiceName}' to retry. {Combine(startOut, startErr).Trim()}",
                "Windows");
        }

        return new ServiceInstallResult(true, "Service installed and started.", "Windows");
    }

    private static async Task<ServiceInstallResult> UninstallWindowsAsync(CancellationToken ct)
    {
        // Best-effort stop before delete.
        await RunProcessAsync("sc.exe", $"stop {ServiceName}", ct).ConfigureAwait(false);

        var (exitCode, stdOut, stdErr) = await RunProcessAsync("sc.exe", $"delete {ServiceName}", ct).ConfigureAwait(false);

        if (exitCode != 0)
        {
            return WindowsErrorResult("uninstall", exitCode, stdOut, stdErr);
        }

        return new ServiceInstallResult(true, "Service stopped and removed.", "Windows");
    }

    private static async Task<ServiceInstallResult> GetStatusWindowsAsync(CancellationToken ct)
    {
        var (exitCode, stdOut, _) = await RunProcessAsync("sc.exe", $"query {ServiceName}", ct).ConfigureAwait(false);

        if (exitCode != 0)
        {
            return new ServiceInstallResult(false, "Service is not installed.", "Windows");
        }

        var state = ParseWindowsServiceState(stdOut);
        return new ServiceInstallResult(true, state, "Windows");
    }

    private static bool IsInstalledWindows()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"query {ServiceName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            process.WaitForExit(5_000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string ParseWindowsServiceState(string queryOutput)
    {
        foreach (var line in queryOutput.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("STATE", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }
        }

        return queryOutput.Trim();
    }

    private static ServiceInstallResult WindowsErrorResult(
        string operation, int exitCode, string stdOut, string stdErr)
    {
        var combined = Combine(stdOut, stdErr).Trim();
        var accessDenied = combined.Contains("Access is denied", StringComparison.OrdinalIgnoreCase)
                           || exitCode == 5;

        if (accessDenied)
        {
            return new ServiceInstallResult(
                false,
                $"Access denied. Run as Administrator to {operation} the service.",
                "Windows");
        }

        return new ServiceInstallResult(false, $"sc.exe failed (exit {exitCode}): {combined}", "Windows");
    }

    // ────────────────────────────────────────────────────────────────
    //  Linux (systemd)
    // ────────────────────────────────────────────────────────────────

    private static async Task<ServiceInstallResult> InstallLinuxAsync(
        string executablePath, int port, CancellationToken ct)
    {
        if (!HasRootPrivileges())
        {
            return new ServiceInstallResult(
                false,
                "Root privileges required. Run with: sudo openvepa service install",
                "Linux");
        }

        var workingDir = Path.GetDirectoryName(executablePath) ?? "/usr/local/bin";
        var user = Environment.UserName;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? "/usr/share/dotnet";
        var openvepaHome = $"{home}/.openvepa";
        var urlArg = $"http://localhost:{port.ToString(CultureInfo.InvariantCulture)}";

        var unitContent = BuildSystemdUnit(executablePath, urlArg, workingDir, user, dotnetRoot, openvepaHome);

        try
        {
            await File.WriteAllTextAsync(SystemdUnitPath, unitContent, Encoding.UTF8, ct).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException)
        {
            return new ServiceInstallResult(
                false,
                "Root privileges required. Run with: sudo openvepa service install",
                "Linux");
        }

        var (reloadExit, _, reloadErr) = await RunProcessAsync("systemctl", "daemon-reload", ct).ConfigureAwait(false);
        if (reloadExit != 0)
        {
            return new ServiceInstallResult(false, $"systemctl daemon-reload failed: {reloadErr.Trim()}", "Linux");
        }

        var (enableExit, _, enableErr) = await RunProcessAsync("systemctl", $"enable {ServiceName}", ct).ConfigureAwait(false);
        if (enableExit != 0)
        {
            return new ServiceInstallResult(false, $"systemctl enable failed: {enableErr.Trim()}", "Linux");
        }

        var (startExit, _, startErr) = await RunProcessAsync("systemctl", $"start {ServiceName}", ct).ConfigureAwait(false);
        if (startExit != 0)
        {
            return new ServiceInstallResult(
                true,
                $"Service registered and enabled but failed to start. {startErr.Trim()}",
                "Linux");
        }

        return new ServiceInstallResult(true, "Service installed, enabled, and started.", "Linux");
    }

    private static async Task<ServiceInstallResult> UninstallLinuxAsync(CancellationToken ct)
    {
        if (!HasRootPrivileges())
        {
            return new ServiceInstallResult(
                false,
                "Root privileges required. Run with: sudo openvepa service uninstall",
                "Linux");
        }

        await RunProcessAsync("systemctl", $"stop {ServiceName}", ct).ConfigureAwait(false);
        await RunProcessAsync("systemctl", $"disable {ServiceName}", ct).ConfigureAwait(false);

        if (File.Exists(SystemdUnitPath))
        {
            File.Delete(SystemdUnitPath);
        }

        await RunProcessAsync("systemctl", "daemon-reload", ct).ConfigureAwait(false);

        return new ServiceInstallResult(true, "Service stopped, disabled, and removed.", "Linux");
    }

    private static async Task<ServiceInstallResult> GetStatusLinuxAsync(CancellationToken ct)
    {
        if (!File.Exists(SystemdUnitPath))
        {
            return new ServiceInstallResult(false, "Service is not installed.", "Linux");
        }

        var (_, stdOut, _) = await RunProcessAsync("systemctl", $"is-active {ServiceName}", ct).ConfigureAwait(false);
        var state = stdOut.Trim();

        return new ServiceInstallResult(true, $"Service is {state}.", "Linux");
    }

    private static string BuildSystemdUnit(
        string executablePath,
        string urlArg,
        string workingDirectory,
        string user,
        string dotnetRoot,
        string openvepaHome)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Unit]");
        sb.AppendLine($"Description={ServiceDescription}");
        sb.AppendLine("After=network.target");
        sb.AppendLine();
        sb.AppendLine("[Service]");
        sb.AppendLine("Type=notify");
        sb.AppendLine($"ExecStart={executablePath} start --urls={urlArg}");
        sb.AppendLine($"WorkingDirectory={workingDirectory}");
        sb.AppendLine("Restart=on-failure");
        sb.AppendLine("RestartSec=10");
        sb.AppendLine($"User={user}");
        sb.AppendLine($"Environment=DOTNET_ROOT={dotnetRoot}");
        sb.AppendLine($"Environment=OPENVEPA_HOME={openvepaHome}");
        sb.AppendLine();
        sb.AppendLine("[Install]");
        sb.AppendLine("WantedBy=multi-user.target");
        return sb.ToString();
    }

    private static bool HasRootPrivileges()
    {
        try
        {
            // geteuid() returns 0 for root on Linux/macOS.
            return Environment.IsPrivilegedProcess;
        }
        catch
        {
            return false;
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  macOS (launchd)
    // ────────────────────────────────────────────────────────────────

    private static async Task<ServiceInstallResult> InstallMacOsAsync(
        string executablePath, int port, CancellationToken ct)
    {
        var workingDir = Path.GetDirectoryName(executablePath) ?? "/usr/local/bin";
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var openvepaHome = $"{home}/.openvepa";
        var logsDir = $"{openvepaHome}/logs";
        var urlArg = $"http://localhost:{port.ToString(CultureInfo.InvariantCulture)}";

        Directory.CreateDirectory(logsDir);

        var plistDir = Path.GetDirectoryName(LaunchdPlistPath);
        if (plistDir is not null)
        {
            Directory.CreateDirectory(plistDir);
        }

        var plistContent = BuildLaunchdPlist(executablePath, urlArg, workingDir, openvepaHome, logsDir);

        try
        {
            await File.WriteAllTextAsync(LaunchdPlistPath, plistContent, Encoding.UTF8, ct).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ServiceInstallResult(false, $"Cannot write plist: {ex.Message}", "macOS");
        }

        var (exitCode, _, stdErr) = await RunProcessAsync("launchctl", $"load {LaunchdPlistPath}", ct).ConfigureAwait(false);

        if (exitCode != 0)
        {
            return new ServiceInstallResult(
                true,
                $"Plist written but launchctl load reported an error. {stdErr.Trim()}",
                "macOS");
        }

        return new ServiceInstallResult(true, "Service installed and loaded.", "macOS");
    }

    private static async Task<ServiceInstallResult> UninstallMacOsAsync(CancellationToken ct)
    {
        if (File.Exists(LaunchdPlistPath))
        {
            await RunProcessAsync("launchctl", $"unload {LaunchdPlistPath}", ct).ConfigureAwait(false);
            File.Delete(LaunchdPlistPath);
        }

        return new ServiceInstallResult(true, "Service unloaded and plist removed.", "macOS");
    }

    private static async Task<ServiceInstallResult> GetStatusMacOsAsync(CancellationToken ct)
    {
        if (!File.Exists(LaunchdPlistPath))
        {
            return new ServiceInstallResult(false, "Service is not installed.", "macOS");
        }

        var (_, stdOut, _) = await RunProcessAsync("launchctl", "list", ct).ConfigureAwait(false);

        var running = stdOut.Contains(LaunchdLabel, StringComparison.Ordinal);
        var state = running ? "running" : "not running";

        return new ServiceInstallResult(true, $"Service is {state}.", "macOS");
    }

    private static string BuildLaunchdPlist(
        string executablePath,
        string urlArg,
        string workingDirectory,
        string openvepaHome,
        string logsDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
        sb.AppendLine("<plist version=\"1.0\">");
        sb.AppendLine("<dict>");
        sb.AppendLine("    <key>Label</key>");
        sb.AppendLine($"    <string>{LaunchdLabel}</string>");
        sb.AppendLine("    <key>ProgramArguments</key>");
        sb.AppendLine("    <array>");
        sb.AppendLine($"        <string>{EscapeXml(executablePath)}</string>");
        sb.AppendLine("        <string>start</string>");
        sb.AppendLine($"        <string>--urls={EscapeXml(urlArg)}</string>");
        sb.AppendLine("    </array>");
        sb.AppendLine("    <key>RunAtLoad</key>");
        sb.AppendLine("    <true/>");
        sb.AppendLine("    <key>KeepAlive</key>");
        sb.AppendLine("    <true/>");
        sb.AppendLine("    <key>WorkingDirectory</key>");
        sb.AppendLine($"    <string>{EscapeXml(workingDirectory)}</string>");
        sb.AppendLine("    <key>EnvironmentVariables</key>");
        sb.AppendLine("    <dict>");
        sb.AppendLine("        <key>OPENVEPA_HOME</key>");
        sb.AppendLine($"        <string>{EscapeXml(openvepaHome)}</string>");
        sb.AppendLine("    </dict>");
        sb.AppendLine("    <key>StandardOutPath</key>");
        sb.AppendLine($"    <string>{EscapeXml(logsDir)}/stdout.log</string>");
        sb.AppendLine("    <key>StandardErrorPath</key>");
        sb.AppendLine($"    <string>{EscapeXml(logsDir)}/stderr.log</string>");
        sb.AppendLine("</dict>");
        sb.AppendLine("</plist>");
        return sb.ToString();
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
    }

    // ────────────────────────────────────────────────────────────────
    //  Process helper
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs an external process and captures its output.
    /// </summary>
    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName, string arguments, CancellationToken ct)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            // Read streams concurrently to avoid deadlocks.
            var stdOutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stdErrTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            var stdOut = await stdOutTask.ConfigureAwait(false);
            var stdErr = await stdErrTask.ConfigureAwait(false);

            return (process.ExitCode, stdOut, stdErr);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return (-1, string.Empty, $"{fileName} not found or cannot be executed: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            return (-1, string.Empty, "Operation was cancelled.");
        }
    }

    private static string Combine(string stdOut, string stdErr)
    {
        if (string.IsNullOrWhiteSpace(stdErr))
        {
            return stdOut;
        }

        if (string.IsNullOrWhiteSpace(stdOut))
        {
            return stdErr;
        }

        return $"{stdOut}\n{stdErr}";
    }
}
