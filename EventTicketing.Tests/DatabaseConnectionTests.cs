
using EventTicketing.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Tests;

public class DatabaseConnectionTests
{
    [Fact]
    public async Task Database_ShouldConnectSuccessfully()
    {
        await using var context = TestDatabaseFactory.CreateContext();

        var canConnect = await context.Database.CanConnectAsync();

        Assert.True(
            canConnect,
            "Cannot connect to the local test database.");
    }

    [Fact]
    public async Task Database_ShouldHaveAppliedMigrations()
    {
        await using var context = TestDatabaseFactory.CreateContext();

        var pendingMigrations =
            await context.Database.GetPendingMigrationsAsync();

        Assert.Empty(pendingMigrations);
    }
}
