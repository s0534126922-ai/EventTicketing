
using System.Net;
using EventTicketing.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EventTicketing.Tests.Infrastructure;

public static class TestDatabaseFactory
{
    private const string ExpectedDatabase = "eventticketing_test";
    private const int ExpectedPort = 5432;

    public static NpgsqlConnection CreateConnection()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(TestDatabaseFactory).Assembly)
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Test database connection string is missing from User Secrets.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (!string.Equals(
                builder.Database,
                ExpectedDatabase,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Integration tests must use eventticketing_test.");
        }

        if (builder.Host != "localhost" &&
            builder.Host != "127.0.0.1" &&
            builder.Host != "::1")
        {
            throw new InvalidOperationException(
                "Integration tests must use local PostgreSQL.");
        }

        if (builder.Port != ExpectedPort)
        {
            throw new InvalidOperationException(
                "Integration tests must use PostgreSQL port 5432.");
        }

        return new NpgsqlConnection(connectionString);
    }

    public static EventTicketingDbContext CreateContext()
    {
        var connection = CreateConnection();

        var options = new DbContextOptionsBuilder<EventTicketingDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new EventTicketingDbContext(options);
    }

    public static EventTicketingDbContext CreateContext(
        NpgsqlConnection connection)
    {
        var options = new DbContextOptionsBuilder<EventTicketingDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new EventTicketingDbContext(options);
    }

    public static async Task VerifyDatabaseAsync(
        NpgsqlConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();

        command.CommandText = """
            select current_database(),
                   inet_server_addr()::text,
                   inet_server_port()
            """;

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "Could not verify test database identity.");
        }

        var database = reader.GetString(0);

        var serverAddress = reader.IsDBNull(1)
            ? string.Empty
            : reader.GetString(1);

        var serverPort = reader.IsDBNull(2)
            ? 0
            : reader.GetInt32(2);

        if (!string.Equals(
                database,
                ExpectedDatabase,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Connected to an unexpected database.");
        }

        var ipPart = serverAddress.Split('/')[0];

        if (!IPAddress.TryParse(ipPart, out var address) ||
            !IPAddress.IsLoopback(address) ||
            serverPort != ExpectedPort)
        {
            throw new InvalidOperationException(
                "Integration tests require a local PostgreSQL server.");
        }
    }
}
