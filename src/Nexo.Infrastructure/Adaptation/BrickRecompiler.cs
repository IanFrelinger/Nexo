using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure.Observation;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Recompiles BrickManifest into deployable bricks. For known types, creates instances with config.
/// </summary>
public sealed class BrickRecompiler : IBrickRecompiler
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BrickRecompiler>? _logger;

    /// <summary>Initializes a new brick recompiler.</summary>
    public BrickRecompiler(IServiceProvider services, ILogger<BrickRecompiler>? logger = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<DomainBrick?> RecompileAsync(BrickManifest manifest, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (manifest.ImplementationTypeName != null)
        {
            var type = Type.GetType(manifest.ImplementationTypeName);
            if (type != null)
            {
                try
                {
                    var brick = CreateFromType(type, manifest);
                    if (brick != null)
                        return Task.FromResult<DomainBrick?>(brick);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to recompile brick {Id} from type {Type}", manifest.Id, manifest.ImplementationTypeName);
                }
            }
        }

        return Task.FromResult<DomainBrick?>(null);
    }

    private DomainBrick? CreateFromType(Type type, BrickManifest manifest)
    {
        if (type == typeof(ObservationContextBrick))
        {
            var assembler = _services.GetService<IContextAssembler>();
            if (assembler == null) return null;
            return new ObservationContextBrick(assembler);
        }

        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);
        if (parameterlessCtor != null)
        {
            return (DomainBrick?)Activator.CreateInstance(type);
        }

        try
        {
            return (DomainBrick?)ActivatorUtilities.CreateInstance(_services, type);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
