using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenVEPA.Server.Auth;

/// <summary>
/// Generates and manages an ephemeral loopback token for local TUI access.
/// A new token is generated on each startup and written to a temp file
/// with restrictive (user-only) permissions. Valid only for connections
/// from 127.0.0.1 or ::1.
/// </summary>
public sealed class LoopbackTokenService : IHostedService, IDisposable
{
    private const int TokenSizeBytes = 32;
    private const string TokenFileName = "openvpa-loopback.token";

    private readonly ILogger<LoopbackTokenService> _logger;
    private string? _token;
    private string? _tokenFilePath;

    public LoopbackTokenService(ILogger<LoopbackTokenService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Gets the current loopback token. Null before startup.</summary>
    public string? Token => _token;

    /// <summary>Gets the path to the token file. Null before startup.</summary>
    public string? TokenFilePath => _tokenFilePath;

    /// <summary>
    /// Validates a token for loopback access. Returns true only if the token
    /// matches and the remote address is a loopback address.
    /// </summary>
    public bool Validate(string token, System.Net.IPAddress? remoteAddress)
    {
        if (string.IsNullOrEmpty(token) || _token is null)
        {
            return false;
        }

        if (remoteAddress is null || !System.Net.IPAddress.IsLoopback(remoteAddress))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(token),
            System.Text.Encoding.UTF8.GetBytes(_token));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _token = GenerateToken();
        _tokenFilePath = WriteTokenFile(_token);
        _logger.LogInformation("Loopback token written to {Path}", _tokenFilePath);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        CleanupTokenFile();
        _token = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        CleanupTokenFile();
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeBytes);
        return Convert.ToBase64String(bytes);
    }

    private string WriteTokenFile(string token)
    {
        var directory = Path.Combine(Path.GetTempPath(), "openvpa");
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, TokenFileName);
        File.WriteAllText(filePath, token);

        // Restrict permissions on non-Windows platforms.
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        return filePath;
    }

    private void CleanupTokenFile()
    {
        if (_tokenFilePath is null)
        {
            return;
        }

        try
        {
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
                _logger.LogDebug("Removed loopback token file {Path}", _tokenFilePath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to clean up loopback token file {Path}", _tokenFilePath);
        }
    }
}
