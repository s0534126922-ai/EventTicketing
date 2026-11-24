
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using EventTicketing.Tests.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace EventTicketing.Tests;

public sealed class HttpApiIntegrationTests
{
    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        using var factory = new HttpApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/bookings/my");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithUserRole_Returns403()
    {
        using var factory = new HttpApiFactory();
        using var client = factory.CreateHttpsClient();

        var token = CreateUserToken();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(
            "/api/http-tests/admin-only");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task ConcurrencyException_Returns409()
    {
        using var factory = new HttpApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            "/api/http-tests/concurrency");

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Contains(
            "application/problem+json",
            response.Content.Headers.ContentType?.ToString());

        Assert.Contains(
            "modified by another operation",
            content);
    }

    [Fact]
    public async Task UnexpectedException_Returns500_WithoutInternalDetails()
    {
        using var factory = new HttpApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            "/api/http-tests/unexpected-error");

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        Assert.Contains(
            "An unexpected error occurred",
            content);

        Assert.DoesNotContain(
            "Sensitive internal diagnostic information",
            content);
    }

    private static string CreateUserToken()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(HttpApiFactory.JwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "User")
        };

        var token = new JwtSecurityToken(
            issuer: HttpApiFactory.JwtIssuer,
            audience: HttpApiFactory.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
