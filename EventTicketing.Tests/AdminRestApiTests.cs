using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using EventTicketing.Core.DTOs.EventSeats;
using EventTicketing.Core.Interfaces.Services;
using EventTicketing.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace EventTicketing.Tests;

public sealed class AdminRestApiTests
{
    private const int EventId = 42;

    private static readonly int[] SeatIds = { 10, 11 };

    [Theory]
    [InlineData("DELETE", "/api/events/42", "cancel")]
    [InlineData("PATCH", "/api/events/42/status", "restore")]
    [InlineData("POST", "/api/events/42/seats/reservations", "reserve")]
    [InlineData("PATCH", "/api/events/42/seats/reservations", "release")]
    public async Task AdminOperation_WithAdminRole_Returns204(
        string method,
        string path,
        string operation)
    {
        var eventService = new Mock<IEventService>(
            MockBehavior.Strict);

        var seatService = new Mock<IEventSeatService>(
            MockBehavior.Strict);

        switch (operation)
        {
            case "cancel":
                eventService.Setup(x => x.CancelAsync(
                        EventId,
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                break;

            case "restore":
                eventService.Setup(x => x.RestoreAsync(
                        EventId,
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                break;

            case "reserve":
                seatService.Setup(x => x.ReserveAsync(
                        EventId,
                        It.Is<AdminSeatReservationRequestDto>(
                            request => request.EventSeatIds
                                .SequenceEqual(SeatIds)),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                break;

            case "release":
                seatService.Setup(x => x.ReleaseAsync(
                        EventId,
                        It.Is<AdminSeatReservationRequestDto>(
                            request => request.EventSeatIds
                                .SequenceEqual(SeatIds)),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                break;

            default:
                throw new ArgumentException(
                    "Unknown admin operation.");
        }

        using var factory = new HttpApiFactory();

        using var configuredFactory = ConfigureFactory(
            factory,
            eventService,
            seatService);

        using var client = CreateClient(configuredFactory);

        Authorize(client, "Admin");

        using var request = CreateRequest(
            method,
            path,
            operation);

        using var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Empty(
            await response.Content.ReadAsStringAsync());

        switch (operation)
        {
            case "cancel":
                eventService.Verify(x => x.CancelAsync(
                        EventId,
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                break;

            case "restore":
                eventService.Verify(x => x.RestoreAsync(
                        EventId,
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                break;

            case "reserve":
                seatService.Verify(x => x.ReserveAsync(
                        EventId,
                        It.Is<AdminSeatReservationRequestDto>(
                            dto => dto.EventSeatIds
                                .SequenceEqual(SeatIds)),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                break;

            case "release":
                seatService.Verify(x => x.ReleaseAsync(
                        EventId,
                        It.Is<AdminSeatReservationRequestDto>(
                            dto => dto.EventSeatIds
                                .SequenceEqual(SeatIds)),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                break;
        }

        eventService.VerifyNoOtherCalls();
        seatService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("DELETE", "/api/events/42", "cancel")]
    [InlineData("PATCH", "/api/events/42/status", "restore")]
    [InlineData("POST", "/api/events/42/seats/reservations", "reserve")]
    [InlineData("PATCH", "/api/events/42/seats/reservations", "release")]
    public async Task AdminOperation_WithUserRole_Returns403(
        string method,
        string path,
        string operation)
    {
        var eventService = new Mock<IEventService>(
            MockBehavior.Strict);

        var seatService = new Mock<IEventSeatService>(
            MockBehavior.Strict);

        using var factory = new HttpApiFactory();

        using var configuredFactory = ConfigureFactory(
            factory,
            eventService,
            seatService);

        using var client = CreateClient(configuredFactory);

        Authorize(client, "User");

        using var request = CreateRequest(
            method,
            path,
            operation);

        using var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        eventService.VerifyNoOtherCalls();
        seatService.VerifyNoOtherCalls();
    }

    private static WebApplicationFactory<Program> ConfigureFactory(
        HttpApiFactory factory,
        Mock<IEventService> eventService,
        Mock<IEventSeatService> seatService)
    {
        return factory.WithWebHostBuilder(
            builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEventService>();
                services.RemoveAll<IEventSeatService>();

                services.AddSingleton<IEventService>(
                    eventService.Object);

                services.AddSingleton<IEventSeatService>(
                    seatService.Object);
            }));
    }

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    private static HttpRequestMessage CreateRequest(
        string method,
        string path,
        string operation)
    {
        var request = new HttpRequestMessage(
            new HttpMethod(method),
            path);

        if (operation is "reserve" or "release")
        {
            request.Content = JsonContent.Create(
                new AdminSeatReservationRequestDto
                {
                    EventSeatIds = SeatIds.ToList()
                });
        }

        return request;
    }

    private static void Authorize(
        HttpClient client,
        string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(HttpApiFactory.JwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                "1"),

            new Claim(
                ClaimTypes.Name,
                "Test User"),

            new Claim(
                ClaimTypes.Role,
                role)
        };

        var token = new JwtSecurityToken(
            issuer: HttpApiFactory.JwtIssuer,
            audience: HttpApiFactory.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                new JwtSecurityTokenHandler().WriteToken(token));
    }
}