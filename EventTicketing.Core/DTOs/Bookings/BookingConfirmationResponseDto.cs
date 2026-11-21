using System;
using System.Collections.Generic;
using System.Text;

namespace EventTicketing.Core.DTOs.Bookings;

public class BookingConfirmationResponseDto
{
    public BookingResponseDto Booking { get; set; } = new();

    public string Message { get; set; } = string.Empty;
}