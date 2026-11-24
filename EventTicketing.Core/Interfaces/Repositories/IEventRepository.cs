using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;

namespace EventTicketing.Core.Interfaces.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Event?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Event?> GetByIdWithDetailsReadOnlyAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Event> Items, int TotalCount)> SearchAsync(
        string? search,
        int? hallId,
        EventStatus? status,
        bool includeAllStatuses,
        bool descending,
        int pageNumber,
        int pageSize,
        DateTime currentTime,
        CancellationToken cancellationToken = default);

    Task<bool> HasScheduleConflictAsync(
        int hallId,
        DateTime startTime,
        DateTime endTime,
        int? excludedEventId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyBookingsAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Event eventEntity,
        CancellationToken cancellationToken = default);

    void Update(Event eventEntity);

    Task<int> MarkFinishedEventsAsync(
        DateTime currentTime,
        CancellationToken cancellationToken = default);
}