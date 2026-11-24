using EventTicketing.Core.DTOs.EventSeats;
using EventTicketing.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/seats")]
public class EventSeatsController : ControllerBase
{
    private readonly IEventSeatService _eventSeatService;

    public EventSeatsController(
        IEventSeatService eventSeatService)
    {
        _eventSeatService = eventSeatService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(IReadOnlyList<EventSeatResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EventSeatResponseDto>>>
        GetByEventId(
            [FromRoute] int eventId,
            CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");

        var seats = await _eventSeatService.GetByEventIdAsync(
            eventId,
            isAdmin,
            cancellationToken);

        return Ok(seats);
    }

    [HttpPost("reservations")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reserve(
        [FromRoute] int eventId,
        [FromBody] AdminSeatReservationRequestDto request,
        CancellationToken cancellationToken)
    {
        await _eventSeatService.ReserveAsync(
            eventId,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("reservations")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Release(
        [FromRoute] int eventId,
        [FromBody] AdminSeatReservationRequestDto request,
        CancellationToken cancellationToken)
    {
        await _eventSeatService.ReleaseAsync(
            eventId,
            request,
            cancellationToken);

        return NoContent();
    }
}