using System;
using System.Collections.Generic;
using System.Text;

namespace EventTicketing.Core.DTOs.Halls;

public class HallResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SeatCount { get; set; }

    public bool HasEverHostedEvent { get; set; }
}