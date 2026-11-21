using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Core.DTOs.Halls;

public class CreateHallRequestDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int SeatCount { get; set; }
}