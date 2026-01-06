using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Export;

namespace Nexo.Infrastructure.Export;

/// <summary>
/// Generates content using AI for export.
/// </summary>
public interface IContentGenerator
{
    Task<GeneratedContent> GenerateAsync(
        Brick brick,
        GenerationConfig config,
        CancellationToken ct = default);
}

