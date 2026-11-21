using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.Enums;

namespace EventTicketing.Core.DTOs.Bookings;

public class BookingResponseDto
{
    public int Id { get; set; }

    public int EventSeatId { get; set; }

    public int SeatNumber { get; set; }

    public BookingStatus Status { get; set; }

    public DateTime BookedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime EventStartTimeAtBooking { get; set; }

    public DateTime EventEndTimeAtBooking { get; set; }
}