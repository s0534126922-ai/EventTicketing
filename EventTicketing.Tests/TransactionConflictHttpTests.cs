using System.Net;
using EventTicketing.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventTicketing.Tests;

public sealed class TransactionConflictHttpTests
{
    [Theory]
    [InlineData("40001", false)]
    [InlineData("40001", true)]
    [InlineData("40P01", false)]
    [InlineData("40P01", true)]
    public async Task TransactionConflict_Returns409(
        string sqlState,
        bool wrapped)
    {
        using var factory = new HttpApiFactory();

        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync(
            $"/api/transaction-conflict-tests/{sqlState}/{wrapped}");

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        Assert.Contains(
            "Transaction Conflict",
            content);

        Assert.Contains(
            "Please try again",
            content);

        Assert.DoesNotContain(
            "Internal database diagnostic information",
            content);
    }
}

[ApiController]
[AllowAnonymous]
[Route("api/transaction-conflict-tests")]
public sealed class TransactionConflictTestController
    : ControllerBase
{
    [HttpGet("{sqlState}/{wrapped:bool}")]
    public IActionResult SimulateConflict(
        string sqlState,
        bool wrapped)
    {
        var exception = new PostgresException(
            "Internal database diagnostic information",
            "ERROR",
            "ERROR",
            sqlState);

        if (wrapped)
        {
            throw new InvalidOperationException(
                "Transient database failure.",
                new DbUpdateException(
                    "Database update failed.",
                    exception));
        }

        throw exception;
    }
}