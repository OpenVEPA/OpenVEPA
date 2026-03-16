using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenVEPA.Storage;

namespace OpenVEPA.Server.Health;

/// <summary>
/// Health check that verifies the SQLite database is reachable.
/// </summary>
public sealed class OpenVepaHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<OpenVepaDbContext> _dbFactory;

    public OpenVepaHealthCheck(IDbContextFactory<OpenVepaDbContext> dbFactory)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var canConnect = await db.Database
                .CanConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            return canConnect
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database connection failed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}
