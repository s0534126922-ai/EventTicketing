using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.DTOs.Events;

namespace EventTicketing.Core.Interfaces.Services;

public interface IEventService
{
    Task<PagedEventsResponseDto> SearchAsync(
        EventSearchRequestDto request,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<EventResponseDto> GetByIdAsync(
        int id,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<EventResponseDto> CreateAsync(
        CreateEventRequestDto request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id,
        UpdateEventRequestDto request,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task RestoreAsync(
        int id,
        CancellationToken cancellationToken = default);
}