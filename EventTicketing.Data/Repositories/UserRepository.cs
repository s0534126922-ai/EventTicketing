using EventTicketing.Core.Entities;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly EventTicketingDbContext _context;

    public UserRepository(EventTicketingDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(
                user => user.Email == email,
                cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(
            user,
            cancellationToken);
    }
}