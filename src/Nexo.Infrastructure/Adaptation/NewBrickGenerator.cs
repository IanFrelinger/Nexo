using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Generates new brick manifests from observed patterns. Stub implementation.
/// </summary>
public sealed class NewBrickGenerator : INewBrickGenerator
{
    /// <inheritdoc />
    public Task<BrickManifest> GenerateAsync(
        string patternType,
        IReadOnlyDictionary<string, object>? patternMetadata = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var manifest = new BrickManifest
        {
            Id = $"generated.{patternType}.{Guid.NewGuid():N}",
            Name = $"Generated from {patternType}",
            Version = "1.0.0",
            Category = BrickCategory.Control,
            Description = $"Stub brick manifest for pattern type '{patternType}'",
            Interface = new BrickInterface
            {
                Inputs = [],
                Outputs = [new BrickOutputDefinition("result", "object", "Placeholder output")],
            },
            ImplementationSource = null,
        };

        return Task.FromResult(manifest);
    }
}
