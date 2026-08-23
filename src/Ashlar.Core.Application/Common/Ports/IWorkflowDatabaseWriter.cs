namespace Ashlar.Core.Application.Common.Ports;

/// <summary>
/// Abstraction for writing data to a database in workflow output nodes.
/// Implementations live in Infrastructure.
/// </summary>
public interface IWorkflowDatabaseWriter
{
    Task WriteAsync(string connectionString, string tableName, object data, CancellationToken ct = default);
}
