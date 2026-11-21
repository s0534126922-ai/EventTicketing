
using EventTicketing.Core.Interfaces;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventTicketing.Data;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly EventTicketingDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(EventTicketingDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already active.");
        }

        _transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "No active transaction to commit.");
        }

        var transaction = _transaction;

        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            _transaction = null;
            await transaction.DisposeAsync();
        }
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "No active transaction to roll back.");
        }

        var transaction = _transaction;

        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _transaction = null;
            await transaction.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            var transaction = _transaction;
            _transaction = null;

            await transaction.DisposeAsync();
        }
    }
}
