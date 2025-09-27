using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Observability;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Models;
using Nexo.Core.Application.Services;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Services;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Services;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Services;
using Nexo.Feature.Template.Interfaces;
using Nexo.Feature.Template.Services;
using Nexo.Infrastructure.Services;
using Nexo.Infrastructure.Services.AI;
using Nexo.Infrastructure.Services.Caching;
using Nexo.Shared;
using Nexo.Shared.Models;
using Nexo.Shared.Services;
using Nexo.Shared.Interfaces;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Services;
using Nexo.Infrastructure.Services.Resource;
using Nexo.Shared.Interfaces.Resource;
using Nexo.Feature.Factory;
using Nexo.Feature.Unity;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Extensions;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;

namespace Nexo.CLI
{
    /// <summary>
    /// Dependency injection configuration for the Nexo CLI application.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public static partial class DependencyInjection
    {
        // This class acts as an orchestrator for various dependency injection functionalities,
        // with specific categories defined in partial classes.
    }
}