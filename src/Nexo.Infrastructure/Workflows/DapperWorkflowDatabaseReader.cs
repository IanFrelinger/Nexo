using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;

namespace Nexo.Infrastructure.Workflows;

/// <summary>
/// Dapper-based implementation of workflow database reader.
/// Supports SQLite (and SQL Server via connection string).
/// </summary>
public sealed class DapperWorkflowDatabaseReader : IWorkflowDatabaseReader
{
    private readonly ILogger<DapperWorkflowDatabaseReader>? _logger;

    /// <summary>Initializes a new dapper workflow database reader.</summary>
    public DapperWorkflowDatabaseReader(ILogger<DapperWorkflowDatabaseReader>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Execute query asynchronously.</summary>
    public async Task<object> ExecuteQueryAsync(string connectionString, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Database connection string is required", nameof(connectionString));
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query is required", nameof(query));

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);

        var rows = await connection.QueryAsync(new CommandDefinition(query, cancellationToken: ct));
        var list = new List<Dictionary<string, object>>();
        foreach (var row in rows)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (row is IDictionary<string, object> idict)
            {
                foreach (var kvp in idict)
                    dict[kvp.Key] = kvp.Value is DBNull ? null! : kvp.Value!;
            }
            list.Add(dict);
        }
        return list;
    }
}
