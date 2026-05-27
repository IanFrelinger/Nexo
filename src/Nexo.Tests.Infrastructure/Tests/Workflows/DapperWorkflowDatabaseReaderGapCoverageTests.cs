using FluentAssertions;
using Microsoft.Data.Sqlite;
using Nexo.Infrastructure.Workflows;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Workflows;

public sealed class DapperWorkflowDatabaseReaderGapCoverageTests
{
    [Theory]
    [InlineData(null, "SELECT 1", "connectionString")]
    [InlineData("", "SELECT 1", "connectionString")]
    [InlineData("Data Source=:memory:", null, "query")]
    [InlineData("Data Source=:memory:", "   ", "query")]
    public async Task ExecuteQueryAsync_rejects_blank_inputs(string? connectionString, string? query, string paramName)
    {
        var reader = new DapperWorkflowDatabaseReader();

        var act = () => reader.ExecuteQueryAsync(connectionString!, query!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName(paramName);
    }

    [Fact]
    public async Task ExecuteQueryAsync_returns_rows_with_nullable_columns()
    {
        var dbPath = CreateTempDatabasePath();
        var connectionString = $"Data Source={dbPath}";
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE sample (Name TEXT, Amount INTEGER); INSERT INTO sample VALUES ('alpha', 1), (NULL, 2);";
            await command.ExecuteNonQueryAsync();
        }

        var reader = new DapperWorkflowDatabaseReader();
        var result = await reader.ExecuteQueryAsync(connectionString, "SELECT Name, Amount FROM sample ORDER BY Amount");

        var rows = result.Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        rows.Should().HaveCount(2);
        rows[0]["Name"].Should().Be("alpha");
        rows[1]["Name"].Should().BeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_returns_empty_list_for_no_rows()
    {
        var dbPath = CreateTempDatabasePath();
        var connectionString = $"Data Source={dbPath}";
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE empty_table (Id INTEGER);";
            await command.ExecuteNonQueryAsync();
        }

        var reader = new DapperWorkflowDatabaseReader();
        var result = await reader.ExecuteQueryAsync(connectionString, "SELECT Id FROM empty_table");

        var rows = result.Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        rows.Should().BeEmpty();
    }

    private static string CreateTempDatabasePath()
        => Path.Combine(Path.GetTempPath(), "nexo-dapper-read-" + Guid.NewGuid().ToString("N") + ".db");
}
