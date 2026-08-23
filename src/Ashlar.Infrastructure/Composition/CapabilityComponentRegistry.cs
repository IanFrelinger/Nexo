using System.Collections.Concurrent;
using Ashlar.Core.Application.Composition.Models;
using Ashlar.Core.Application.Composition.Ports;

namespace Ashlar.Infrastructure.Composition;

/// <summary>
/// In-memory registry of capability components. Seed from existing bricks.
/// </summary>
public sealed class CapabilityComponentRegistry : ICapabilityComponentRegistry
{
    private readonly ConcurrentDictionary<string, ComponentDescriptor> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<ComponentDescriptor>> _byCapability = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes a new capability component registry.</summary>
    public CapabilityComponentRegistry()
    {
        SeedFromCodeAnalysis();
        SeedTestRunnerComponents();
        SeedPlaceholderComponents();
    }

    /// <inheritdoc />
    public void Register(ComponentDescriptor descriptor)
    {
        var metadataIssues = ComponentDescriptorValidator.Validate(descriptor);
        if (metadataIssues.Count > 0)
        {
            var ordered = metadataIssues
                .OrderBy(issue => issue.Code, StringComparer.Ordinal)
                .ThenBy(issue => issue.ComponentId, StringComparer.Ordinal)
                .ThenBy(issue => issue.Message, StringComparer.Ordinal)
                .Select(issue => $"{issue.Code}: {issue.Message}");
            throw new ArgumentException(
                $"Component descriptor '{descriptor.Id}' is invalid. {string.Join(" | ", ordered)}",
                nameof(descriptor));
        }

        _byId[descriptor.Id] = descriptor;
        var list = _byCapability.GetOrAdd(descriptor.Capability, _ => new List<ComponentDescriptor>());
        lock (list)
        {
            if (!list.Any(x => string.Equals(x.Id, descriptor.Id, StringComparison.OrdinalIgnoreCase)))
                list.Add(descriptor);
            else
            {
                var existingIndex = list.FindIndex(x => string.Equals(x.Id, descriptor.Id, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                    list[existingIndex] = descriptor;
            }
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

    /// <inheritdoc />
    public IReadOnlyList<ComponentDescriptor> GetAll()
    {
        return _byId.Values
            .OrderBy(descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<ComponentValidationIssue> ValidateSelection(IReadOnlyList<string> componentIds, IReadOnlyCollection<string> availableCapabilities)
    {
        var issues = new List<ComponentValidationIssue>();
        var orderedIds = componentIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var available = new HashSet<string>(
            availableCapabilities.Where(capability => !string.IsNullOrWhiteSpace(capability)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var id in orderedIds)
        {
            var descriptor = GetById(id);
            if (descriptor == null)
            {
                issues.Add(new ComponentValidationIssue("selection.missing-component", id, $"Component '{id}' is not registered."));
                continue;
            }

            var metadataIssues = ComponentDescriptorValidator.Validate(descriptor);
            issues.AddRange(metadataIssues);

            foreach (var requiredCapability in descriptor.RequiredCapabilities
                         .Where(capability => !string.IsNullOrWhiteSpace(capability))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(capability => capability, StringComparer.OrdinalIgnoreCase))
            {
                if (available.Contains(requiredCapability))
                    continue;

                issues.Add(new ComponentValidationIssue(
                    "selection.missing-required-capability",
                    descriptor.Id,
                    $"Component '{descriptor.Id}' requires capability '{requiredCapability}', but it is unavailable."));
            }
        }

        foreach (var id in orderedIds)
        {
            var descriptor = GetById(id);
            if (descriptor == null)
                continue;

            foreach (var incompatibleComponent in descriptor.IncompatibleWith
                         .Where(component => !string.IsNullOrWhiteSpace(component))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(component => component, StringComparer.OrdinalIgnoreCase))
            {
                if (!orderedIds.Any(selected => string.Equals(selected, incompatibleComponent, StringComparison.OrdinalIgnoreCase)))
                    continue;

                issues.Add(new ComponentValidationIssue(
                    "selection.incompatible-components",
                    descriptor.Id,
                    $"Component '{descriptor.Id}' is incompatible with '{incompatibleComponent}'."));
            }
        }

        return issues
            .Distinct()
            .OrderBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.ComponentId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToList();
    }

    private void SeedFromCodeAnalysis()
    {
        Register(new ComponentDescriptor
        {
            Id = "code-analysis",
            Capability = "code-analysis",
            DisplayName = "Code analysis",
            Summary = "Static code analysis and architecture scoring.",
            ImplementationType = "Ashlar.Infrastructure.Analysis.BrickAnalyzer.RoslynBrickStaticAnalyzer, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"}}}",
            OutputSchema = "{\"type\":\"object\",\"properties\":{\"passed\":{\"type\":\"boolean\"},\"violations\":{\"type\":\"array\"}}}",
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
            DisplayName = "Test discovery",
            Summary = "Discovers test matrices and parameter combinations.",
            ImplementationType = "Ashlar.Core.Application.ParallelTesting.Ports.IParameterMatrixGenerator",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\",\"properties\":{\"projectPath\":{\"type\":\"string\"}}}",
            OutputSchema = "{\"type\":\"object\",\"properties\":{\"matrix\":{\"type\":\"array\"}}}",
        });
        Register(new ComponentDescriptor
        {
            Id = "test-execution",
            Capability = "test-execution",
            DisplayName = "Test execution",
            Summary = "Spawns execution workers for test runs.",
            ImplementationType = "Ashlar.Core.Application.ParallelTesting.Ports.IInstanceSpawner",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\",\"properties\":{\"matrix\":{\"type\":\"array\"},\"filter\":{\"type\":\"string\"}}}",
            OutputSchema = "{\"type\":\"object\",\"properties\":{\"runs\":{\"type\":\"array\"}}}",
            RequiredCapabilities = ["test-discovery"],
        });
        Register(new ComponentDescriptor
        {
            Id = "result-aggregation",
            Capability = "result-aggregation",
            DisplayName = "Result aggregation",
            Summary = "Collects test outcomes into deterministic summaries.",
            ImplementationType = "Ashlar.Core.Application.ParallelTesting.Ports.IResultCollector",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\",\"properties\":{\"runs\":{\"type\":\"array\"}}}",
            OutputSchema = "{\"type\":\"object\",\"properties\":{\"summary\":{\"type\":\"string\"},\"passed\":{\"type\":\"boolean\"}}}",
            RequiredCapabilities = ["test-execution"],
        });
    }

    /// <summary>
    /// Placeholder descriptors for North Star component families (Perception, Action, Reasoning, Memory, Reporting).
    /// Enables composition engine to discover required capabilities. Pipeline IDs (perception, validation, reporting,
    /// understanding) map to existing or placeholder components so ashlar compose does not fail.
    /// See docs/ComponentLibrary.md and docs/SeedComponentLibraryAudit.md.
    /// </summary>
    private void SeedPlaceholderComponents()
    {
        // Pipeline IDs used by CompositionEngine rules (perception, validation, reporting, understanding)
        Register(new ComponentDescriptor
        {
            Id = "perception",
            Capability = "perception",
            DisplayName = "Perception",
            Summary = "Collects observation context used by downstream stages.",
            ImplementationType = "Ashlar.Infrastructure.Observation.ObservationContextBrick, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\",\"properties\":{\"projectPath\":{\"type\":\"string\"},\"since\":{\"type\":\"string\"}}}",
            OutputSchema = "{\"type\":\"object\",\"properties\":{\"patterns\":{\"type\":\"array\"},\"events\":{\"type\":\"array\"}}}",
        });
        Register(new ComponentDescriptor
        {
            Id = "validation",
            Capability = "validation",
            DisplayName = "Validation",
            Summary = "Validates generated outputs for policy and correctness.",
            ImplementationType = "Ashlar.Infrastructure.Analysis.BrickAnalyzer.RoslynBrickStaticAnalyzer, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"}}}",
            OutputSchema = "{\"type\":\"object\",\"properties\":{\"passed\":{\"type\":\"boolean\"},\"violations\":{\"type\":\"array\"}}}",
            RequiredCapabilities = ["perception"],
        });
        Register(new ComponentDescriptor
        {
            Id = "reporting",
            Capability = "reporting",
            DisplayName = "Reporting",
            Summary = "Publishes composed workflow results to operators.",
            ImplementationType = "Ashlar.Infrastructure.SelfContext.ChangelogGenerator, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\",\"properties\":{\"since\":{\"type\":\"string\"},\"format\":{\"type\":\"string\"}}}",
            OutputSchema = "{\"type\":\"object\",\"properties\":{\"report\":{\"type\":\"string\"}}}",
            RequiredCapabilities = ["validation"],
        });
        Register(new ComponentDescriptor
        {
            Id = "understanding",
            Capability = "understanding",
            DisplayName = "Understanding",
            Summary = "Interprets context before analysis and adaptation.",
            ImplementationType = "Ashlar.Infrastructure.Observation.ObservationContextBrick, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\",\"properties\":{\"context\":{\"type\":\"object\"}}}",
            OutputSchema = "{\"type\":\"object\",\"properties\":{\"interpretation\":{\"type\":\"object\"}}}",
        });

        // Perception (specialized)
        Register(new ComponentDescriptor
        {
            Id = "vision-input",
            Capability = "vision-input",
            DisplayName = "Vision input",
            Summary = "Placeholder for specialized image/video ingestion.",
            ImplementationType = "Ashlar.Infrastructure.Composition.PlaceholderCapabilityComponent, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Placeholder,
        });
        Register(new ComponentDescriptor
        {
            Id = "audio-input",
            Capability = "audio-input",
            DisplayName = "Audio input",
            Summary = "Placeholder for speech and audio ingestion.",
            ImplementationType = "Ashlar.Infrastructure.Composition.PlaceholderCapabilityComponent, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Placeholder,
        });
        Register(new ComponentDescriptor
        {
            Id = "data-parsing",
            Capability = "data-parsing",
            DisplayName = "Data parsing",
            Summary = "Placeholder for structured/unstructured data parsing.",
            ImplementationType = "Ashlar.Infrastructure.Composition.PlaceholderCapabilityComponent, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Placeholder,
        });

        // Action
        Register(new ComponentDescriptor
        {
            Id = "ui-interaction",
            Capability = "ui-interaction",
            DisplayName = "UI interaction",
            Summary = "Placeholder for deterministic UI automation components.",
            ImplementationType = "Ashlar.Infrastructure.Composition.PlaceholderCapabilityComponent, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Placeholder,
            IncompatibleWith = ["process-control"],
        });
        Register(new ComponentDescriptor
        {
            Id = "process-control",
            Capability = "process-control",
            DisplayName = "Process control",
            Summary = "Placeholder for shell and process orchestration.",
            ImplementationType = "Ashlar.Infrastructure.Composition.PlaceholderCapabilityComponent, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Placeholder,
            IncompatibleWith = ["ui-interaction"],
        });

        // Memory
        Register(new ComponentDescriptor
        {
            Id = "episodic-memory",
            Capability = "episodic-memory",
            DisplayName = "Episodic memory",
            Summary = "Placeholder for long-horizon memory indexing.",
            ImplementationType = "Ashlar.Infrastructure.Composition.PlaceholderCapabilityComponent, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Placeholder,
        });

        // Reporting
        Register(new ComponentDescriptor
        {
            Id = "suggestion-surface",
            Capability = "suggestion-surface",
            DisplayName = "Suggestion surface",
            Summary = "Placeholder for operator-facing recommendation rendering.",
            ImplementationType = "Ashlar.Infrastructure.Composition.PlaceholderCapabilityComponent, Ashlar.Infrastructure",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Placeholder,
        });
    }
}
