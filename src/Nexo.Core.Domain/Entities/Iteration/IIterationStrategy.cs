// This file has been refactored into specialized iteration strategy classes:
// - Core/IIterationStrategy.cs - Main iteration strategy interface
// - Performance/IterationPerformanceModels.cs - Performance-related models
// - Context/IterationContextModels.cs - Context-related models and interfaces
// - Requirements/IterationRequirementsModels.cs - Requirements and code generation models
// - Enums/IterationEnums.cs - Enumerations for iteration strategies
// - Extensions/PerformanceRequirementsExtensions.cs - Extension methods

// Re-export all classes for backward compatibility
using Nexo.Core.Domain.Entities.Iteration.Core;
using Nexo.Core.Domain.Entities.Iteration.Performance;
using Nexo.Core.Domain.Entities.Iteration.Context;
using Nexo.Core.Domain.Entities.Iteration.Requirements;
using Nexo.Core.Domain.Entities.Iteration.Enums;
using Nexo.Core.Domain.Entities.Iteration.Extensions;

namespace Nexo.Core.Domain.Entities.Iteration
{
    // All iteration strategy classes are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable classes
}
