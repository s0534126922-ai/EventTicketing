
using EventTicketing.Core.DTOs.Halls;
using EventTicketing.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/halls")]
[Authorize(Roles = "Admin")]
public class HallsController : ControllerBase
{
    private readonly IHallService _hallService;

    public HallsController(IHallService hallService)
    {
        _hallService = hallService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<HallResponseDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HallResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var halls = await _hallService.GetAllAsync(cancellationToken);

        return Ok(halls);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(HallResponseDto),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<HallResponseDto>> GetById(
        int id,
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
    public async Task<IActionResult> Update(
        int id,
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
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await _hallService.DeleteAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}
