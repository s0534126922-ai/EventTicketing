
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
    public async Task<ActionResult<IReadOnlyList<EventSeatResponseDto>>>
        GetByEventId(
            int eventId,
            CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");

        var seats = await _eventSeatService.GetByEventIdAsync(
            eventId,
            isAdmin,
            cancellationToken);

        return Ok(seats);
    }

    [HttpPost("reserve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reserve(
        int eventId,
        [FromBody] AdminSeatReservationRequestDto request,
        CancellationToken cancellationToken)
    {
        await _eventSeatService.ReserveAsync(
            eventId,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("release")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Release(
        int eventId,
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
