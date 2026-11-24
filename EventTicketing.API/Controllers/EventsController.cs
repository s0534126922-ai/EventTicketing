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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponseDto>> GetById(
        [FromRoute] int id,
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateEventRequestDto request,
        CancellationToken cancellationToken)
    {
        await _eventService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await _eventService.CancelAsync(
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await _eventService.RestoreAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}