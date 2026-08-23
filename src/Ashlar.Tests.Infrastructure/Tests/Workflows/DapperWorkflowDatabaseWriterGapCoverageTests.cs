using FluentAssertions;
using Microsoft.Data.Sqlite;
using Ashlar.Infrastructure.Workflows;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Workflows;

/// <summary>Tests for dapper workflow database writer gap coverage.</summary>
public sealed class DapperWorkflowDatabaseWriterGapCoverageTests
{
    [Theory]
    [InlineData(null, "table", "connectionString")]
    [InlineData("", "table", "connectionString")]
    [InlineData("Data Source=:memory:", null, "tableName")]
    [InlineData("Data Source=:memory:", "  ", "tableName")]
    public async Task WriteAsync_rejects_blank_inputs(string? connectionString, string? tableName, string paramName)
    {
        var writer = new DapperWorkflowDatabaseWriter();

        var act = () => writer.WriteAsync(connectionString!, tableName!, new { ok = true });

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName(paramName);
    }

    [Fact]
    public async Task WriteAsync_inserts_single_dictionary_row()
    {
        var dbPath = CreateTempDatabasePath();
        var connectionString = $"Data Source={dbPath}";
        /// <summary>Creates events table async.</summary>
        await CreateEventsTableAsync(connectionString);

        var writer = new DapperWorkflowDatabaseWriter();
        await writer.WriteAsync(
            connectionString,
            "events",
            new Dictionary<string, object>
            {
                ["Name"] = "created",
                ["Count"] = 1,
            });

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, Count FROM events";
        await using var reader = await command.ExecuteReaderAsync();
        reader.Read().Should().BeTrue();
        reader.GetString(0).Should().Be("created");
        reader.GetInt32(1).Should().Be(1);
    }

    [Fact]
    public async Task WriteAsync_inserts_multiple_dictionary_rows()
    {
        var dbPath = CreateTempDatabasePath();
        var connectionString = $"Data Source={dbPath}";
        /// <summary>Creates events table async.</summary>
        await CreateEventsTableAsync(connectionString);

        var writer = new DapperWorkflowDatabaseWriter();
        await writer.WriteAsync(
            connectionString,
            "events",
            new List<Dictionary<string, object>>
            {
                new() { ["Name"] = "first", ["Count"] = 1 },
                new() { ["Name"] = "second", ["Count"] = 2 },
            });

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM events";
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        count.Should().Be(2);
    }

    [Fact]
    public async Task WriteAsync_wraps_scalar_data_in_value_column()
    {
        var dbPath = CreateTempDatabasePath();
        var connectionString = $"Data Source={dbPath}";
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE payloads (Value TEXT);";
            await command.ExecuteNonQueryAsync();
        }

        var writer = new DapperWorkflowDatabaseWriter();
        await writer.WriteAsync(connectionString, "payloads", "raw-payload");

        await using var verify = new SqliteConnection(connectionString);
        await verify.OpenAsync();
        await using var read = verify.CreateCommand();
        read.CommandText = "SELECT Value FROM payloads";
        (await read.ExecuteScalarAsync()).Should().Be("raw-payload");
    }

    private static async Task CreateEventsTableAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE events (Name TEXT, Count INTEGER);";
        await command.ExecuteNonQueryAsync();
    }

    private static string CreateTempDatabasePath()
        => Path.Combine(Path.GetTempPath(), "ashlar-dapper-write-" + Guid.NewGuid().ToString("N") + ".db");
}
