using System;
using System.Collections.Generic;
using System.Text;

namespace EventTicketing.Core.Entities;

public class Seat
{
    public int Id { get; set; }
    public int HallId { get; set; }
    public int SeatNumber { get; set; }

    public Hall Hall { get; set; } = null!;
    public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
}
