using Ashlar.Core.Application.Composition.Models;

namespace Ashlar.Core.Application.Composition.Ports;

/// <summary>
/// Registry of capability components. Maps bricks to descriptors.
/// </summary>
public interface ICapabilityComponentRegistry
{
    void Register(ComponentDescriptor descriptor);
    IReadOnlyList<ComponentDescriptor> GetByCapability(string capability);
    ComponentDescriptor? GetById(string id);
    IReadOnlyList<ComponentDescriptor> GetAll();
    IReadOnlyList<ComponentValidationIssue> ValidateSelection(IReadOnlyList<string> componentIds, IReadOnlyCollection<string> availableCapabilities);
}
