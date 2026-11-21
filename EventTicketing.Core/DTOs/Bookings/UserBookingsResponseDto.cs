using System;
using System.Collections.Generic;
using System.Text;

namespace EventTicketing.Core.DTOs.Bookings;

public class UserBookingsResponseDto
{
    public IReadOnlyList<BookingEventResponseDto> Events { get; set; }
        = Array.Empty<BookingEventResponseDto>();
}