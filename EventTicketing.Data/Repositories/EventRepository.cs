using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly EventTicketingDbContext _context;

    public EventRepository(EventTicketingDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .FirstOrDefaultAsync(
                eventEntity => eventEntity.Id == id,
                cancellationToken);
    }

    public Task<Event?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return GetByIdWithDetailsInternalAsync(
            id, true, cancellationToken);
    }

    public Task<Event?> GetByIdWithDetailsReadOnlyAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return GetByIdWithDetailsInternalAsync(
            id, false, cancellationToken);
    }

    private async Task<Event?> GetByIdWithDetailsInternalAsync(
        int id,
        bool tracking,
        CancellationToken cancellationToken)
    {
        IQueryable<Event> query = _context.Events;

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query
            .Include(eventEntity => eventEntity.Hall)
            .Include(eventEntity => eventEntity.EventSeats)
                .ThenInclude(eventSeat => eventSeat.Seat)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                eventEntity => eventEntity.Id == id,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> SearchAsync(
        string? search,
        int? hallId,
        EventStatus? status,
        bool includeAllStatuses,
        bool descending,
        int pageNumber,
        int pageSize,
        DateTime currentTime,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Event> query = _context.Events
            .AsNoTracking();

        if (!includeAllStatuses)
        {
            query = query.Where(eventEntity =>
                eventEntity.Status == EventStatus.Active &&
                eventEntity.StartTime > currentTime);
        }
        else if (status.HasValue)
        {
            query = query.Where(eventEntity =>
                eventEntity.Status == status.Value);
        }

        if (hallId.HasValue)
        {
            query = query.Where(eventEntity =>
                eventEntity.HallId == hallId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string searchText = search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            query = query.Where(eventEntity =>
                EF.Functions.ILike(
                    eventEntity.Name,
                    "%" + searchText + "%",
                    "\\"));
        }

        int totalCount = await query.CountAsync(
            cancellationToken);

        query = descending
            ? query.OrderByDescending(
                    eventEntity => eventEntity.StartTime)
                .ThenByDescending(
                    eventEntity => eventEntity.Id)
            : query.OrderBy(
                    eventEntity => eventEntity.StartTime)
                .ThenBy(
                    eventEntity => eventEntity.Id);

        int skip = (pageNumber - 1) * pageSize;

        List<Event> items = await query
            .Include(eventEntity => eventEntity.Hall)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> HasScheduleConflictAsync(
        int hallId,
        DateTime startTime,
        DateTime endTime,
        int? excludedEventId,
        CancellationToken cancellationToken = default)
    {
        DateTime conflictStart = startTime.AddHours(-1);
        DateTime conflictEnd = endTime.AddHours(1);

        return await _context.Events
            .AnyAsync(eventEntity =>
                eventEntity.HallId == hallId &&
                eventEntity.Status != EventStatus.Cancelled &&
                (!excludedEventId.HasValue ||
                 eventEntity.Id != excludedEventId.Value) &&
                eventEntity.StartTime < conflictEnd &&
                eventEntity.EndTime > conflictStart,
                cancellationToken);
    }

    public async Task<bool> HasAnyBookingsAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AnyAsync(booking =>
                booking.EventSeat.EventId == eventId,
                cancellationToken);
    }

    public async Task AddAsync(
        Event eventEntity,
        CancellationToken cancellationToken = default)
    {
        await _context.Events.AddAsync(
            eventEntity,
            cancellationToken);
    }

    public void Update(Event eventEntity)
    {
        _context.Events.Update(eventEntity);
    }

    public async Task<int> MarkFinishedEventsAsync(
        DateTime currentTime,
        CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Where(eventEntity =>
                eventEntity.Status == EventStatus.Active &&
                eventEntity.EndTime <= currentTime)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    eventEntity => eventEntity.Status,
                    EventStatus.Finished),
                cancellationToken);
    }
}