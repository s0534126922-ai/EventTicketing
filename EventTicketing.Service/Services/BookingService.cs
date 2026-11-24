using EventTicketing.Core.DTOs.Bookings;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Exceptions;
using EventTicketing.Core.Interfaces;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Core.Interfaces.Services;
using EventTicketing.Service.Helpers;

namespace EventTicketing.Service.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventSeatRepository _eventSeatRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BookingService(
        IBookingRepository bookingRepository,
        IEventSeatRepository eventSeatRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _eventSeatRepository = eventSeatRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BookingConfirmationResponseDto> CreateAsync(
        int userId,
        CreateBookingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        if (request is null || request.EventSeatId <= 0)
        {
            throw new ValidationException(
                "A valid event seat ID is required.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var user = await _userRepository.GetByIdAsync(
                    userId, ct);

                if (user is null)
                {
                    throw new NotFoundException(
                        "User not found.");
                }

                var seat = await _eventSeatRepository
                    .GetByIdWithDetailsAsync(
                        request.EventSeatId, ct);

                if (seat is null)
                {
                    throw new NotFoundException(
                        "Event seat not found.");
                }

                var eventEntity = seat.Event;
                var now = IsraelTimeHelper.UtcNow;

                if (eventEntity.Status != EventStatus.Active ||
                    eventEntity.StartTime <= now)
                {
                    throw new ConflictException(
                        "Bookings are not available for this event.");
                }

                if (seat.Status != SeatStatus.Available)
                {
                    throw new ConflictException(
                        "The selected seat is not available.");
                }

                var booking = new Booking
                {
                    UserId = userId,
                    EventSeatId = seat.Id,
                    EventSeat = seat,
                    BookedAt = now,
                    Status = BookingStatus.Confirmed,
                    EventStartTimeAtBooking = eventEntity.StartTime,
                    EventEndTimeAtBooking = eventEntity.EndTime
                };

                seat.Status = SeatStatus.Booked;
                seat.Version = Guid.NewGuid();

                await _bookingRepository.AddAsync(
                    booking, ct);

                _eventSeatRepository.Update(seat);

                await _unitOfWork.SaveChangesAsync(ct);

                return new BookingConfirmationResponseDto
                {
                    Booking = ToResponseDto(booking),
                    Message = "Booking confirmed successfully."
                };
            },
            cancellationToken);
    }

    public async Task<BookingResponseDto> GetByIdAsync(
        int bookingId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        if (bookingId <= 0)
        {
            throw new ValidationException(
                "A valid booking ID is required.");
        }

        var booking = await _bookingRepository
            .GetByIdForUserReadOnlyAsync(
                bookingId,
                userId,
                cancellationToken);

        if (booking is null)
        {
            throw new NotFoundException(
                "Booking not found.");
        }

        return ToResponseDto(booking);
    }

    public async Task<UserBookingsResponseDto> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        var bookings = await _bookingRepository
            .GetByUserIdAsync(userId, cancellationToken);

        var events = bookings
            .GroupBy(booking => booking.EventSeat.EventId)
            .Select(group =>
            {
                var first = group.First();
                var eventEntity = first.EventSeat.Event;

                return new BookingEventResponseDto
                {
                    EventId = group.Key,
                    EventName = eventEntity.Name,
                    HallName = eventEntity.Hall.Name,
                    Bookings = group
                        .OrderBy(booking =>
                            booking.EventSeat.Seat.SeatNumber)
                        .ThenBy(booking => booking.Id)
                        .Select(ToResponseDto)
                        .ToList()
                };
            })
            .ToList();

        return new UserBookingsResponseDto
        {
            Events = events
        };
    }

    public async Task CancelAsync(
        int bookingId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        if (bookingId <= 0)
        {
            throw new ValidationException(
                "A valid booking ID is required.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var booking = await _bookingRepository
                    .GetByIdWithDetailsAsync(
                        bookingId, ct);

                if (booking is null || booking.UserId != userId)
                {
                    throw new NotFoundException(
                        "Booking not found.");
                }

                if (booking.Status != BookingStatus.Confirmed)
                {
                    throw new ConflictException(
                        "Only confirmed bookings can be cancelled.");
                }

                var seat = booking.EventSeat;
                var eventEntity = seat.Event;
                var now = IsraelTimeHelper.UtcNow;

                if (eventEntity.Status != EventStatus.Active)
                {
                    throw new ConflictException(
                        "The event is not active.");
                }

                if (now >= eventEntity.StartTime.AddHours(-1))
                {
                    throw new ConflictException(
                        "The booking cancellation deadline has passed.");
                }

                if (seat.Status != SeatStatus.Booked)
                {
                    throw new ConflictException(
                        "Booking and seat status are inconsistent.");
                }

                booking.Status = BookingStatus.Deleted;
                booking.DeletedAt = now;

                seat.Status = SeatStatus.Available;
                seat.Version = Guid.NewGuid();

                _bookingRepository.Update(booking);
                _eventSeatRepository.Update(seat);

                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);
    }

    private static BookingResponseDto ToResponseDto(
        Booking booking)
    {
        return new BookingResponseDto
        {
            Id = booking.Id,
            EventSeatId = booking.EventSeatId,
            SeatNumber = booking.EventSeat.Seat.SeatNumber,
            Status = booking.Status,
            BookedAt = booking.BookedAt,
            DeletedAt = booking.DeletedAt,
            EventStartTimeAtBooking =
                booking.EventStartTimeAtBooking,
            EventEndTimeAtBooking =
                booking.EventEndTimeAtBooking
        };
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ValidationException(
                "A valid user ID is required.");
        }
    }
}