using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Workflows;

/// <summary>
/// Input node for providing data to the workflow.
/// </summary>
public record InputNode : WorkflowNode
{
    /// <summary>How workflow input is sourced.</summary>
    public InputType Type { get; init; } = InputType.File;

    /// <summary>Filesystem path when <see cref="Type"/> is <see cref="InputType.File"/>.</summary>
    public string? FilePath { get; init; }

    /// <summary>Inline content when <see cref="Type"/> is <see cref="InputType.Content"/>.</summary>
    public string? Content { get; init; }

    /// <summary>Webhook URL when <see cref="Type"/> is <see cref="InputType.Webhook"/>.</summary>
    public string? WebhookUrl { get; init; }

    /// <summary>Database connection string when <see cref="Type"/> is <see cref="InputType.Database"/>.</summary>
    public string? DatabaseConnectionString { get; init; }

    /// <summary>SQL or query text for database input nodes.</summary>
    public string? Query { get; init; }
}
