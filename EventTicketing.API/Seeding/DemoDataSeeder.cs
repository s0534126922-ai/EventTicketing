using System.ComponentModel.DataAnnotations;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Interfaces.Services;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.API.Seeding;

public static class DemoDataSeeder
{
    private const string FirstHallName = "אולם הדגמה מרכזי";
    private const string SecondHallName = "אולם הדגמה משני";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("SeedDemo:Enabled"))
        {
            return;
        }

        var name = configuration["SeedDemo:Name"]?.Trim();

        var email = configuration["SeedDemo:Email"]?
            .Trim()
            .ToLowerInvariant();

        var password = configuration["SeedDemo:Password"];

        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > 100 ||
            string.IsNullOrWhiteSpace(email) ||
            email.Length > 256 ||
            !new EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length < 8)
        {
            throw new InvalidOperationException(
                "SeedDemo configuration is incomplete or invalid.");
        }

        await using var scope = services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<EventTicketingDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher>();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DemoDataSeeder));

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var existingUser = await dbContext.Users
            .SingleOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);

        if (existingUser is not null &&
            existingUser.Role != UserRole.User)
        {
            throw new InvalidOperationException(
                "Seed demo email belongs to an administrator.");
        }

        if (existingUser is null)
        {
            dbContext.Users.Add(new User
            {
                FullName = name,
                Email = email,
                PasswordHash =
                    passwordHasher.HashPassword(password),
                Role = UserRole.User
            });
        }

        var existingDemoHall = await dbContext.Halls
            .AnyAsync(
                hall => hall.Name == FirstHallName ||
                        hall.Name == SecondHallName,
                cancellationToken);

        if (existingDemoHall)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            logger.LogInformation(
                "Demo hall data already exists. Hall and event seeding skipped.");

            return;
        }

        var firstHall = CreateHall(
            FirstHallName,
            30);

        var secondHall = CreateHall(
            SecondHallName,
            50);

        dbContext.Halls.AddRange(
            firstHall,
            secondHall);

        var eventNames = new[]
        {
            "סדנת תכנות למתחילים",
            "כנס חדשנות וטכנולוגיה",
            "הרצאה בנושא בינה מלאכותית",
            "סדנת עיצוב גרפי",
            "כנס פיתוח תוכנה",
            "הרצאה בנושא ניהול זמן",
            "סדנת צילום מקצועי",
            "כנס יזמות עסקית",
            "סדנת פיתוח אתרים",
            "הרצאה בנושא אבטחת מידע",
            "סדנת ניהול פרויקטים",
            "כנס טכנולוגיות העתיד"
        };

        var israelTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Jerusalem");

        var israelNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            israelTimeZone);

        var firstEventDate = israelNow.Date.AddDays(3);

        for (int i = 0; i < eventNames.Length; i++)
        {
            var hall = i % 2 == 0
                ? firstHall
                : secondHall;

            var localStartTime = DateTime.SpecifyKind(
                firstEventDate.AddDays(i).AddHours(18),
                DateTimeKind.Unspecified);

            var localEndTime = localStartTime.AddHours(2);

            var startTime = TimeZoneInfo.ConvertTimeToUtc(
                localStartTime,
                israelTimeZone);

            var endTime = TimeZoneInfo.ConvertTimeToUtc(
                localEndTime,
                israelTimeZone);

            var eventEntity = new Event
            {
                Name = eventNames[i],
                Description = $"אירוע הדגמה: {eventNames[i]}",
                Hall = hall,
                StartTime = startTime,
                EndTime = endTime,
                Status = EventStatus.Active
            };

            dbContext.Events.Add(eventEntity);

            foreach (var seat in hall.Seats)
            {
                dbContext.EventSeats.Add(new EventSeat
                {
                    Event = eventEntity,
                    Seat = seat,
                    Status = SeatStatus.Available,
                    Version = Guid.NewGuid()
                });
            }

            hall.HasEverHostedEvent = true;
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Demo data created successfully. " +
            "Halls: {HallCount}, Events: {EventCount}",
            2,
            eventNames.Length);
    }

    private static Hall CreateHall(
        string name,
        int seatCount)
    {
        var hall = new Hall
        {
            Name = name,
            SeatCount = seatCount
        };

        for (int seatNumber = 1;
             seatNumber <= seatCount;
             seatNumber++)
        {
            hall.Seats.Add(new Seat
            {
                Hall = hall,
                SeatNumber = seatNumber
            });
        }

        return hall;
    }
}