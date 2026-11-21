using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Core.DTOs.Bookings;

public class CreateBookingRequestDto
{
    [Range(1, int.MaxValue)]
    public int EventSeatId { get; set; }
}