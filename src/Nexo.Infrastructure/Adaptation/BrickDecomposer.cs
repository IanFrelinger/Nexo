using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Decomposes bricks into editable BrickManifest.
/// </summary>
public sealed class BrickDecomposer : IBrickDecomposer
{
    /// <inheritdoc />
    public Task<BrickManifest> DecomposeAsync(Brick brick, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var manifest = new BrickManifest
        {
            Id = brick.Id,
            Name = brick.Name,
            Version = brick.Version,
            Description = brick.Description,
            Category = brick.Category,
            Interface = brick.Interface,
            ImplementationTypeName = brick.GetType().AssemblyQualifiedName,
        };

        return Task.FromResult(manifest);
    }
}
