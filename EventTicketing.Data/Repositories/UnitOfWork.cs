using System.Data;
using EventTicketing.Core.Interfaces;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore;
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
        if (_transaction is not null ||
            _context.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already active.");
        }

        _transaction = await _context.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
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

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteInTransactionAsync(
            async ct =>
            {
                await operation(ct);
                return true;
            },
            cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var existingTransaction =
            _context.Database.CurrentTransaction;

        if (existingTransaction is not null)
        {
            return await ExecuteWithSavepointAsync(
                existingTransaction,
                operation,
                cancellationToken);
        }

        await BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation(cancellationToken);

            await CommitTransactionAsync(cancellationToken);

            return result;
        }
        catch
        {
            try
            {
                if (_transaction is not null)
                {
                    await RollbackTransactionAsync(
                        CancellationToken.None);
                }
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }

            throw;
        }
    }

    private async Task<TResult> ExecuteWithSavepointAsync<TResult>(
        IDbContextTransaction transaction,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        if (transaction.GetDbTransaction().IsolationLevel !=
            IsolationLevel.Serializable)
        {
            throw new InvalidOperationException(
                "The existing transaction must use Serializable isolation.");
        }

        if (!transaction.SupportsSavepoints)
        {
            throw new InvalidOperationException(
                "The existing transaction does not support savepoints.");
        }

        var savepoint = $"sp_{Guid.NewGuid():N}";

        await transaction.CreateSavepointAsync(
            savepoint, cancellationToken);

        try
        {
            var result = await operation(cancellationToken);

            await transaction.ReleaseSavepointAsync(
                savepoint, cancellationToken);

            return result;
        }
        catch
        {
            try
            {
                await transaction.RollbackToSavepointAsync(
                    savepoint, CancellationToken.None);

                await transaction.ReleaseSavepointAsync(
                    savepoint, CancellationToken.None);
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }

            throw;
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