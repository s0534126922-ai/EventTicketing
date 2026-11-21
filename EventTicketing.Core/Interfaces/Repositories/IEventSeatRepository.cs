using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.Entities;

namespace EventTicketing.Core.Interfaces.Repositories;

public interface IEventSeatRepository
{
    Task<EventSeat?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<EventSeat?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventSeat>> GetByEventIdAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventSeat>> GetByIdsAsync(
        int eventId,
        IReadOnlyCollection<int> eventSeatIds,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<EventSeat> eventSeats,
        CancellationToken cancellationToken = default);

    void RemoveRange(
        IEnumerable<EventSeat> eventSeats);

    void Update(EventSeat eventSeat);
}