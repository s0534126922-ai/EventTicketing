using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;
using EventTicketing.Core.Enums;

namespace EventTicketing.Core.DTOs.Events;

public class EventSearchRequestDto
{
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? HallId { get; set; }

    public EventStatus? Status { get; set; }

    public bool Descending { get; set; } = false;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;
}