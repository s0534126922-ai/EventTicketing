using EventTicketing.Core.Entities;

namespace EventTicketing.Core.Interfaces.Repositories;

public interface IHallRepository
{
    Task<IReadOnlyList<Hall>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Hall?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Hall?> GetByIdReadOnlyAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Hall?> GetByIdWithSeatsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Hall hall,
        CancellationToken cancellationToken = default);

    void Update(Hall hall);

    void Delete(Hall hall);

    Task<bool> HasEverHostedEventAsync(
        int hallId,
        CancellationToken cancellationToken = default);
}