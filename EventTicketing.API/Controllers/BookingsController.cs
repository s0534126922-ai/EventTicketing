
using System.Security.Claims;
using EventTicketing.Core.DTOs.Bookings;
using EventTicketing.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize(Roles = "User")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(
        IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(BookingConfirmationResponseDto),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<BookingConfirmationResponseDto>>
        Create(
            [FromBody] CreateBookingRequestDto request,
            CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var confirmation = await _bookingService.CreateAsync(
            userId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            confirmation);
    }

    [HttpGet("my")]
    [ProducesResponseType(
        typeof(UserBookingsResponseDto),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<UserBookingsResponseDto>>
        GetMyBookings(
            CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var bookings = await _bookingService.GetByUserIdAsync(
            userId,
            cancellationToken);

        return Ok(bookings);
    }

    [HttpPost("{bookingId:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _bookingService.CancelAsync(
            bookingId,
            userId,
            cancellationToken);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId) ||
            userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "A valid authenticated user is required.");
        }

        return userId;
    }
}
