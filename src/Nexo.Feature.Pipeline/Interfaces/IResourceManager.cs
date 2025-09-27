// This file has been refactored into specialized resource manager classes:
// - Core/IResourceManager.cs - Main resource manager interface
// - Requirements/ResourceRequirementsModels.cs - Resource requirements models
// - Allocation/ResourceAllocationModels.cs - Resource allocation models
// - Utilization/ResourceUtilizationModels.cs - Resource utilization models
// - Optimization/ResourceOptimizationModels.cs - Resource optimization models
// - Enums/ResourceEnums.cs - Resource-related enumerations

// Re-export all classes for backward compatibility
using Nexo.Feature.Pipeline.Interfaces.Core;
using Nexo.Feature.Pipeline.Interfaces.Requirements;
using Nexo.Feature.Pipeline.Interfaces.Allocation;
using Nexo.Feature.Pipeline.Interfaces.Utilization;
using Nexo.Feature.Pipeline.Interfaces.Optimization;
using Nexo.Feature.Pipeline.Interfaces.Enums;

namespace Nexo.Feature.Pipeline.Interfaces
{
    // All resource manager classes are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable classes
}
