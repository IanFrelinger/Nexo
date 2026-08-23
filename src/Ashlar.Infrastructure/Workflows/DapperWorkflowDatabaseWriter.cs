using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Common.Ports;

namespace Ashlar.Infrastructure.Workflows;

/// <summary>
/// Dapper-based implementation of workflow database writer.
/// Inserts data as JSON into a single column, or maps to columns when data is a dictionary.
/// </summary>
public sealed class DapperWorkflowDatabaseWriter : IWorkflowDatabaseWriter
{
    private readonly ILogger<DapperWorkflowDatabaseWriter>? _logger;

    /// <summary>Initializes a new dapper workflow database writer.</summary>
    public DapperWorkflowDatabaseWriter(ILogger<DapperWorkflowDatabaseWriter>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Write asynchronously.</summary>
    public async Task WriteAsync(string connectionString, string tableName, object data, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Database connection string is required", nameof(connectionString));
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name is required", nameof(tableName));

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);

        if (data is IEnumerable<Dictionary<string, object>> rows)
        {
            foreach (var row in rows)
            {
                await InsertRowAsync(connection, tableName, row, ct);
            }
        }
        else if (data is Dictionary<string, object> singleRow)
        {
            await InsertRowAsync(connection, tableName, singleRow, ct);
        }
        else
        {
            var row = new Dictionary<string, object> { ["Value"] = data };
            await InsertRowAsync(connection, tableName, row, ct);
        }
    }

    private static async Task InsertRowAsync(
        SqliteConnection connection,
        string tableName,
        Dictionary<string, object> row,
        CancellationToken ct)
    {
        var columns = string.Join(", ", row.Keys.Select(k => "[" + k + "]"));
        var paramsList = string.Join(", ", row.Keys.Select((k, i) => "@p" + i));
        var sql = $"INSERT INTO [{tableName}] ({columns}) VALUES ({paramsList})";
        var parameters = new DynamicParameters();
        var i = 0;
        foreach (var kvp in row)
        {
            parameters.Add("p" + i, kvp.Value);
            i++;
        }
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }
}
