using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;

namespace Nexo.Infrastructure.Export;

/// <summary>
/// A single variation of generated content.
/// </summary>
public class GeneratedVariation
{
    /// <summary>Generated content text.</summary>
    public string Content { get; init; } = default!;
    /// <summary>Optional metadata associated with the generated content.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
