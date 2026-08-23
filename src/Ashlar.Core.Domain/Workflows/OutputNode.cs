using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Clusters;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Workflows;

/// <summary>
/// Output node for receiving workflow results.
/// </summary>
public record OutputNode : WorkflowNode
{
    /// <summary>How workflow output is delivered.</summary>
    public OutputType Type { get; init; } = OutputType.Display;

    /// <summary>Serialization format for emitted output.</summary>
    public WorkflowOutputFormat Format { get; init; } = WorkflowOutputFormat.Json;

    /// <summary>Filesystem destination when <see cref="Type"/> is <see cref="OutputType.File"/>.</summary>
    public string? FilePath { get; init; }

    /// <summary>Webhook URL when <see cref="Type"/> is <see cref="OutputType.Webhook"/>.</summary>
    public string? WebhookUrl { get; init; }

    /// <summary>Database connection string when <see cref="Type"/> is <see cref="OutputType.Database"/>.</summary>
    public string? DatabaseConnectionString { get; init; }

    /// <summary>Target table name for database output nodes.</summary>
    public string? TableName { get; init; }
}
