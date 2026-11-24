using EventTicketing.Core.DTOs.Bookings;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Exceptions;
using EventTicketing.Data;
using EventTicketing.Data.Repositories;
using EventTicketing.Service.Services;
using EventTicketing.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventTicketing.Tests;

public sealed class BookingReadIntegrationTests
{
    [Fact]
    public async Task GetByIdAsync_WithOwner_ReturnsBooking()
    {
        await RunWithBookingAsync(async (service, data) =>
        {
            var booking = await service.GetByIdAsync(
                data.BookingId,
                data.User1Id);

            Assert.Equal(data.BookingId, booking.Id);
            Assert.Equal(data.EventSeatId, booking.EventSeatId);
            Assert.Equal(1, booking.SeatNumber);

            Assert.Equal(
                BookingStatus.Confirmed,
                booking.Status);
        });
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentUser_ThrowsNotFound()
    {
        await RunWithBookingAsync(async (service, data) =>
        {
            await Assert.ThrowsAsync<NotFoundException>(
                () => service.GetByIdAsync(
                    data.BookingId,
                    data.User2Id));
        });
    }

    [Fact]
    public async Task GetByIdAsync_AfterCancellation_ReturnsDeletedBooking()
    {
        await RunWithBookingAsync(async (service, data) =>
        {
            await service.CancelAsync(
                data.BookingId,
                data.User1Id);

            var booking = await service.GetByIdAsync(
                data.BookingId,
                data.User1Id);

            Assert.Equal(data.BookingId, booking.Id);

            Assert.Equal(
                BookingStatus.Deleted,
                booking.Status);

            Assert.NotNull(booking.DeletedAt);

            var history = await service.GetByUserIdAsync(
                data.User1Id);

            var historyBooking = Assert.Single(
                history.Events.SelectMany(
                    eventItem => eventItem.Bookings));

            Assert.Equal(
                data.BookingId,
                historyBooking.Id);

            Assert.Equal(
                BookingStatus.Deleted,
                historyBooking.Status);
        });
    }

    private static async Task RunWithBookingAsync(
        Func<BookingService, TestData, Task> test)
    {
        await using var connection =
            TestDatabaseFactory.CreateConnection();

        await TestDatabaseFactory.VerifyDatabaseAsync(
            connection);

        await using var transaction =
            await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            int user1Id;
            int user2Id;
            int eventSeatId;
            int bookingId;

            await using (var context =
                TestDatabaseFactory.CreateContext(connection))
            {
                await context.Database.UseTransactionAsync(
                    transaction);

                var user1 = new User
                {
                    FullName = "Booking Read Test User 1",
                    Email = $"read1-{uniqueId}@example.com",
                    PasswordHash = "integration-test-only",
                    Role = UserRole.User
                };

                var user2 = new User
                {
                    FullName = "Booking Read Test User 2",
                    Email = $"read2-{uniqueId}@example.com",
                    PasswordHash = "integration-test-only",
                    Role = UserRole.User
                };

                var hall = new Hall
                {
                    Name = $"Booking Read Test Hall {uniqueId}",
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

                var eventEntity = new Event
                {
                    Name = "Booking Read Test Event",
                    Hall = hall,
                    StartTime = startTime,
                    EndTime = startTime.AddHours(2),
                    Status = EventStatus.Active
                };

                var eventSeat = new EventSeat
                {
                    Event = eventEntity,
                    Seat = seat,
                    Status = SeatStatus.Available
                };

                context.Users.AddRange(user1, user2);
                context.EventSeats.Add(eventSeat);

                await context.SaveChangesAsync();

                user1Id = user1.Id;
                user2Id = user2.Id;
                eventSeatId = eventSeat.Id;

                var service = new BookingService(
                    new BookingRepository(context),
                    new EventSeatRepository(context),
                    new UserRepository(context),
                    new UnitOfWork(context));

                var result = await service.CreateAsync(
                    user1Id,
                    new CreateBookingRequestDto
                    {
                        EventSeatId = eventSeatId
                    });

                bookingId = result.Booking.Id;
            }

            await using var testContext =
                TestDatabaseFactory.CreateContext(connection);

            await testContext.Database.UseTransactionAsync(
                transaction);

            var testService = new BookingService(
                new BookingRepository(testContext),
                new EventSeatRepository(testContext),
                new UserRepository(testContext),
                new UnitOfWork(testContext));

            await test(
                testService,
                new TestData(
                    user1Id,
                    user2Id,
                    bookingId,
                    eventSeatId));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private sealed record TestData(
        int User1Id,
        int User2Id,
        int BookingId,
        int EventSeatId);
}