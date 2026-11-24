using EventTicketing.Core.Entities;
using EventTicketing.Data;
using EventTicketing.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventTicketing.Tests;

public sealed class SerializableConcurrencyTests
{
    [Fact]
    public async Task ConcurrentTransactions_ShouldRejectConflictingUpdate()
    {
        await using var connection1 =
            TestDatabaseFactory.CreateConnection();

        await using var connection2 =
            TestDatabaseFactory.CreateConnection();

        await TestDatabaseFactory.VerifyDatabaseAsync(connection1);
        await TestDatabaseFactory.VerifyDatabaseAsync(connection2);

        var name = $"Concurrency Test {Guid.NewGuid():N}";
        int hallId = 0;

        try
        {
            await using (var setup =
                TestDatabaseFactory.CreateContext(connection1))
            {
                var hall = new Hall
                {
                    Name = name,
                    SeatCount = 1
                };

                setup.Halls.Add(hall);

                await setup.SaveChangesAsync();

                hallId = hall.Id;
            }

            await using var context1 =
                TestDatabaseFactory.CreateContext(connection1);

            await using var context2 =
                TestDatabaseFactory.CreateContext(connection2);

            await using var unitOfWork1 =
                new UnitOfWork(context1);

            await using var unitOfWork2 =
                new UnitOfWork(context2);

            await unitOfWork1.BeginTransactionAsync();
            await unitOfWork2.BeginTransactionAsync();

            var hall1 = await context1.Halls
                .SingleAsync(hall => hall.Id == hallId);

            var hall2 = await context2.Halls
                .SingleAsync(hall => hall.Id == hallId);

            hall1.Name = name + " First";

            await unitOfWork1.SaveChangesAsync();
            await unitOfWork1.CommitTransactionAsync();

            hall2.Name = name + " Second";

            var exception = await Record.ExceptionAsync(
                async () =>
                {
                    await unitOfWork2.SaveChangesAsync();
                    await unitOfWork2.CommitTransactionAsync();
                });

            Assert.NotNull(exception);

            Assert.True(
                IsSerializationFailure(exception),
                exception.ToString());

            if (context2.Database.CurrentTransaction is not null)
            {
                await unitOfWork2.RollbackTransactionAsync();
            }

            await using var verification =
                TestDatabaseFactory.CreateContext(connection1);

            var savedHall = await verification.Halls
                .AsNoTracking()
                .SingleAsync(hall => hall.Id == hallId);

            Assert.Equal(name + " First", savedHall.Name);
        }
        finally
        {
            if (hallId > 0)
            {
                await using var cleanup =
                    TestDatabaseFactory.CreateContext(connection1);

                await cleanup.Halls
                    .Where(hall => hall.Id == hallId)
                    .ExecuteDeleteAsync();
            }
        }
    }

    private static bool IsSerializationFailure(
        Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is PostgresException postgresException &&
                postgresException.SqlState ==
                    PostgresErrorCodes.SerializationFailure)
            {
                return true;
            }
        }

        return false;
    }
}