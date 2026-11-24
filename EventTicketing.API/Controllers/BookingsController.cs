using System.Security.Claims;
using EventTicketing.Core.DTOs.Bookings;
using EventTicketing.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize(Roles = "User")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        return CreatedAtAction(
            nameof(GetById),
            new { bookingId = confirmation.Booking.Id },
            confirmation);
    }

    [HttpGet("{bookingId:int}")]
    [ProducesResponseType(
        typeof(BookingResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponseDto>> GetById(
        [FromRoute] int bookingId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var booking = await _bookingService.GetByIdAsync(
            bookingId,
            userId,
            cancellationToken);

        return Ok(booking);
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

    [HttpDelete("{bookingId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [FromRoute] int bookingId,
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