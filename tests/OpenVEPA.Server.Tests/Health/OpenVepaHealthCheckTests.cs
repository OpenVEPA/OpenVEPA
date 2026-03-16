using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using OpenVEPA.Server.Health;
using OpenVEPA.Storage;

namespace OpenVEPA.Server.Tests.Health;

public sealed class OpenVepaHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenDbCanConnect_ReturnsHealthy()
    {
        var dbFactory = Substitute.For<IDbContextFactory<OpenVepaDbContext>>();
        var dbContext = CreateInMemoryContext();

        dbFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(dbContext);

        var healthCheck = new OpenVepaHealthCheck(dbFactory);
        var context = new HealthCheckContext();

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDbThrows_ReturnsUnhealthy()
    {
        var dbFactory = Substitute.For<IDbContextFactory<OpenVepaDbContext>>();

        dbFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        var healthCheck = new OpenVepaHealthCheck(dbFactory);
        var context = new HealthCheckContext();

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void Constructor_NullDbFactory_ThrowsArgumentNullException()
    {
        var act = () => new OpenVepaHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static OpenVepaDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<OpenVepaDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var context = new OpenVepaDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }
}
