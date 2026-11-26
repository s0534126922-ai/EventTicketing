using EventTicketing.Core.DTOs.Halls;
using EventTicketing.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/halls")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class HallsController : ControllerBase
{
    private readonly IHallService _hallService;

    public HallsController(IHallService hallService)
    {
        _hallService = hallService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(IReadOnlyList<HallResponseDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HallResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var halls = await _hallService.GetAllAsync(
            cancellationToken);

        return Ok(halls);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(HallResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HallResponseDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var hall = await _hallService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(hall);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(HallResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HallResponseDto>> Create(
        [FromBody] CreateHallRequestDto request,
        CancellationToken cancellationToken)
    {
        var hall = await _hallService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = hall.Id },
            hall);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateHallRequestDto request,
        CancellationToken cancellationToken)
    {
        await _hallService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await _hallService.DeleteAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}