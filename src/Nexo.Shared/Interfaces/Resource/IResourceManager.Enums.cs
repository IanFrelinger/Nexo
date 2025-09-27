using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Shared.Interfaces.Resource
{
    /// <summary>
    /// Enum definitions for IResourceManager.
    /// </summary>
    public partial interface IResourceManager
    {
        // This interface acts as an orchestrator for various resource management functionalities,
        // with specific categories defined in partial interfaces.
    }

    /// <summary>
    /// Resource types.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// CPU resources.
        /// </summary>
        CPU,

        /// <summary>
        /// Memory resources.
        /// </summary>
        Memory,

        /// <summary>
        /// GPU resources.
        /// </summary>
        GPU,

        /// <summary>
        /// Storage resources.
        /// </summary>
        Storage,

        /// <summary>
        /// Network bandwidth.
        /// </summary>
        Network,

        /// <summary>
        /// AI model resources.
        /// </summary>
        AIModel
    }

    /// <summary>
    /// Resource priority levels.
    /// </summary>
    public enum ResourcePriority
    {
        /// <summary>
        /// Low priority.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Resource alert types.
    /// </summary>
    public enum ResourceAlertType
    {
        /// <summary>
        /// High utilization alert.
        /// </summary>
        HighUtilization,

        /// <summary>
        /// Resource exhaustion alert.
        /// </summary>
        ResourceExhaustion,

        /// <summary>
        /// Allocation failure alert.
        /// </summary>
        AllocationFailure,

        /// <summary>
        /// Provider health alert.
        /// </summary>
        ProviderHealth
    }

    /// <summary>
    /// Resource alert severity levels.
    /// </summary>
    public enum ResourceAlertSeverity
    {
        /// <summary>
        /// Information level.
        /// </summary>
        Information,

        /// <summary>
        /// Warning level.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Resource health levels.
    /// </summary>
    public enum ResourceHealth
    {
        /// <summary>
        /// Healthy status.
        /// </summary>
        Healthy,

        /// <summary>
        /// Degraded status.
        /// </summary>
        Degraded,

        /// <summary>
        /// Unhealthy status.
        /// </summary>
        Unhealthy,

        /// <summary>
        /// Unknown status.
        /// </summary>
        Unknown
    }
}
