using System.Collections.Generic;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Framework-agnostic application logic container that can be transformed into platform-specific implementations.
    /// Part of Epic 5.3: Application Logic Standardization.
    /// </summary>
    public class StandardizedApplicationLogic
    {
        /// <summary>
        /// Application patterns (Repository, UnitOfWork, Command, Query, Mediator)
        /// </summary>
        public List<ApplicationPattern> Patterns { get; set; } = new List<ApplicationPattern>();

        /// <summary>
        /// Security patterns (JWT, Authorization, Input Validation, Data Encryption)
        /// </summary>
        public List<SecurityPattern> SecurityPatterns { get; set; } = new List<SecurityPattern>();

        /// <summary>
        /// State management patterns (Global state, local state, Redux, MobX)
        /// </summary>
        public List<StateManagementPattern> StateManagementPatterns { get; set; } = new List<StateManagementPattern>();

        /// <summary>
        /// API contracts with parameters, responses, and validation
        /// </summary>
        public List<ApiContract> ApiContracts { get; set; } = new List<ApiContract>();

        /// <summary>
        /// Data flow patterns (Unidirectional, bidirectional, event-driven)
        /// </summary>
        public List<DataFlowPattern> DataFlowPatterns { get; set; } = new List<DataFlowPattern>();

        /// <summary>
        /// Caching strategies (Memory cache, distributed cache, response cache)
        /// </summary>
        public List<CachingStrategy> CachingStrategies { get; set; } = new List<CachingStrategy>();
    }

    /// <summary>
    /// Application pattern definition
    /// </summary>
    public class ApplicationPattern
    {
        public string Name { get; set; } = string.Empty;
        public PatternType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Security pattern definition
    /// </summary>
    public class SecurityPattern
    {
        public string Name { get; set; } = string.Empty;
        public SecurityPatternType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
    }

    /// <summary>
    /// State management pattern definition
    /// </summary>
    public class StateManagementPattern
    {
        public string Name { get; set; } = string.Empty;
        public StateManagementType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
    }

    /// <summary>
    /// API contract definition
    /// </summary>
    public class ApiContract
    {
        public string Name { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public Enums.HttpMethod Method { get; set; }
        public List<ApiParameter> Parameters { get; set; } = new List<ApiParameter>();
        public ApiResponse Response { get; set; } = new ApiResponse();
    }

    /// <summary>
    /// API parameter definition
    /// </summary>
    public class ApiParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
    }

    /// <summary>
    /// API response definition
    /// </summary>
    public class ApiResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data flow pattern definition
    /// </summary>
    public class DataFlowPattern
    {
        public string Name { get; set; } = string.Empty;
        public DataFlowType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Caching strategy definition
    /// </summary>
    public class CachingStrategy
    {
        public string Name { get; set; } = string.Empty;
        public CachingStrategyType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
    }
}
