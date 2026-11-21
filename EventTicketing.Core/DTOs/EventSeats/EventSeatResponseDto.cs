using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.Enums;

namespace EventTicketing.Core.DTOs.EventSeats;

public class EventSeatResponseDto
{
    public int Id { get; set; }

    public int SeatNumber { get; set; }

    public SeatStatus Status { get; set; }
}