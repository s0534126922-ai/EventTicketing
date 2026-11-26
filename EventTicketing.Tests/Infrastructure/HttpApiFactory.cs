using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Infrastructure;

public sealed class HttpApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKey =
        "EventTicketingHttpTestsOnlySecretKey2026!";

    public const string JwtIssuer = "EventTicketing.HttpTests";
    public const string JwtAudience = "EventTicketing.HttpTests.Client";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("HttpTests");

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=127.0.0.1;Port=1;Database=eventticketing_http_tests;Username=unused;Password=unused");

        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);

        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=127.0.0.1;Port=1;Database=eventticketing_http_tests;Username=unused;Password=unused",

                    ["Jwt:Key"] = JwtKey,
                    ["Jwt:Issuer"] = JwtIssuer,
                    ["Jwt:Audience"] = JwtAudience,

                    ["SeedAdmin:Name"] = null,
                    ["Database:ApplyMigrationsOnStartup"] = "false",
                    ["SeedAdmin:Email"] = null,
                    ["SeedAdmin:Password"] = null
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services
                .AddControllers()
                .AddApplicationPart(typeof(HttpTestController).Assembly);
        });
    }

    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}