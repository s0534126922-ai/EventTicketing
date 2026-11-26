using EventTicketing.Core.DTOs.Bookings;
using EventTicketing.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/bookings")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class EventBookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public EventBookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<AdminBookingResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AdminBookingResponseDto>>> GetAll(
        [FromRoute] int eventId,
        CancellationToken cancellationToken)
    {
        var bookings = await _bookingService.GetEventBookingsAsync(
            eventId,
            cancellationToken);

        return Ok(bookings);
    }
}
