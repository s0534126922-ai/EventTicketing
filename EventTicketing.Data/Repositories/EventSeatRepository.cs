using System;
using System.Collections.Generic;
using System.Text;


using EventTicketing.Core.Entities;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Data.Repositories;

public class EventSeatRepository : IEventSeatRepository
{
    private readonly EventTicketingDbContext _context;

    public EventSeatRepository(EventTicketingDbContext context)
    {
        _context = context;
    }

    public async Task<EventSeat?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.EventSeats
            .FirstOrDefaultAsync(
                eventSeat => eventSeat.Id == id,
                cancellationToken);
    }

    public async Task<EventSeat?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.EventSeats
            .Include(eventSeat => eventSeat.Seat)
            .Include(eventSeat => eventSeat.Event)
                .ThenInclude(eventEntity => eventEntity.Hall)
            .FirstOrDefaultAsync(
                eventSeat => eventSeat.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<EventSeat>> GetByEventIdAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EventSeats
            .AsNoTracking()
            .Include(eventSeat => eventSeat.Seat)
            .Where(eventSeat =>
                eventSeat.EventId == eventId)
            .OrderBy(eventSeat =>
                eventSeat.Seat.SeatNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventSeat>> GetByIdsAsync(
        int eventId,
        IReadOnlyCollection<int> eventSeatIds,
        CancellationToken cancellationToken = default)
    {
        if (eventSeatIds.Count == 0)
        {
            return Array.Empty<EventSeat>();
        }

        return await _context.EventSeats
            .Where(eventSeat =>
                eventSeat.EventId == eventId &&
                eventSeatIds.Contains(eventSeat.Id))
            .OrderBy(eventSeat => eventSeat.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<EventSeat> eventSeats,
        CancellationToken cancellationToken = default)
    {
        await _context.EventSeats.AddRangeAsync(
            eventSeats,
            cancellationToken);
    }

    public void RemoveRange(
        IEnumerable<EventSeat> eventSeats)
    {
        _context.EventSeats.RemoveRange(eventSeats);
    }

    public void Update(EventSeat eventSeat)
    {
        _context.EventSeats.Update(eventSeat);
    }
}
