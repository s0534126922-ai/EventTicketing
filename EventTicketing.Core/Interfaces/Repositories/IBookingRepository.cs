using EventTicketing.Core.Entities;

namespace EventTicketing.Core.Interfaces.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdForUserReadOnlyAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetByEventIdAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetConfirmedByEventIdAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>?> GetEventBookingsReadOnlyAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default);

    void Update(Booking booking);
}