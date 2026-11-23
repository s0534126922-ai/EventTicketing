
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Tests;

public class EventSeatConcurrencyTests
{
    [Fact]
    public async Task UpdatingSameEventSeat_ShouldThrowConcurrencyException()
    {
        await using var connection = TestDatabaseFactory.CreateConnection();

        await TestDatabaseFactory.VerifyDatabaseAsync(connection);

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            int eventSeatId;

            await using (var setupContext =
                TestDatabaseFactory.CreateContext(connection))
            {
                await setupContext.Database.UseTransactionAsync(transaction);

                var hall = new Hall
                {
                    Name = "Concurrency Test Hall",
                    SeatCount = 1,
                    HasEverHostedEvent = true
                };

                var seat = new Seat
                {
                    Hall = hall,
                    SeatNumber = 1
                };

                var startTime = DateTime.UtcNow.Date.AddDays(3).AddHours(18);

                var testEvent = new Event
                {
                    Name = "Concurrency Test Event",
                    Hall = hall,
                    StartTime = startTime,
                    EndTime = startTime.AddHours(2),
                    Status = EventStatus.Active
                };

                var eventSeat = new EventSeat
                {
                    Event = testEvent,
                    Seat = seat,
                    Status = SeatStatus.Available
                };

                setupContext.EventSeats.Add(eventSeat);

                await setupContext.SaveChangesAsync();

                eventSeatId = eventSeat.Id;
            }

            await using var contextA =
                TestDatabaseFactory.CreateContext(connection);

            await using var contextB =
                TestDatabaseFactory.CreateContext(connection);

            await contextA.Database.UseTransactionAsync(transaction);
            await contextB.Database.UseTransactionAsync(transaction);

            var seatA = await contextA.EventSeats
                .SingleAsync(x => x.Id == eventSeatId);

            var seatB = await contextB.EventSeats
                .SingleAsync(x => x.Id == eventSeatId);

            var originalVersion = seatA.Version;

            Assert.Equal(originalVersion, seatB.Version);

            seatA.Status = SeatStatus.Booked;

            await contextA.SaveChangesAsync();

            Assert.NotEqual(originalVersion, seatA.Version);

            seatB.Status = SeatStatus.ReservedByAdmin;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                async () => await contextB.SaveChangesAsync());

            await using var verificationContext =
                TestDatabaseFactory.CreateContext(connection);

            await verificationContext.Database
                .UseTransactionAsync(transaction);

            var savedSeat = await verificationContext.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventSeatId);

            Assert.Equal(SeatStatus.Booked, savedSeat.Status);
            Assert.Equal(seatA.Version, savedSeat.Version);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
}
