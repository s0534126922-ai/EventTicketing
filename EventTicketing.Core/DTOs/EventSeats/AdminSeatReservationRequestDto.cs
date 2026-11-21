using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Core.DTOs.EventSeats;

public class AdminSeatReservationRequestDto
{
    [Required]
    [MinLength(1)]
    public List<int> EventSeatIds { get; set; } = new();
}