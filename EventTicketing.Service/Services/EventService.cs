
using AutoMapper;
using EventTicketing.Core.DTOs.Events;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Exceptions;
using EventTicketing.Core.Interfaces;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Core.Interfaces.Services;
using EventTicketing.Service.Helpers;

namespace EventTicketing.Service.Services;

public class EventService : IEventService
{
    private const int PageSize = 10;

    private readonly IEventRepository _eventRepository;
    private readonly IHallRepository _hallRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventSeatRepository _eventSeatRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EventService(
        IEventRepository eventRepository,
        IHallRepository hallRepository,
        IBookingRepository bookingRepository,
        IEventSeatRepository eventSeatRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _hallRepository = hallRepository;
        _bookingRepository = bookingRepository;
        _eventSeatRepository = eventSeatRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedEventsResponseDto> SearchAsync(
        EventSearchRequestDto request,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ValidationException(
                "Search request is required.");

        if (request.PageNumber < 1)
            throw new ValidationException(
                "Page number must be greater than zero.");

        if (request.HallId.HasValue &&
            request.HallId.Value <= 0)
            throw new ValidationException(
                "Hall ID must be greater than zero.");

        if (request.Status.HasValue &&
            !Enum.IsDefined(request.Status.Value))
            throw new ValidationException(
                "Invalid event status.");

        await _eventRepository.MarkFinishedEventsAsync(
            IsraelTimeHelper.UtcNow,
            cancellationToken);

        var result = await _eventRepository.SearchAsync(
            request.Search?.Trim(),
            request.HallId,
            isAdmin ? request.Status : EventStatus.Active,
            isAdmin,
            request.Descending,
            request.PageNumber,
            PageSize,
            IsraelTimeHelper.UtcNow,
            cancellationToken);

        return new PagedEventsResponseDto
        {
            Items = _mapper.Map<List<EventResponseDto>>(
                result.Items),
            TotalCount = result.TotalCount,
            PageNumber = request.PageNumber,
            PageSize = PageSize
        };
    }

    public async Task<EventResponseDto> GetByIdAsync(
        int id,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        await _eventRepository.MarkFinishedEventsAsync(
            IsraelTimeHelper.UtcNow,
            cancellationToken);

        var eventEntity =
            await GetEventAsync(id, cancellationToken);

        if (!isAdmin &&
            (eventEntity.Status != EventStatus.Active ||
             eventEntity.StartTime <= IsraelTimeHelper.UtcNow))
        {
            throw new NotFoundException(
                "Event not found.");
        }

        return _mapper.Map<EventResponseDto>(
            eventEntity);
    }

    public async Task<EventResponseDto> CreateAsync(
        CreateEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ValidationException(
                "Event request is required.");

        var name = ValidateName(request.Name);
        var description =
            ValidateDescription(request.Description);

        ValidateHallId(request.HallId);

        var (startUtc, endUtc) = ValidateTimes(
            request.StartTime, request.EndTime);

        if (startUtc < IsraelTimeHelper.UtcNow.AddHours(24))
            throw new ValidationException(
                "Events must be created at least 24 hours in advance.");

        var hall = await GetHallWithSeatsAsync(
            request.HallId, cancellationToken);

        await EnsureNoScheduleConflictAsync(
            hall.Id,
            startUtc,
            endUtc,
            null,
            cancellationToken);

        var eventEntity = new Event
        {
            Name = name,
            Description = description,
            HallId = hall.Id,
            Hall = hall,
            StartTime = startUtc,
            EndTime = endUtc,
            Status = EventStatus.Active
        };

        foreach (var seat in hall.Seats)
        {
            eventEntity.EventSeats.Add(new EventSeat
            {
                SeatId = seat.Id,
                Status = SeatStatus.Available
            });
        }

        hall.HasEverHostedEvent = true;

        await _eventRepository.AddAsync(
            eventEntity, cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<EventResponseDto>(
            eventEntity);
    }

    public async Task UpdateAsync(
        int id,
        UpdateEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ValidationException(
                "Event request is required.");

        var eventEntity =
            await GetEventAsync(id, cancellationToken);

        EnsureEditable(eventEntity);

        var name = ValidateName(request.Name);
        var description =
            ValidateDescription(request.Description);

        ValidateHallId(request.HallId);

        var (startUtc, endUtc) = ValidateTimes(
            request.StartTime, request.EndTime);

        var hasBookings =
            await _eventRepository.HasAnyBookingsAsync(
                id, cancellationToken);

        var oldDate = IsraelTimeHelper
            .ToIsrael(eventEntity.StartTime).Date;

        var newDate = IsraelTimeHelper
            .ToIsrael(startUtc).Date;

        bool hallChanged =
            request.HallId != eventEntity.HallId;

        bool dateChanged = oldDate != newDate;

        bool timesChanged =
            startUtc != eventEntity.StartTime ||
            endUtc != eventEntity.EndTime;

        if (hasBookings &&
            (hallChanged || dateChanged || timesChanged))
        {
            throw new ConflictException(
                "Hall, date and times cannot be changed " +
                "after any user booking has existed.");
        }

        if (IsraelTimeHelper.IsraelNow >= newDate)
        {
            throw new ConflictException(
                "The new event date has passed its editing deadline.");
        }

        if (startUtc <= IsraelTimeHelper.UtcNow)
        {
            throw new ValidationException(
                "Event start time must be in the future.");
        }

        await EnsureNoScheduleConflictAsync(
            request.HallId,
            startUtc,
            endUtc,
            id,
            cancellationToken);

        if (hallChanged)
        {
            var newHall = await GetHallWithSeatsAsync(
                request.HallId, cancellationToken);

            var oldEventSeats =
                eventEntity.EventSeats.ToList();

            _eventSeatRepository.RemoveRange(
                oldEventSeats);

            eventEntity.EventSeats.Clear();

            var newEventSeats = newHall.Seats
                .Select(seat => new EventSeat
                {
                    EventId = eventEntity.Id,
                    SeatId = seat.Id,
                    Status = SeatStatus.Available
                })
                .ToList();

            await _eventSeatRepository.AddRangeAsync(
                newEventSeats, cancellationToken);

            eventEntity.HallId = newHall.Id;
            eventEntity.Hall = newHall;

            newHall.HasEverHostedEvent = true;
        }

        eventEntity.Name = name;
        eventEntity.Description = description;
        eventEntity.StartTime = startUtc;
        eventEntity.EndTime = endUtc;

        _eventRepository.Update(eventEntity);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    public async Task CancelAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var eventEntity =
            await GetEventAsync(id, cancellationToken);

        if (eventEntity.Status != EventStatus.Active)
            throw new ConflictException(
                "Only active events can be cancelled.");

        if (eventEntity.StartTime <= IsraelTimeHelper.UtcNow)
            throw new ConflictException(
                "An event cannot be cancelled after it starts.");

        var bookings =
            await _bookingRepository.GetConfirmedByEventIdAsync(
                id, cancellationToken);

        var bookedSeatIds = bookings
            .Select(booking => booking.EventSeatId)
            .ToHashSet();

        eventEntity.Status = EventStatus.Cancelled;

        foreach (var booking in bookings)
        {
            booking.Status = BookingStatus.CancelledByEvent;
            _bookingRepository.Update(booking);
        }

        foreach (var eventSeat in eventEntity.EventSeats)
        {
            if (bookedSeatIds.Contains(eventSeat.Id))
            {
                if (eventSeat.Status != SeatStatus.Booked)
                {
                    throw new ConflictException(
                        "Booking and seat status are inconsistent.");
                }

                eventSeat.Status = SeatStatus.Available;
                eventSeat.Version = Guid.NewGuid();

                _eventSeatRepository.Update(eventSeat);
            }
        }

        _eventRepository.Update(eventEntity);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    public async Task RestoreAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var eventEntity =
            await GetEventAsync(id, cancellationToken);

        if (eventEntity.Status != EventStatus.Cancelled)
            throw new ConflictException(
                "Only cancelled events can be restored.");

        var eventDate = IsraelTimeHelper
            .ToIsrael(eventEntity.StartTime).Date;

        if (IsraelTimeHelper.IsraelNow >= eventDate)
        {
            throw new ConflictException(
                "The event restoration deadline has passed.");
        }

        await EnsureNoScheduleConflictAsync(
            eventEntity.HallId,
            eventEntity.StartTime,
            eventEntity.EndTime,
            id,
            cancellationToken);

        eventEntity.Status = EventStatus.Active;

        _eventRepository.Update(eventEntity);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<Event> GetEventAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var eventEntity =
            await _eventRepository.GetByIdWithDetailsAsync(
                id, cancellationToken);

        if (eventEntity is null)
            throw new NotFoundException(
                "Event not found.");

        return eventEntity;
    }

    private async Task<Hall> GetHallWithSeatsAsync(
        int hallId,
        CancellationToken cancellationToken)
    {
        var hall = await _hallRepository.GetByIdWithSeatsAsync(
            hallId, cancellationToken);

        if (hall is null)
            throw new NotFoundException(
                "Hall not found.");

        if (hall.SeatCount <= 0 ||
            hall.Seats.Count != hall.SeatCount ||
            hall.Seats.Select(seat => seat.SeatNumber)
                .Distinct().Count() != hall.SeatCount)
        {
            throw new ConflictException(
                "Hall seats are not configured correctly.");
        }

        return hall;
    }

    private async Task EnsureNoScheduleConflictAsync(
        int hallId,
        DateTime startUtc,
        DateTime endUtc,
        int? excludedEventId,
        CancellationToken cancellationToken)
    {
        var conflict =
            await _eventRepository.HasScheduleConflictAsync(
                hallId,
                startUtc,
                endUtc,
                excludedEventId,
                cancellationToken);

        if (conflict)
            throw new ConflictException(
                "Another event is scheduled in this hall.");
    }

    private static void EnsureEditable(Event eventEntity)
    {
        if (eventEntity.Status != EventStatus.Active)
            throw new ConflictException(
                "Only active events can be edited.");

        var eventDate = IsraelTimeHelper
            .ToIsrael(eventEntity.StartTime).Date;

        if (IsraelTimeHelper.IsraelNow >= eventDate)
            throw new ConflictException(
                "The event editing deadline has passed.");
    }

    private static (DateTime Start, DateTime End)
        ValidateTimes(DateTime start, DateTime end)
    {
        if (start == default || end == default)
            throw new ValidationException(
                "Start and end times are required.");

        if (start.Kind == DateTimeKind.Utc ||
            end.Kind == DateTimeKind.Utc)
            throw new ValidationException(
                "Event times must be provided in Israel local time.");

        if (start.Date != end.Date)
            throw new ValidationException(
                "An event must start and end on the same day.");

        if (start.TimeOfDay < TimeSpan.FromHours(8))
            throw new ValidationException(
                "An event cannot start before 08:00.");

        if (end <= start)
            throw new ValidationException(
                "End time must be later than start time.");

        return (
            IsraelTimeHelper.ToUtc(start),
            IsraelTimeHelper.ToUtc(end)
        );
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException(
                "Event name is required.");

        var trimmed = name.Trim();

        if (trimmed.Length > 100)
            throw new ValidationException(
                "Event name cannot exceed 100 characters.");

        return trimmed;
    }

    private static string? ValidateDescription(
        string? description)
    {
        var trimmed = description?.Trim();

        if (trimmed?.Length > 1000)
            throw new ValidationException(
                "Description cannot exceed 1000 characters.");

        return string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed;
    }

    private static void ValidateHallId(int hallId)
    {
        if (hallId <= 0)
            throw new ValidationException(
                "A valid hall is required.");
    }
}
