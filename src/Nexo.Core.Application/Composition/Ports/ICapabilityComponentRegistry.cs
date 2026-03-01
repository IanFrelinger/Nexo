using Nexo.Core.Application.Composition.Models;

namespace Nexo.Core.Application.Composition.Ports;

/// <summary>
/// Registry of capability components. Maps bricks to descriptors.
/// </summary>
public interface ICapabilityComponentRegistry
{
    void Register(ComponentDescriptor descriptor);
    IReadOnlyList<ComponentDescriptor> GetByCapability(string capability);
    ComponentDescriptor? GetById(string id);
}
