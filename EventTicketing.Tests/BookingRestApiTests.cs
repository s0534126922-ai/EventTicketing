using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using EventTicketing.Core.DTOs.Bookings;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Exceptions;
using EventTicketing.Core.Interfaces.Services;
using EventTicketing.Tests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace EventTicketing.Tests;

public sealed class BookingRestApiTests
{
    private const int UserId = 1;
    private const int OtherUserId = 2;
    private const int BookingId = 42;
    private const int EventSeatId = 10;

    [Fact]
    public async Task CreateBooking_Returns201WithLocation()
    {
        var service = new Mock<IBookingService>();

        var booking = CreateBookingResponse();

        service.Setup(x => x.CreateAsync(
                UserId,
                It.Is<CreateBookingRequestDto>(
                    request => request.EventSeatId == EventSeatId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookingConfirmationResponseDto
            {
                Booking = booking,
                Message = "Booking confirmed successfully."
            });

        service.Setup(x => x.GetByIdAsync(
                BookingId,
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        using var factory = new HttpApiFactory();

        using var configuredFactory = factory.WithWebHostBuilder(
            builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBookingService>();
                services.AddSingleton<IBookingService>(service.Object);
            }));

        using var client = configuredFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        Authorize(client, UserId);

        var response = await client.PostAsJsonAsync(
            "/api/bookings",
            new CreateBookingRequestDto
            {
                EventSeatId = EventSeatId
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.NotNull(response.Headers.Location);

        var location = new Uri(
            client.BaseAddress!,
            response.Headers.Location!);

        Assert.Equal(
            $"/api/bookings/{BookingId}",
            location.AbsolutePath);

        var confirmation = await response.Content
            .ReadFromJsonAsync<BookingConfirmationResponseDto>();

        Assert.NotNull(confirmation);
        Assert.Equal(BookingId, confirmation.Booking.Id);

        var getResponse = await client.GetAsync(location);

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var returnedBooking = await getResponse.Content
            .ReadFromJsonAsync<BookingResponseDto>();

        Assert.NotNull(returnedBooking);
        Assert.Equal(BookingId, returnedBooking.Id);

        service.Verify(x => x.CreateAsync(
                UserId,
                It.IsAny<CreateBookingRequestDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        service.Verify(x => x.GetByIdAsync(
                BookingId,
                UserId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBooking_WithOwner_Returns200()
    {
        var service = new Mock<IBookingService>();

        service.Setup(x => x.GetByIdAsync(
                BookingId,
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateBookingResponse());

        using var factory = new HttpApiFactory();
        using var client = CreateClient(factory, service);

        Authorize(client, UserId);

        var response = await client.GetAsync(
            $"/api/bookings/{BookingId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var booking = await response.Content
            .ReadFromJsonAsync<BookingResponseDto>();

        Assert.NotNull(booking);
        Assert.Equal(BookingId, booking.Id);

        service.Verify(x => x.GetByIdAsync(
                BookingId,
                UserId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBooking_WithDifferentUser_Returns404()
    {
        var service = new Mock<IBookingService>();

        service.Setup(x => x.GetByIdAsync(
                BookingId,
                OtherUserId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(
                "Booking not found."));

        using var factory = new HttpApiFactory();
        using var client = CreateClient(factory, service);

        Authorize(client, OtherUserId);

        var response = await client.GetAsync(
            $"/api/bookings/{BookingId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        service.Verify(x => x.GetByIdAsync(
                BookingId,
                OtherUserId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteBooking_Returns204()
    {
        var service = new Mock<IBookingService>();

        service.Setup(x => x.CancelAsync(
                BookingId,
                UserId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var factory = new HttpApiFactory();
        using var client = CreateClient(factory, service);

        Authorize(client, UserId);

        var response = await client.DeleteAsync(
            $"/api/bookings/{BookingId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Empty(
            await response.Content.ReadAsStringAsync());

        service.Verify(x => x.CancelAsync(
                BookingId,
                UserId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static HttpClient CreateClient(
        HttpApiFactory factory,
        Mock<IBookingService> service)
    {
        return factory.WithWebHostBuilder(
                builder => builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IBookingService>();
                    services.AddSingleton<IBookingService>(
                        service.Object);
                }))
            .CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing
                    .WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });
    }

    private static BookingResponseDto CreateBookingResponse()
    {
        var startTime = DateTime.UtcNow.AddDays(3);

        return new BookingResponseDto
        {
            Id = BookingId,
            EventSeatId = EventSeatId,
            SeatNumber = 1,
            Status = BookingStatus.Confirmed,
            BookedAt = DateTime.UtcNow,
            EventStartTimeAtBooking = startTime,
            EventEndTimeAtBooking = startTime.AddHours(2)
        };
    }

    private static void Authorize(
        HttpClient client,
        int userId)
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
                userId.ToString()),

            new Claim(
                ClaimTypes.Name,
                "Test User"),

            new Claim(
                ClaimTypes.Role,
                "User")
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