using System;
using System.Collections.Generic;
using System.Text;

namespace EventTicketing.Core.DTOs.Bookings;

public class BookingEventResponseDto
{
    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string HallName { get; set; } = string.Empty;

    public IReadOnlyList<BookingResponseDto> Bookings { get; set; }
        = Array.Empty<BookingResponseDto>();
}