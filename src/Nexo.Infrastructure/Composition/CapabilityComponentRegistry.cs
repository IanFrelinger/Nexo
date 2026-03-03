using System.Collections.Concurrent;
using Nexo.Core.Application.Composition.Models;
using Nexo.Core.Application.Composition.Ports;

namespace Nexo.Infrastructure.Composition;

/// <summary>
/// In-memory registry of capability components. Seed from existing bricks.
/// </summary>
public sealed class CapabilityComponentRegistry : ICapabilityComponentRegistry
{
    private readonly ConcurrentDictionary<string, ComponentDescriptor> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<ComponentDescriptor>> _byCapability = new(StringComparer.OrdinalIgnoreCase);

    public CapabilityComponentRegistry()
    {
        SeedFromCodeAnalysis();
        SeedTestRunnerComponents();
    }

    /// <inheritdoc />
    public void Register(ComponentDescriptor descriptor)
    {
        _byId[descriptor.Id] = descriptor;
        var list = _byCapability.GetOrAdd(descriptor.Capability, _ => new List<ComponentDescriptor>());
        lock (list)
        {
            if (!list.Any(x => string.Equals(x.Id, descriptor.Id, StringComparison.OrdinalIgnoreCase)))
                list.Add(descriptor);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ComponentDescriptor> GetByCapability(string capability)
    {
        return _byCapability.TryGetValue(capability, out var list)
            ? list.ToList()
            : Array.Empty<ComponentDescriptor>();
    }

    /// <inheritdoc />
    public ComponentDescriptor? GetById(string id)
    {
        return _byId.TryGetValue(id, out var d) ? d : null;
    }

    private void SeedFromCodeAnalysis()
    {
        Register(new ComponentDescriptor
        {
            Id = "code-analysis",
            Capability = "code-analysis",
            ImplementationType = "Nexo.Infrastructure.Analysis.BrickAnalyzer.RoslynBrickStaticAnalyzer, Nexo.Infrastructure",
            Version = "1.0.0",
        });
    }

    /// <summary>
    /// Phase D: Test-runner capability components for composition-driven testing.
    /// Maps to IParameterMatrixGenerator, IInstanceSpawner, IResultCollector.
    /// </summary>
    private void SeedTestRunnerComponents()
    {
        Register(new ComponentDescriptor
        {
            Id = "test-discovery",
            Capability = "test-discovery",
            ImplementationType = "Nexo.Core.Application.ParallelTesting.Ports.IParameterMatrixGenerator",
            Version = "1.0.0",
        });
        Register(new ComponentDescriptor
        {
            Id = "test-execution",
            Capability = "test-execution",
            ImplementationType = "Nexo.Core.Application.ParallelTesting.Ports.IInstanceSpawner",
            Version = "1.0.0",
        });
        Register(new ComponentDescriptor
        {
            Id = "result-aggregation",
            Capability = "result-aggregation",
            ImplementationType = "Nexo.Core.Application.ParallelTesting.Ports.IResultCollector",
            Version = "1.0.0",
        });
    }
}
