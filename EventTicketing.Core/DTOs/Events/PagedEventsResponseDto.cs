using System;
using System.Collections.Generic;
using System.Text;

namespace EventTicketing.Core.DTOs.Events;

public class PagedEventsResponseDto
{
    public IReadOnlyList<EventResponseDto> Items { get; set; }
        = Array.Empty<EventResponseDto>();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalPages =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;
}