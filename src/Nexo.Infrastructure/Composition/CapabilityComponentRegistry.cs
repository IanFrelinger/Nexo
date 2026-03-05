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
        SeedPlaceholderComponents();
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

    /// <summary>
    /// Placeholder descriptors for North Star component families (Perception, Action, Reasoning, Memory, Reporting).
    /// Enables composition engine to discover required capabilities. Pipeline IDs (perception, validation, reporting,
    /// understanding) map to existing or placeholder components so nexo compose does not fail.
    /// See docs/ComponentLibrary.md and docs/SeedComponentLibraryAudit.md.
    /// </summary>
    private void SeedPlaceholderComponents()
    {
        // Pipeline IDs used by CompositionEngine rules (perception, validation, reporting, understanding)
        Register(new ComponentDescriptor { Id = "perception", Capability = "perception", ImplementationType = "Nexo.Infrastructure.Observation.ObservationContextBrick, Nexo.Infrastructure", Version = "1.0.0" });
        Register(new ComponentDescriptor { Id = "validation", Capability = "validation", ImplementationType = "Nexo.Infrastructure.Analysis.BrickAnalyzer.RoslynBrickStaticAnalyzer, Nexo.Infrastructure", Version = "1.0.0" });
        Register(new ComponentDescriptor { Id = "reporting", Capability = "reporting", ImplementationType = "Nexo.Infrastructure.SelfContext.ChangelogGenerator, Nexo.Infrastructure", Version = "1.0.0" });
        Register(new ComponentDescriptor { Id = "understanding", Capability = "understanding", ImplementationType = "Nexo.Infrastructure.Observation.ObservationContextBrick, Nexo.Infrastructure", Version = "1.0.0" });

        // Perception (specialized)
        Register(new ComponentDescriptor { Id = "vision-input", Capability = "vision-input", ImplementationType = "TBD", Version = "0.0.0" });
        Register(new ComponentDescriptor { Id = "audio-input", Capability = "audio-input", ImplementationType = "TBD", Version = "0.0.0" });
        Register(new ComponentDescriptor { Id = "data-parsing", Capability = "data-parsing", ImplementationType = "TBD", Version = "0.0.0" });

        // Action
        Register(new ComponentDescriptor { Id = "ui-interaction", Capability = "ui-interaction", ImplementationType = "TBD", Version = "0.0.0" });
        Register(new ComponentDescriptor { Id = "process-control", Capability = "process-control", ImplementationType = "TBD", Version = "0.0.0" });

        // Memory
        Register(new ComponentDescriptor { Id = "episodic-memory", Capability = "episodic-memory", ImplementationType = "TBD", Version = "0.0.0" });

        // Reporting
        Register(new ComponentDescriptor { Id = "suggestion-surface", Capability = "suggestion-surface", ImplementationType = "TBD", Version = "0.0.0" });
    }
}
