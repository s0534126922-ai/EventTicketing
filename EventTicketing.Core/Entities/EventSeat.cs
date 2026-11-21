using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.Enums;

namespace EventTicketing.Core.Entities;

public class EventSeat
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    public SeatStatus Status { get; set; } = SeatStatus.Available;

    public Guid Version { get; set; } = Guid.NewGuid();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
