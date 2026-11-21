using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.DTOs.EventSeats;

namespace EventTicketing.Core.Interfaces.Services;

public interface IEventSeatService
{
    Task<IReadOnlyList<EventSeatResponseDto>> GetByEventIdAsync(
        int eventId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task ReserveAsync(
        int eventId,
        AdminSeatReservationRequestDto request,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        int eventId,
        AdminSeatReservationRequestDto request,
        CancellationToken cancellationToken = default);
}
