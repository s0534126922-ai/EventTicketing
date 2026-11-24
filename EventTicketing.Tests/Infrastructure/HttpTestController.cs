
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Tests.Infrastructure;

[ApiController]
[Route("api/http-tests")]
public sealed class HttpTestController : ControllerBase
{
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok();
    }

    [HttpGet("concurrency")]
    [AllowAnonymous]
    public IActionResult Concurrency()
    {
        throw new DbUpdateConcurrencyException(
            "Simulated database concurrency conflict.");
    }

    [HttpGet("unexpected-error")]
    [AllowAnonymous]
    public IActionResult UnexpectedError()
    {
        throw new InvalidOperationException(
            "Sensitive internal diagnostic information.");
    }
}
