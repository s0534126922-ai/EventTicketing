
using System.Security.Claims;
using EventTicketing.Core.DTOs.Events;
using EventTicketing.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(PagedEventsResponseDto),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedEventsResponseDto>> Search(
        [FromQuery] EventSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");

        var events = await _eventService.SearchAsync(
            request,
            isAdmin,
            cancellationToken);

        return Ok(events);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(EventResponseDto),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<EventResponseDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");

        var eventDetails = await _eventService.GetByIdAsync(
            id,
            isAdmin,
            cancellationToken);

        return Ok(eventDetails);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(
        typeof(EventResponseDto),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<EventResponseDto>> Create(
        [FromBody] CreateEventRequestDto request,
        CancellationToken cancellationToken)
    {
        var createdEvent = await _eventService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdEvent.Id },
            createdEvent);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateEventRequestDto request,
        CancellationToken cancellationToken)
    {
        await _eventService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        await _eventService.CancelAsync(
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:int}/restore")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restore(
        int id,
        CancellationToken cancellationToken)
    {
        await _eventService.RestoreAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}
