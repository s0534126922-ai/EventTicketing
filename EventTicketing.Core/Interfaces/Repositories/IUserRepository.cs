using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.Entities;

namespace EventTicketing.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);
}
