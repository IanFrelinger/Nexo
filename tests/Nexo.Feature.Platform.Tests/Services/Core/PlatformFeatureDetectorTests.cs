using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;
using Xunit;
using Nexo.Feature.Platform.Tests.Services.Models;
using Nexo.Feature.Platform.Tests.Services.Functionality;
using Nexo.Feature.Platform.Tests.Services.CrossPlatform;
using Nexo.Feature.Platform.Tests.Services.ErrorHandling;
using Nexo.Feature.Platform.Tests.Services.Performance;

namespace Nexo.Feature.Platform.Tests.Services.Core
{
    /// <summary>
    /// Core test orchestrator for Platform Feature Detector tests.
    /// Delegates to specialized test classes for different test categories.
    /// </summary>
    public partial class PlatformFeatureDetectorTests
    {
        private readonly Mock<ILogger<PlatformFeatureDetector>> _mockLogger;
        private readonly PlatformFeatureDetector _detector;

        public PlatformFeatureDetectorTests()
        {
            _mockLogger = new Mock<ILogger<PlatformFeatureDetector>>();
            _detector = new PlatformFeatureDetector(_mockLogger.Object);
        }

        // Delegate to specialized test classes
        private readonly ModelTests _modelTests;
        private readonly FunctionalityTests _functionalityTests;
        private readonly CrossPlatformTests _crossPlatformTests;
        private readonly ErrorHandlingTests _errorTests;
        private readonly PerformanceTests _performanceTests;
    }
}
