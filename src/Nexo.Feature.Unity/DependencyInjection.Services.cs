using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Services;
using Nexo.Feature.Unity.AI.Agents;
using Nexo.Feature.Unity.Workflows;
using Nexo.Feature.Unity.Monitoring;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Application.Services.Adaptation;

namespace Nexo.Feature.Unity
{
    /// <summary>
    /// Service implementations for DependencyInjection.
    /// This class acts as an orchestrator, delegating specific functionality to partial class implementations.
    /// </summary>
    public static partial class DependencyInjection
    {
        // This class acts as an orchestrator for various Unity dependency injection functionalities,
        // with specific categories defined in partial classes.
    }
}