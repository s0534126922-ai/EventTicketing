
using AutoMapper;
using EventTicketing.Core.DTOs.Events;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Exceptions;
using EventTicketing.Data;
using EventTicketing.Data.Context;
using EventTicketing.Data.Repositories;
using EventTicketing.Service.Helpers;
using EventTicketing.Service.Mappings;
using EventTicketing.Service.Services;
using EventTicketing.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using System.Data.Common;

namespace EventTicketing.Tests;

public class EventServiceIntegrationTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateEventAndAllEventSeats()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 3);
            var start = FutureIsraelTime();

            await using var context = await CreateContextAsync(connection, transaction);
            var result = await CreateService(context).CreateAsync(
                NewEventRequest(hallId, start, start.AddHours(2)));

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == result.Id);

            var seats = await verification.EventSeats
                .AsNoTracking()
                .Where(x => x.EventId == result.Id)
                .ToListAsync();

            var hall = await verification.Halls
                .AsNoTracking()
                .SingleAsync(x => x.Id == hallId);

            Assert.Equal(EventStatus.Active, savedEvent.Status);
            Assert.Equal(3, seats.Count);
            Assert.All(seats, x =>
                Assert.Equal(SeatStatus.Available, x.Status));
            Assert.True(hall.HasEverHostedEvent);
        });
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectEventsLessThanHourApart()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 2);
            var start = FutureIsraelTime();

            await CreateEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2));

            await using var context = await CreateContextAsync(connection, transaction);

            await Assert.ThrowsAsync<ConflictException>(
                () => CreateService(context).CreateAsync(
                    NewEventRequest(
                        hallId,
                        start.AddHours(2).AddMinutes(30),
                        start.AddHours(4).AddMinutes(30))));

            await using var verification = await CreateContextAsync(connection, transaction);

            Assert.Equal(1, await verification.Events
                .CountAsync(x => x.HallId == hallId));
        });
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectTimeChangeAfterBooking()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 1);
            var start = FutureIsraelTime();

            var eventId = await CreateEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2));

            await CreateConfirmedBookingAsync(
                connection, transaction, eventId);

            await using var context = await CreateContextAsync(connection, transaction);

            await Assert.ThrowsAsync<ConflictException>(
                () => CreateService(context).UpdateAsync(
                    eventId,
                    NewUpdateRequest(
                        hallId,
                        start.AddHours(1),
                        start.AddHours(3))));

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            Assert.Equal(IsraelTimeHelper.ToUtc(start), savedEvent.StartTime);
        });
    }

    [Fact]
    public async Task CancelAsync_ShouldCancelEventAndConfirmedBookings()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 1);
            var start = FutureIsraelTime();

            var eventId = await CreateEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2));

            var bookingId = await CreateConfirmedBookingAsync(
                connection, transaction, eventId);

            await using (var context = await CreateContextAsync(connection, transaction))
            {
                await CreateService(context).CancelAsync(eventId);
            }

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            var booking = await verification.Bookings
                .AsNoTracking()
                .SingleAsync(x => x.Id == bookingId);

            var eventSeat = await verification.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.EventId == eventId);

            Assert.Equal(EventStatus.Cancelled, savedEvent.Status);
            Assert.Equal(BookingStatus.CancelledByEvent, booking.Status);
            Assert.Equal(SeatStatus.Available, eventSeat.Status);
        });
    }

    [Fact]
    public async Task RestoreAsync_ShouldNotRestoreCancelledBookings()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 1);
            var start = FutureIsraelTime();

            var eventId = await CreateEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2));

            var bookingId = await CreateConfirmedBookingAsync(
                connection, transaction, eventId);

            await using (var context = await CreateContextAsync(connection, transaction))
            {
                await CreateService(context).CancelAsync(eventId);
            }

            await using (var context = await CreateContextAsync(connection, transaction))
            {
                await CreateService(context).RestoreAsync(eventId);
            }

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            var booking = await verification.Bookings
                .AsNoTracking()
                .SingleAsync(x => x.Id == bookingId);

            var eventSeat = await verification.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.EventId == eventId);

            Assert.Equal(EventStatus.Active, savedEvent.Status);
            Assert.Equal(BookingStatus.CancelledByEvent, booking.Status);
            Assert.Equal(SeatStatus.Available, eventSeat.Status);
        });
    }

    [Fact]
    public async Task RestoreAsync_ShouldRejectAfterDeadline()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 1);

            var start = DateTime.UtcNow.AddDays(-2);

            var eventId = await InsertEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2),
                EventStatus.Cancelled);

            await using var context = await CreateContextAsync(connection, transaction);

            await Assert.ThrowsAsync<ConflictException>(
                () => CreateService(context).RestoreAsync(eventId));

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            Assert.Equal(EventStatus.Cancelled, savedEvent.Status);
        });
    }

    [Fact]
    public async Task SearchAsync_ShouldPersistFinishedEventStatus()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 1);
            var start = DateTime.UtcNow.AddDays(-2);

            var eventId = await InsertEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2),
                EventStatus.Active);

            await using (var context = await CreateContextAsync(connection, transaction))
            {
                await CreateService(context).SearchAsync(
                    new EventSearchRequestDto { PageNumber = 1 },
                    isAdmin: true);
            }

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            Assert.Equal(EventStatus.Finished, savedEvent.Status);
        });
    }

    // Additional tests

    [Fact]
    public async Task UpdateAsync_ShouldRejectHallChangeAfterBooking()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var firstHallId = await CreateHallAsync(connection, transaction, 1);
            var secondHallId = await CreateHallAsync(connection, transaction, 2);
            var start = FutureIsraelTime();

            var eventId = await CreateEventAsync(
                connection, transaction, firstHallId,
                start, start.AddHours(2));

            await CreateConfirmedBookingAsync(
                connection, transaction, eventId);

            await using var context = await CreateContextAsync(connection, transaction);

            await Assert.ThrowsAsync<ConflictException>(
                () => CreateService(context).UpdateAsync(
                    eventId,
                    NewUpdateRequest(
                        secondHallId,
                        start,
                        start.AddHours(2))));

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            Assert.Equal(firstHallId, savedEvent.HallId);
            Assert.Equal(1, await verification.EventSeats
                .CountAsync(x => x.EventId == eventId));
        });
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectDateChangeAfterBooking()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 1);
            var start = FutureIsraelTime();

            var eventId = await CreateEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2));

            await CreateConfirmedBookingAsync(
                connection, transaction, eventId);

            var newStart = start.AddDays(1);

            await using var context = await CreateContextAsync(connection, transaction);

            await Assert.ThrowsAsync<ConflictException>(
                () => CreateService(context).UpdateAsync(
                    eventId,
                    NewUpdateRequest(
                        hallId,
                        newStart,
                        newStart.AddHours(2))));

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            Assert.Equal(IsraelTimeHelper.ToUtc(start), savedEvent.StartTime);
        });
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectEditingOnEventDay()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 1);

            var israelNow = IsraelTimeHelper.ToIsrael(DateTime.UtcNow);

            var eventStart = DateTime.SpecifyKind(
                israelNow.Date.AddHours(23),
                DateTimeKind.Unspecified);

            var eventId = await InsertEventAsync(
                connection, transaction, hallId,
                IsraelTimeHelper.ToUtc(eventStart),
                IsraelTimeHelper.ToUtc(eventStart.AddMinutes(30)),
                EventStatus.Active);

            await using var context = await CreateContextAsync(connection, transaction);

            await Assert.ThrowsAsync<ConflictException>(
                () => CreateService(context).UpdateAsync(
                    eventId,
                    NewUpdateRequest(
                        hallId,
                        eventStart,
                        eventStart.AddMinutes(30))));

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            Assert.Equal("Direct Test Event", savedEvent.Name);
        });
    }

    [Fact]
    public async Task CancelAsync_ShouldPreserveAdminReservedSeats()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 2);
            var start = FutureIsraelTime();

            var eventId = await CreateEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2));

            var reservedSeatId = await ReserveSeatForAdminAsync(
                connection, transaction, eventId);

            await using (var context = await CreateContextAsync(connection, transaction))
            {
                await CreateService(context).CancelAsync(eventId);
            }

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            var reservedSeat = await verification.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.Id == reservedSeatId);

            Assert.Equal(EventStatus.Cancelled, savedEvent.Status);
            Assert.Equal(SeatStatus.ReservedByAdmin, reservedSeat.Status);
        });
    }

    [Fact]
    public async Task RestoreAsync_ShouldPreserveAdminReservedSeats()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 2);
            var start = FutureIsraelTime();

            var eventId = await CreateEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2));

            var reservedSeatId = await ReserveSeatForAdminAsync(
                connection, transaction, eventId);

            await using (var context = await CreateContextAsync(connection, transaction))
            {
                await CreateService(context).CancelAsync(eventId);
            }

            await using (var context = await CreateContextAsync(connection, transaction))
            {
                await CreateService(context).RestoreAsync(eventId);
            }

            await using var verification = await CreateContextAsync(connection, transaction);

            var savedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == eventId);

            var reservedSeat = await verification.EventSeats
                .AsNoTracking()
                .SingleAsync(x => x.Id == reservedSeatId);

            Assert.Equal(EventStatus.Active, savedEvent.Status);
            Assert.Equal(SeatStatus.ReservedByAdmin, reservedSeat.Status);
        });
    }

    [Fact]
    public async Task SearchAsync_Public_ShouldShowOnlyFutureActiveEvents()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 1);
            var start = FutureIsraelTime();

            var activeEventId = await CreateEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2));

            var cancelledStart = start.AddDays(1);

            var cancelledEventId = await InsertEventAsync(
                connection, transaction, hallId,
                IsraelTimeHelper.ToUtc(cancelledStart),
                IsraelTimeHelper.ToUtc(cancelledStart.AddHours(2)),
                EventStatus.Cancelled);

            var pastStart = DateTime.UtcNow.AddDays(-2);

            var finishedEventId = await InsertEventAsync(
                connection, transaction, hallId,
                pastStart, pastStart.AddHours(2),
                EventStatus.Active);

            await using var context = await CreateContextAsync(connection, transaction);

            var result = await CreateService(context).SearchAsync(
                new EventSearchRequestDto
                {
                    PageNumber = 1,
                    HallId = hallId
                },
                isAdmin: false);

            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(activeEventId, result.Items[0].Id);

            Assert.DoesNotContain(result.Items,
                x => x.Id == cancelledEventId);

            Assert.DoesNotContain(result.Items,
                x => x.Id == finishedEventId);
        });
    }

    [Fact]
    public async Task SearchAsync_Admin_ShouldShowAllEventStatuses()
    {
        await RunInTransactionAsync(async (connection, transaction) =>
        {
            var hallId = await CreateHallAsync(connection, transaction, 1);
            var start = FutureIsraelTime();

            var activeEventId = await CreateEventAsync(
                connection, transaction, hallId,
                start, start.AddHours(2));

            var cancelledStart = start.AddDays(1);

            var cancelledEventId = await InsertEventAsync(
                connection, transaction, hallId,
                IsraelTimeHelper.ToUtc(cancelledStart),
                IsraelTimeHelper.ToUtc(cancelledStart.AddHours(2)),
                EventStatus.Cancelled);

            var pastStart = DateTime.UtcNow.AddDays(-2);

            var finishedEventId = await InsertEventAsync(
                connection, transaction, hallId,
                pastStart, pastStart.AddHours(2),
                EventStatus.Active);

            await using var context = await CreateContextAsync(connection, transaction);

            var result = await CreateService(context).SearchAsync(
                new EventSearchRequestDto
                {
                    PageNumber = 1,
                    HallId = hallId
                },
                isAdmin: true);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);

            Assert.Contains(result.Items, x => x.Id == activeEventId);
            Assert.Contains(result.Items, x => x.Id == cancelledEventId);
            Assert.Contains(result.Items, x => x.Id == finishedEventId);

            await using var verification = await CreateContextAsync(connection, transaction);

            var finishedEvent = await verification.Events
                .AsNoTracking()
                .SingleAsync(x => x.Id == finishedEventId);

            Assert.Equal(EventStatus.Finished, finishedEvent.Status);
        });
    }

    // Test infrastructure

    private static EventService CreateService(
        EventTicketingDbContext context)
    {
        var mapperConfiguration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        var mapper = mapperConfiguration.CreateMapper();

        return new EventService(
            new EventRepository(context),
            new HallRepository(context),
            new BookingRepository(context),
            new EventSeatRepository(context),
            new UnitOfWork(context),
            mapper);
    }

    private static CreateEventRequestDto NewEventRequest(
        int hallId,
        DateTime start,
        DateTime end)
    {
        return new CreateEventRequestDto
        {
            Name = "Integration Test Event",
            HallId = hallId,
            StartTime = start,
            EndTime = end
        };
    }

    private static UpdateEventRequestDto NewUpdateRequest(
        int hallId,
        DateTime start,
        DateTime end)
    {
        return new UpdateEventRequestDto
        {
            Name = "Updated Event",
            HallId = hallId,
            StartTime = start,
            EndTime = end
        };
    }

    private static DateTime FutureIsraelTime()
    {
        var futureDate = IsraelTimeHelper
            .ToIsrael(DateTime.UtcNow.AddDays(7))
            .Date;

        return DateTime.SpecifyKind(
            futureDate.AddHours(12),
            DateTimeKind.Unspecified);
    }

    private static async Task<int> CreateHallAsync(
        NpgsqlConnection connection,
        DbTransaction transaction,
        int seatCount)
    {
        await using var context = await CreateContextAsync(connection, transaction);

        var hall = new Hall
        {
            Name = $"Test Hall {Guid.NewGuid():N}",
            SeatCount = seatCount
        };

        for (var number = 1; number <= seatCount; number++)
        {
            hall.Seats.Add(new Seat
            {
                SeatNumber = number
            });
        }

        context.Halls.Add(hall);
        await context.SaveChangesAsync();

        return hall.Id;
    }

    private static async Task<int> CreateEventAsync(
        NpgsqlConnection connection,
        DbTransaction transaction,
        int hallId,
        DateTime start,
        DateTime end)
    {
        await using var context = await CreateContextAsync(connection, transaction);

        var result = await CreateService(context).CreateAsync(
            NewEventRequest(hallId, start, end));

        return result.Id;
    }

    private static async Task<int> InsertEventAsync(
        NpgsqlConnection connection,
        DbTransaction transaction,
        int hallId,
        DateTime startUtc,
        DateTime endUtc,
        EventStatus status)
    {
        await using var context = await CreateContextAsync(connection, transaction);

        var hall = await context.Halls
            .Include(x => x.Seats)
            .SingleAsync(x => x.Id == hallId);

        var testEvent = new Event
        {
            Name = "Direct Test Event",
            Hall = hall,
            StartTime = startUtc,
            EndTime = endUtc,
            Status = status
        };

        foreach (var seat in hall.Seats)
        {
            testEvent.EventSeats.Add(new EventSeat
            {
                SeatId = seat.Id,
                Status = SeatStatus.Available
            });
        }

        context.Events.Add(testEvent);
        await context.SaveChangesAsync();

        return testEvent.Id;
    }

    private static async Task<int> CreateConfirmedBookingAsync(
        NpgsqlConnection connection,
        DbTransaction transaction,
        int eventId)
    {
        await using var context = await CreateContextAsync(connection, transaction);

        var eventSeat = await context.EventSeats
            .Include(x => x.Event)
            .FirstAsync(x => x.EventId == eventId);

        var user = new User
        {
            FullName = "Event Integration Test User",
            Email = $"event-test-{Guid.NewGuid():N}@example.com",
            PasswordHash = "integration-test-only",
            Role = UserRole.User
        };

        var booking = new Booking
        {
            User = user,
            EventSeat = eventSeat,
            BookedAt = DateTime.UtcNow,
            Status = BookingStatus.Confirmed,
            EventStartTimeAtBooking = eventSeat.Event.StartTime,
            EventEndTimeAtBooking = eventSeat.Event.EndTime
        };

        eventSeat.Status = SeatStatus.Booked;

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        return booking.Id;
    }

    private static async Task<int> ReserveSeatForAdminAsync(
        NpgsqlConnection connection,
        DbTransaction transaction,
        int eventId)
    {
        await using var context = await CreateContextAsync(connection, transaction);

        var eventSeat = await context.EventSeats
            .FirstAsync(x => x.EventId == eventId);

        eventSeat.Status = SeatStatus.ReservedByAdmin;
        await context.SaveChangesAsync();

        return eventSeat.Id;
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

    private static async Task RunInTransactionAsync(
        Func<NpgsqlConnection, DbTransaction, Task> test)
    {
        await using var connection = TestDatabaseFactory.CreateConnection();

        await TestDatabaseFactory.VerifyDatabaseAsync(connection);

        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            await test(connection, transaction);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
}
