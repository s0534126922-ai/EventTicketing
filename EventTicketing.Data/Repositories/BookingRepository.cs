using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Data.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly EventTicketingDbContext _context;

    public BookingRepository(EventTicketingDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(
                booking => booking.Id == id,
                cancellationToken);
    }

    public async Task<Booking?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(
                booking => booking.Id == id,
                cancellationToken);
    }

    public async Task<Booking?> GetByIdForUserReadOnlyAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                booking => booking.Id == id &&
                           booking.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .AsNoTracking()
            .Where(booking => booking.UserId == userId)
            .OrderByDescending(booking =>
                booking.EventStartTimeAtBooking)
            .ThenBy(booking =>
                booking.EventSeat.Seat.SeatNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetByEventIdAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(booking =>
                booking.EventSeat.EventId == eventId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetConfirmedByEventIdAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(booking =>
                booking.EventSeat.EventId == eventId &&
                booking.Status == BookingStatus.Confirmed)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>?> GetEventBookingsReadOnlyAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.Events.AnyAsync(
                eventEntity => eventEntity.Id == eventId,
                cancellationToken))
        {
            return null;
        }

        return await _context.Bookings
            .AsNoTracking()
            .Where(booking => booking.EventSeat.EventId == eventId)
            .Include(booking => booking.User)
            .Include(booking => booking.EventSeat)
                .ThenInclude(eventSeat => eventSeat.Seat)
            .OrderBy(booking => booking.EventSeat.Seat.SeatNumber)
            .ThenBy(booking => booking.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        await _context.Bookings.AddAsync(
            booking,
            cancellationToken);
    }

    public void Update(Booking booking)
    {
        _context.Bookings.Update(booking);
    }

    private IQueryable<Booking> QueryWithDetails()
    {
        return _context.Bookings
            .Include(booking => booking.EventSeat)
                .ThenInclude(eventSeat => eventSeat.Seat)
            .Include(booking => booking.EventSeat)
                .ThenInclude(eventSeat => eventSeat.Event)
                    .ThenInclude(eventEntity => eventEntity.Hall)
            .AsSplitQuery();
    }
}