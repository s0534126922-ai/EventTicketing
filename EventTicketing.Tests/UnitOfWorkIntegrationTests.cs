using System.Data;
using EventTicketing.Core.Entities;
using EventTicketing.Data;
using EventTicketing.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventTicketing.Tests;

public sealed class UnitOfWorkIntegrationTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldUseSerializable()
    {
        await using var connection =
            TestDatabaseFactory.CreateConnection();

        await TestDatabaseFactory.VerifyDatabaseAsync(connection);

        await using var context =
            TestDatabaseFactory.CreateContext(connection);

        await using var unitOfWork = new UnitOfWork(context);

        await unitOfWork.ExecuteInTransactionAsync(ct =>
        {
            var transaction =
                context.Database.CurrentTransaction;

            Assert.NotNull(transaction);

            Assert.Equal(
                IsolationLevel.Serializable,
                transaction.GetDbTransaction().IsolationLevel);

            return Task.CompletedTask;
        });

        Assert.Null(context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRollbackOnFailure()
    {
        await using var connection =
            TestDatabaseFactory.CreateConnection();

        await TestDatabaseFactory.VerifyDatabaseAsync(connection);

        var name = $"Rollback Test {Guid.NewGuid():N}";

        await using var context =
            TestDatabaseFactory.CreateContext(connection);

        await using var unitOfWork = new UnitOfWork(context);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => unitOfWork.ExecuteInTransactionAsync(
                    async ct =>
                    {
                        context.Halls.Add(new Hall
                        {
                            Name = name,
                            SeatCount = 1
                        });

                        await unitOfWork.SaveChangesAsync(ct);

                        throw new InvalidOperationException(
                            "Simulated operation failure.");
                    }));

            Assert.Null(context.Database.CurrentTransaction);

            await using var verification =
                TestDatabaseFactory.CreateContext(connection);

            Assert.False(await verification.Halls
                .AsNoTracking()
                .AnyAsync(hall => hall.Name == name));
        }
        finally
        {
            await using var cleanup =
                TestDatabaseFactory.CreateContext(connection);

            await cleanup.Halls
                .Where(hall => hall.Name == name)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldCommitOnSuccess()
    {
        await using var connection =
            TestDatabaseFactory.CreateConnection();

        await TestDatabaseFactory.VerifyDatabaseAsync(connection);

        var name = $"Commit Test {Guid.NewGuid():N}";

        try
        {
            await using var context =
                TestDatabaseFactory.CreateContext(connection);

            await using var unitOfWork = new UnitOfWork(context);

            var hallId = await unitOfWork.ExecuteInTransactionAsync(
                async ct =>
                {
                    var hall = new Hall
                    {
                        Name = name,
                        SeatCount = 1
                    };

                    context.Halls.Add(hall);

                    await unitOfWork.SaveChangesAsync(ct);

                    return hall.Id;
                });

            await using var verification =
                TestDatabaseFactory.CreateContext(connection);

            Assert.True(await verification.Halls
                .AsNoTracking()
                .AnyAsync(hall =>
                    hall.Id == hallId &&
                    hall.Name == name));
        }
        finally
        {
            await using var cleanup =
                TestDatabaseFactory.CreateContext(connection);

            await cleanup.Halls
                .Where(hall => hall.Name == name)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRejectLowerIsolation()
    {
        await using var connection =
            TestDatabaseFactory.CreateConnection();

        await TestDatabaseFactory.VerifyDatabaseAsync(connection);

        await using var context =
            TestDatabaseFactory.CreateContext(connection);

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        await using var unitOfWork = new UnitOfWork(context);

        var executed = false;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => unitOfWork.ExecuteInTransactionAsync(ct =>
            {
                executed = true;

                return Task.CompletedTask;
            }));

        Assert.False(executed);

        await transaction.RollbackAsync();
    }
}