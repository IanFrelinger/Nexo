using System;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// iOS native code generator for Phase 6.
    /// Generates native iOS code with Swift UI, Core Data, and Metal optimization.
    /// </summary>
    public partial class iOSCodeGenerator : IIOSCodeGenerator
    {
        private readonly ILogger<iOSCodeGenerator> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public iOSCodeGenerator(
            ILogger<iOSCodeGenerator> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

    }
}
