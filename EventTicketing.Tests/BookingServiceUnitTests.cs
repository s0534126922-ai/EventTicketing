using EventTicketing.Core.DTOs.Bookings;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Exceptions;
using EventTicketing.Core.Interfaces;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Service.Services;
using Moq;

namespace EventTicketing.Tests;

public class BookingServiceUnitTests
{
    private const int UserId = 1;
    private const int EventSeatId = 10;
    private const int BookingId = 20;

    private readonly Mock<IBookingRepository>
        _bookingRepository = new();

    private readonly Mock<IEventSeatRepository>
        _eventSeatRepository = new();

    private readonly Mock<IUserRepository>
        _userRepository = new();

    private readonly Mock<IUnitOfWork>
        _unitOfWork = new();

    private readonly BookingService _service;

    public BookingServiceUnitTests()
    {
        _unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>())).Returns((Func<CancellationToken, Task> operation, CancellationToken ct) => operation(ct));
        _unitOfWork.Setup(x => x.ExecuteInTransactionAsync<BookingConfirmationResponseDto>(It.IsAny<Func<CancellationToken, Task<BookingConfirmationResponseDto>>>(), It.IsAny<CancellationToken>())).Returns((Func<CancellationToken, Task<BookingConfirmationResponseDto>> operation, CancellationToken ct) => operation(ct));

        _service = new BookingService(
            _bookingRepository.Object,
            _eventSeatRepository.Object,
            _userRepository.Object,
            _unitOfWork.Object);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _bookingRepository
            .Setup(x => x.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateAsync_AvailableSeat_ConfirmsBooking()
    {
        var seat = CreateEventSeat();
        var originalVersion = seat.Version;

        SetupExistingUser();
        SetupEventSeat(seat);

        Booking? savedBooking = null;

        _bookingRepository
            .Setup(x => x.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>(
                (booking, _) =>
                {
                    savedBooking = booking;
                    booking.Id = BookingId;
                })
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(
            UserId,
            CreateRequest());

        Assert.NotNull(savedBooking);

        Assert.Equal(UserId, savedBooking.UserId);
        Assert.Equal(EventSeatId, savedBooking.EventSeatId);
        Assert.Equal(
            BookingStatus.Confirmed,
            savedBooking.Status);

        Assert.Equal(
            seat.Event.StartTime,
            savedBooking.EventStartTimeAtBooking);

        Assert.Equal(
            seat.Event.EndTime,
            savedBooking.EventEndTimeAtBooking);

        Assert.Equal(SeatStatus.Booked, seat.Status);
        Assert.NotEqual(originalVersion, seat.Version);

        Assert.Equal(BookingId, result.Booking.Id);
        Assert.Equal(EventSeatId, result.Booking.EventSeatId);
        Assert.Equal(5, result.Booking.SeatNumber);
        Assert.Equal(
            BookingStatus.Confirmed,
            result.Booking.Status);

        _bookingRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventSeatRepository.Verify(
            x => x.Update(seat),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_BookedSeat_ThrowsConflict()
    {
        var seat = CreateEventSeat(
            seatStatus: SeatStatus.Booked);

        SetupExistingUser();
        SetupEventSeat(seat);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(
                UserId,
                CreateRequest()));

        Assert.Equal(SeatStatus.Booked, seat.Status);

        VerifyNoBookingChanges();
    }

    [Fact]
    public async Task CreateAsync_CancelledEvent_ThrowsConflict()
    {
        var seat = CreateEventSeat(
            eventStatus: EventStatus.Cancelled);

        SetupExistingUser();
        SetupEventSeat(seat);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(
                UserId,
                CreateRequest()));

        Assert.Equal(SeatStatus.Available, seat.Status);

        VerifyNoBookingChanges();
    }

    [Fact]
    public async Task CreateAsync_StartedEvent_ThrowsConflict()
    {
        var seat = CreateEventSeat(
            startTime: DateTime.UtcNow.AddMinutes(-10));

        SetupExistingUser();
        SetupEventSeat(seat);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(
                UserId,
                CreateRequest()));

        Assert.Equal(SeatStatus.Available, seat.Status);

        VerifyNoBookingChanges();
    }

    [Fact]
    public async Task CreateAsync_MissingUser_ThrowsNotFound()
    {
        _userRepository
            .Setup(x => x.GetByIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(
                UserId,
                CreateRequest()));

        _eventSeatRepository.Verify(
            x => x.GetByIdWithDetailsAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        VerifyNoBookingChanges();
    }

    [Fact]
    public async Task CreateAsync_MissingSeat_ThrowsNotFound()
    {
        SetupExistingUser();

        _eventSeatRepository
            .Setup(x => x.GetByIdWithDetailsAsync(
                EventSeatId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventSeat?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(
                UserId,
                CreateRequest()));

        VerifyNoBookingChanges();
    }

    [Fact]
    public async Task CancelAsync_ValidBooking_ReleasesSeat()
    {
        var seat = CreateEventSeat(
            seatStatus: SeatStatus.Booked);

        var originalVersion = seat.Version;
        var booking = CreateBooking(seat);

        SetupBooking(booking);

        await _service.CancelAsync(
            BookingId,
            UserId);

        Assert.Equal(
            BookingStatus.Deleted,
            booking.Status);

        Assert.NotNull(booking.DeletedAt);

        Assert.Equal(
            SeatStatus.Available,
            seat.Status);

        Assert.NotEqual(originalVersion, seat.Version);

        _bookingRepository.Verify(
            x => x.Update(booking),
            Times.Once);

        _eventSeatRepository.Verify(
            x => x.Update(seat),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_AfterDeadline_ThrowsConflict()
    {
        var seat = CreateEventSeat(
            seatStatus: SeatStatus.Booked,
            startTime: DateTime.UtcNow.AddMinutes(30));

        var originalVersion = seat.Version;
        var booking = CreateBooking(seat);

        SetupBooking(booking);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CancelAsync(
                BookingId,
                UserId));

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        Assert.Null(booking.DeletedAt);

        Assert.Equal(
            SeatStatus.Booked,
            seat.Status);

        Assert.Equal(originalVersion, seat.Version);

        VerifyNoBookingChanges();
    }

    private void SetupExistingUser()
    {
        _userRepository
            .Setup(x => x.GetByIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = UserId,
                FullName = "Test User",
                Email = "unit.test@example.com"
            });
    }

    private void SetupEventSeat(EventSeat seat)
    {
        _eventSeatRepository
            .Setup(x => x.GetByIdWithDetailsAsync(
                EventSeatId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(seat);
    }

    private void SetupBooking(Booking booking)
    {
        _bookingRepository
            .Setup(x => x.GetByIdWithDetailsAsync(
                BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
    }

    private static CreateBookingRequestDto CreateRequest()
    {
        return new CreateBookingRequestDto
        {
            EventSeatId = EventSeatId
        };
    }

    private static EventSeat CreateEventSeat(
        SeatStatus seatStatus = SeatStatus.Available,
        EventStatus eventStatus = EventStatus.Active,
        DateTime? startTime = null)
    {
        var start =
            startTime ?? DateTime.UtcNow.AddDays(2);

        return new EventSeat
        {
            Id = EventSeatId,
            EventId = 1,
            SeatId = 5,
            Status = seatStatus,
            Version = Guid.NewGuid(),

            Event = new Event
            {
                Id = 1,
                Name = "Test Event",
                StartTime = start,
                EndTime = start.AddHours(2),
                Status = eventStatus
            },

            Seat = new Seat
            {
                Id = 5,
                SeatNumber = 5
            }
        };
    }

    private static Booking CreateBooking(EventSeat seat)
    {
        return new Booking
        {
            Id = BookingId,
            UserId = UserId,
            EventSeatId = seat.Id,
            EventSeat = seat,
            Status = BookingStatus.Confirmed,
            BookedAt = DateTime.UtcNow.AddDays(-1),
            EventStartTimeAtBooking =
                seat.Event.StartTime,
            EventEndTimeAtBooking =
                seat.Event.EndTime
        };
    }

    private void VerifyNoBookingChanges()
    {
        _bookingRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.Verify(
            x => x.Update(It.IsAny<Booking>()),
            Times.Never);

        _eventSeatRepository.Verify(
            x => x.Update(It.IsAny<EventSeat>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}