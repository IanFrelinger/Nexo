namespace Ashlar.Core.Application.Common.Ports;

/// <summary>
/// Abstraction for reading data from a database in workflow input nodes.
/// Implementations live in Infrastructure.
/// </summary>
public interface IWorkflowDatabaseReader
{
    Task<object> ExecuteQueryAsync(string connectionString, string query, CancellationToken ct = default);
}
