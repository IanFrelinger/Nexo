using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Export;
using Ashlar.Core.Domain.Workflows;

namespace Ashlar.Infrastructure.Export;

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
