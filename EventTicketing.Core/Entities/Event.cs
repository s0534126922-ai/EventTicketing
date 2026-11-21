using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.Enums;

namespace EventTicketing.Core.Entities;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int HallId { get; set; }
    public Hall Hall { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Active;

    public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
}
