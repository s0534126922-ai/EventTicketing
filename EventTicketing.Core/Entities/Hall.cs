using System;
using System.Collections.Generic;
using System.Text;

namespace EventTicketing.Core.Entities;

public class Hall
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public bool HasEverHostedEvent { get; set; } = false;

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}