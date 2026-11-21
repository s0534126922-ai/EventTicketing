using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.Enums;

namespace EventTicketing.Core.Entities;

public class Booking
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int EventSeatId { get; set; }
    public EventSeat EventSeat { get; set; } = null!;

    public DateTime BookedAt { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public DateTime? DeletedAt { get; set; }

    public DateTime EventStartTimeAtBooking { get; set; }
    public DateTime EventEndTimeAtBooking { get; set; }
}
