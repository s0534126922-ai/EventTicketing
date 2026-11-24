using EventTicketing.Core.DTOs.EventSeats;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Exceptions;
using EventTicketing.Core.Interfaces;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Core.Interfaces.Services;
using EventTicketing.Service.Helpers;

namespace EventTicketing.Service.Services;

public class EventSeatService : IEventSeatService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventSeatRepository _eventSeatRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EventSeatService(
        IEventRepository eventRepository,
        IEventSeatRepository eventSeatRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _eventSeatRepository = eventSeatRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<EventSeatResponseDto>>
        GetByEventIdAsync(
            int eventId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
    {
        var eventEntity = await GetEventAsync(
            eventId, cancellationToken, readOnly: true);

        if (!isAdmin &&
            (eventEntity.Status != EventStatus.Active ||
             eventEntity.StartTime <= IsraelTimeHelper.UtcNow))
        {
            throw new NotFoundException(
                "Event not found.");
        }

        var seats = await _eventSeatRepository
            .GetByEventIdAsync(eventId, cancellationToken);

        return seats.Select(seat => new EventSeatResponseDto
        {
            Id = seat.Id,
            SeatNumber = seat.Seat.SeatNumber,
            Status = seat.Status
        }).ToList();
    }

    public async Task ReserveAsync(
        int eventId,
        AdminSeatReservationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ChangeReservationAsync(
            eventId,
            request,
            SeatStatus.Available,
            SeatStatus.ReservedByAdmin,
            cancellationToken);
    }

    public async Task ReleaseAsync(
        int eventId,
        AdminSeatReservationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ChangeReservationAsync(
            eventId,
            request,
            SeatStatus.ReservedByAdmin,
            SeatStatus.Available,
            cancellationToken);
    }

    private async Task ChangeReservationAsync(
        int eventId,
        AdminSeatReservationRequestDto request,
        SeatStatus requiredStatus,
        SeatStatus newStatus,
        CancellationToken cancellationToken)
    {
        if (request?.EventSeatIds is null ||
            request.EventSeatIds.Count == 0)
        {
            throw new ValidationException(
                "At least one event seat is required.");
        }

        var ids = request.EventSeatIds;

        if (ids.Any(id => id <= 0) ||
            ids.Distinct().Count() != ids.Count)
        {
            throw new ValidationException(
                "Event seat IDs must be positive and unique.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var eventEntity = await GetEventAsync(
                    eventId, ct);

                if (eventEntity.Status != EventStatus.Active ||
                    eventEntity.StartTime <= IsraelTimeHelper.UtcNow)
                {
                    throw new ConflictException(
                        "Seats can be managed only before an active event starts.");
                }

                var seats = await _eventSeatRepository.GetByIdsAsync(
                    eventId, ids, ct);

                if (seats.Count != ids.Count)
                {
                    throw new NotFoundException(
                        "One or more event seats were not found.");
                }

                if (seats.Any(seat => seat.Status != requiredStatus))
                {
                    throw new ConflictException(
                        "One or more seats do not have the required status.");
                }

                foreach (var seat in seats)
                {
                    seat.Status = newStatus;
                    seat.Version = Guid.NewGuid();

                    _eventSeatRepository.Update(seat);
                }

                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);
    }

    private async Task<Event> GetEventAsync(
        int eventId,
        CancellationToken cancellationToken,
        bool readOnly = false)
    {
        if (eventId <= 0)
        {
            throw new ValidationException(
                "A valid event ID is required.");
        }

        var eventEntity = readOnly
            ? await _eventRepository.GetByIdWithDetailsReadOnlyAsync(
                eventId, cancellationToken)
            : await _eventRepository.GetByIdWithDetailsAsync(
                eventId, cancellationToken);

        if (eventEntity is null)
        {
            throw new NotFoundException(
                "Event not found.");
        }

        return eventEntity;
    }
}