using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Predictive;
using Nexo.Infrastructure.Services.Predictive;
using Nexo.Infrastructure.Tests.Services.Predictive.Success;
using Nexo.Infrastructure.Tests.Services.Predictive.ErrorHandling;
using Nexo.Infrastructure.Tests.Services.Predictive.Cancellation;

namespace Nexo.Infrastructure.Tests.Services.Predictive.Core
{
    /// <summary>
    /// Core test orchestrator for Predictive Development Service tests.
    /// Delegates to specialized test classes for different test categories.
    /// </summary>
    public partial class PredictiveDevelopmentServiceTests : IDisposable
    {
        private readonly Mock<ILogger<PredictiveDevelopmentService>> _mockLogger;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly PredictiveDevelopmentService _predictiveDevelopmentService;

        public PredictiveDevelopmentServiceTests()
        {
            _mockLogger = new Mock<ILogger<PredictiveDevelopmentService>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _predictiveDevelopmentService = new PredictiveDevelopmentService(_mockLogger.Object, _mockModelOrchestrator.Object);
        }

        // Delegate to specialized test classes
        private readonly SuccessCaseTests _successTests;
        private readonly ErrorHandlingTests _errorTests;
        private readonly CancellationTests _cancellationTests;

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
