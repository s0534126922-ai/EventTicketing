
using System.ComponentModel.DataAnnotations;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Interfaces.Services;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.API.Seeding;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var name = configuration["SeedAdmin:Name"]?.Trim();
        var email = configuration["SeedAdmin:Email"]?
            .Trim()
            .ToLowerInvariant();
        var password = configuration["SeedAdmin:Password"];

        var hasAnyConfiguration =
            name is not null ||
            email is not null ||
            password is not null;

        if (!hasAnyConfiguration)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > 100 ||
            string.IsNullOrWhiteSpace(email) ||
            email.Length > 256 ||
            !new EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length < 8)
        {
            throw new InvalidOperationException(
                "SeedAdmin configuration is incomplete or invalid.");
        }

        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<EventTicketingDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher>();

        var existingUser = await dbContext.Users
            .SingleOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.Role != UserRole.Admin)
            {
                throw new InvalidOperationException(
                    "Seed admin email belongs to a regular user.");
            }

            return;
        }

        var admin = new User
        {
            FullName = name,
            Email = email,
            PasswordHash = passwordHasher.HashPassword(password),
            Role = UserRole.Admin
        };

        dbContext.Users.Add(admin);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
