using EventTicketing.Core.Entities;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Data.Repositories;

public class HallRepository : IHallRepository
{
    private readonly EventTicketingDbContext _context;

    public HallRepository(EventTicketingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Hall>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Halls
            .AsNoTracking()
            .OrderBy(h => h.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Hall?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return GetByIdInternalAsync(
            id, true, cancellationToken);
    }

    public Task<Hall?> GetByIdReadOnlyAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return GetByIdInternalAsync(
            id, false, cancellationToken);
    }

    private async Task<Hall?> GetByIdInternalAsync(
        int id,
        bool tracking,
        CancellationToken cancellationToken)
    {
        IQueryable<Hall> query = _context.Halls;

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            h => h.Id == id,
            cancellationToken);
    }

    public async Task<Hall?> GetByIdWithSeatsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Halls
            .Include(h => h.Seats)
            .FirstOrDefaultAsync(
                h => h.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Hall hall,
        CancellationToken cancellationToken = default)
    {
        await _context.Halls.AddAsync(
            hall,
            cancellationToken);
    }

    public void Update(Hall hall)
    {
        _context.Halls.Update(hall);
    }

    public void Delete(Hall hall)
    {
        _context.Seats.RemoveRange(hall.Seats);
        _context.Halls.Remove(hall);
    }

    public async Task<bool> HasEverHostedEventAsync(
        int hallId,
        CancellationToken cancellationToken = default)
    {
        var hall = await _context.Halls
            .AsNoTracking()
            .Where(h => h.Id == hallId)
            .Select(h => new
            {
                h.HasEverHostedEvent
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (hall is null)
            return false;

        if (hall.HasEverHostedEvent)
            return true;

        return await _context.Events.AnyAsync(
            e => e.HallId == hallId,
            cancellationToken);
    }
}