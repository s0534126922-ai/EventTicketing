
using System.Data.Common;
using EventTicketing.Core.DTOs.Bookings;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Exceptions;
using EventTicketing.Data;
using EventTicketing.Data.Context;
using EventTicketing.Data.Repositories;
using EventTicketing.Service.Services;
using EventTicketing.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventTicketing.Tests;

public class BookingServiceIntegrationTests
{
    [Fact]
    public async Task CreateAsync_ShouldBookAvailableSeat()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var data = await CreateTestDataAsync(
                connection, transaction);

            await using var context = await CreateContextAsync(
                connection, transaction);

            var service = CreateBookingService(context);

            var result = await service.CreateAsync(
                data.User1Id,
                new CreateBookingRequestDto
                {
                    EventSeatId = data.EventSeatId
                });

            Assert.Equal(
                BookingStatus.Confirmed,
                result.Booking.Status);

            Assert.Equal(1, result.Booking.SeatNumber);

            await using var verification = await CreateContextAsync(
                connection, transaction);

            var seat = await verification.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.Id == data.EventSeatId);

            var bookings = await verification.Bookings
                .AsNoTracking()
                .Where(x => x.EventSeatId == data.EventSeatId)
                .ToListAsync();

            Assert.Equal(SeatStatus.Booked, seat.Status);
            Assert.Single(bookings);
            Assert.Equal(data.User1Id, bookings[0].UserId);
            Assert.Equal(
                BookingStatus.Confirmed,
                bookings[0].Status);
        });
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectAlreadyBookedSeat()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var data = await CreateTestDataAsync(
                connection, transaction);

            await using (var firstContext = await CreateContextAsync(
                connection, transaction))
            {
                var firstService = CreateBookingService(firstContext);

                await firstService.CreateAsync(
                    data.User1Id,
                    new CreateBookingRequestDto
                    {
                        EventSeatId = data.EventSeatId
                    });
            }

            await using (var secondContext = await CreateContextAsync(
                connection, transaction))
            {
                var secondService = CreateBookingService(secondContext);

                await Assert.ThrowsAsync<ConflictException>(
                    () => secondService.CreateAsync(
                        data.User2Id,
                        new CreateBookingRequestDto
                        {
                            EventSeatId = data.EventSeatId
                        }));
            }

            await using var verification = await CreateContextAsync(
                connection, transaction);

            var bookings = await verification.Bookings
                .AsNoTracking()
                .Where(x => x.EventSeatId == data.EventSeatId)
                .ToListAsync();

            Assert.Single(bookings);
            Assert.Equal(data.User1Id, bookings[0].UserId);
        });
    }

    [Fact]
    public async Task CreateAsync_WithStaleSeatVersion_ShouldRollbackBooking()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var data = await CreateTestDataAsync(
                connection, transaction);

            await using var contextA = await CreateContextAsync(
                connection, transaction);

            await using var contextB = await CreateContextAsync(
                connection, transaction);

            var seatA = await contextA.EventSeats
                .SingleAsync(x => x.Id == data.EventSeatId);

            var seatB = await contextB.EventSeats
                .SingleAsync(x => x.Id == data.EventSeatId);

            Assert.Equal(seatA.Version, seatB.Version);

            var serviceA = CreateBookingService(contextA);
            var serviceB = CreateBookingService(contextB);

            await serviceA.CreateAsync(
                data.User1Id,
                new CreateBookingRequestDto
                {
                    EventSeatId = data.EventSeatId
                });

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => serviceB.CreateAsync(
                    data.User2Id,
                    new CreateBookingRequestDto
                    {
                        EventSeatId = data.EventSeatId
                    }));

            await using var verification = await CreateContextAsync(
                connection, transaction);

            var bookings = await verification.Bookings
                .AsNoTracking()
                .Where(x => x.EventSeatId == data.EventSeatId)
                .ToListAsync();

            var seat = await verification.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.Id == data.EventSeatId);

            Assert.Single(bookings);
            Assert.Equal(data.User1Id, bookings[0].UserId);
            Assert.Equal(
                BookingStatus.Confirmed,
                bookings[0].Status);
            Assert.Equal(SeatStatus.Booked, seat.Status);
        });
    }

    [Fact]
    public async Task CancelAsync_ShouldSoftDeleteBookingAndReleaseSeat()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var data = await CreateTestDataAsync(
                connection, transaction);

            int bookingId;

            await using (var bookingContext = await CreateContextAsync(
                connection, transaction))
            {
                var service = CreateBookingService(bookingContext);

                var result = await service.CreateAsync(
                    data.User1Id,
                    new CreateBookingRequestDto
                    {
                        EventSeatId = data.EventSeatId
                    });

                bookingId = result.Booking.Id;
            }

            await using (var cancellationContext =
                await CreateContextAsync(connection, transaction))
            {
                var service = CreateBookingService(cancellationContext);

                await service.CancelAsync(
                    bookingId,
                    data.User1Id);
            }

            await using var verification = await CreateContextAsync(
                connection, transaction);

            var booking = await verification.Bookings
                .AsNoTracking()
                .SingleAsync(x => x.Id == bookingId);

            var seat = await verification.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.Id == data.EventSeatId);

            Assert.Equal(BookingStatus.Deleted, booking.Status);
            Assert.NotNull(booking.DeletedAt);
            Assert.Equal(SeatStatus.Available, seat.Status);
        });
    }

    [Fact]
    public async Task CancelAsync_ShouldRejectDifferentUser()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var data = await CreateTestDataAsync(
                connection, transaction);

            int bookingId;

            await using (var bookingContext = await CreateContextAsync(
                connection, transaction))
            {
                var service = CreateBookingService(bookingContext);

                var result = await service.CreateAsync(
                    data.User1Id,
                    new CreateBookingRequestDto
                    {
                        EventSeatId = data.EventSeatId
                    });

                bookingId = result.Booking.Id;
            }

            await using (var cancellationContext =
                await CreateContextAsync(connection, transaction))
            {
                var service = CreateBookingService(cancellationContext);

                await Assert.ThrowsAsync<NotFoundException>(
                    () => service.CancelAsync(
                        bookingId,
                        data.User2Id));
            }

            await using var verification = await CreateContextAsync(
                connection, transaction);

            var booking = await verification.Bookings
                .AsNoTracking()
                .SingleAsync(x => x.Id == bookingId);

            var seat = await verification.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.Id == data.EventSeatId);

            Assert.Equal(
                BookingStatus.Confirmed,
                booking.Status);

            Assert.Equal(SeatStatus.Booked, seat.Status);
        });
    }

    [Fact]
    public async Task CancelAsync_ShouldRejectAfterDeadline()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var data = await CreateTestDataAsync(
                connection, transaction);

            int bookingId;

            await using (var bookingContext = await CreateContextAsync(
                connection, transaction))
            {
                var service = CreateBookingService(bookingContext);

                var result = await service.CreateAsync(
                    data.User1Id,
                    new CreateBookingRequestDto
                    {
                        EventSeatId = data.EventSeatId
                    });

                bookingId = result.Booking.Id;
            }

            await using (var setupContext = await CreateContextAsync(
                connection, transaction))
            {
                var eventEntity = await setupContext.Events
                    .SingleAsync(x => x.Id == data.EventId);

                eventEntity.StartTime = DateTime.UtcNow.AddMinutes(30);
                eventEntity.EndTime =
                    eventEntity.StartTime.AddHours(2);

                await setupContext.SaveChangesAsync();
            }

            await using (var cancellationContext =
                await CreateContextAsync(connection, transaction))
            {
                var service = CreateBookingService(cancellationContext);

                await Assert.ThrowsAsync<ConflictException>(
                    () => service.CancelAsync(
                        bookingId,
                        data.User1Id));
            }

            await using var verification = await CreateContextAsync(
                connection, transaction);

            var booking = await verification.Bookings
                .AsNoTracking()
                .SingleAsync(x => x.Id == bookingId);

            var seat = await verification.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.Id == data.EventSeatId);

            Assert.Equal(
                BookingStatus.Confirmed,
                booking.Status);

            Assert.Equal(SeatStatus.Booked, seat.Status);
        });
    }

    private static BookingService CreateBookingService(
        EventTicketingDbContext context)
    {
        return new BookingService(
            new BookingRepository(context),
            new EventSeatRepository(context),
            new UserRepository(context),
            new UnitOfWork(context));
    }

    private static async Task<EventTicketingDbContext> CreateContextAsync(
        NpgsqlConnection connection,
        DbTransaction transaction)
    {
        var context = TestDatabaseFactory.CreateContext(connection);

        try
        {
            await context.Database.UseTransactionAsync(transaction);
            return context;
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    private static async Task<TestData> CreateTestDataAsync(
        NpgsqlConnection connection,
        DbTransaction transaction)
    {
        await using var context = await CreateContextAsync(
            connection, transaction);

        var uniqueId = Guid.NewGuid().ToString("N");

        var user1 = new User
        {
            FullName = "Integration Test User 1",
            Email = $"test1-{uniqueId}@example.com",
            PasswordHash = "integration-test-only",
            Role = UserRole.User
        };

        var user2 = new User
        {
            FullName = "Integration Test User 2",
            Email = $"test2-{uniqueId}@example.com",
            PasswordHash = "integration-test-only",
            Role = UserRole.User
        };

        var hall = new Hall
        {
            Name = $"Integration Test Hall {uniqueId}",
            SeatCount = 1,
            HasEverHostedEvent = true
        };

        var seat = new Seat
        {
            Hall = hall,
            SeatNumber = 1
        };

        var startTime = DateTime.UtcNow.Date
            .AddDays(3)
            .AddHours(12);

        var testEvent = new Event
        {
            Name = "Integration Test Event",
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

        context.Users.AddRange(user1, user2);
        context.EventSeats.Add(eventSeat);

        await context.SaveChangesAsync();

        return new TestData(
            user1.Id,
            user2.Id,
            testEvent.Id,
            eventSeat.Id);
    }

    private static async Task RunInTransactionAsync(
        Func<NpgsqlConnection, DbTransaction, Task> test)
    {
        await using var connection =
            TestDatabaseFactory.CreateConnection();

        await TestDatabaseFactory.VerifyDatabaseAsync(connection);

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            await test(connection, transaction);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private sealed record TestData(
        int User1Id,
        int User2Id,
        int EventId,
        int EventSeatId);
}
