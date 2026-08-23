using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Export;

namespace Ashlar.Infrastructure.Export;

/// <summary>
/// Generates content using AI for export.
/// </summary>
public interface IContentGenerator
{
    Task<GeneratedContent> GenerateAsync(
        DomainBrick brick,
        GenerationConfig config,
        CancellationToken ct = default);
}

