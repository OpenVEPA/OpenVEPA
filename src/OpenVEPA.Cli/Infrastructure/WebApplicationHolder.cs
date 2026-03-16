using Microsoft.AspNetCore.Builder;

namespace OpenVEPA.Cli.Infrastructure;

/// <summary>
/// Holds a reference to the <see cref="WebApplication"/> so that commands
/// resolved through dependency injection can access it after construction.
/// </summary>
internal sealed class WebApplicationHolder
{
    /// <summary>
    /// Gets or sets the configured web application instance.
    /// </summary>
    public WebApplication App { get; set; } = null!;
}
