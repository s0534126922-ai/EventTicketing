using EventTicketing.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace EventTicketing.Data.Context;

public class EventTicketingDbContext : DbContext
{
    public EventTicketingDbContext(
        DbContextOptions<EventTicketingDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventSeat> EventSeats => Set<EventSeat>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureHalls(modelBuilder);
        ConfigureSeats(modelBuilder);
        ConfigureEvents(modelBuilder);
        ConfigureEventSeats(modelBuilder);
        ConfigureBookings(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.Role)
                .HasConversion<int>();
        });
    }

    private static void ConfigureHalls(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hall>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired();

            entity.Property(x => x.SeatCount)
                .IsRequired();

            entity.Property(x => x.HasEverHostedEvent)
                .HasDefaultValue(false);
        });
    }

    private static void ConfigureSeats(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Hall)
                .WithMany(x => x.Seats)
                .HasForeignKey(x => x.HallId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.HallId,
                x.SeatNumber
            }).IsUnique();
        });
    }

    private static void ConfigureEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.HasOne(x => x.Hall)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.HallId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.HallId,
                x.StartTime
            });
        });
    }

    private static void ConfigureEventSeats(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventSeat>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Event)
                .WithMany(x => x.EventSeats)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Seat)
                .WithMany(x => x.EventSeats)
                .HasForeignKey(x => x.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.EventId,
                x.SeatId
            }).IsUnique();

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.Property(x => x.Version)
                .IsConcurrencyToken();
        });
    }

    private static void ConfigureBookings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EventSeat)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.EventSeatId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.EventSeatId);
        });
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        UpdateConcurrencyTokens();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<EventSeat>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }
}
