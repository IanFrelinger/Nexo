// This file has been refactored into specialized test classes:
// - Core/PlatformFeatureDetectorTests.cs - Main test orchestrator
// - Models/ModelTests.cs - Tests for data models and interface implementation
// - Functionality/FunctionalityTests.cs - Tests for core service functionality
// - CrossPlatform/CrossPlatformTests.cs - Tests for cross-platform scenarios
// - ErrorHandling/ErrorHandlingTests.cs - Tests for error handling scenarios
// - Performance/PerformanceTests.cs - Tests for performance requirements

// Re-export all test classes for backward compatibility
using Nexo.Feature.Platform.Tests.Services.Core;
using Nexo.Feature.Platform.Tests.Services.Models;
using Nexo.Feature.Platform.Tests.Services.Functionality;
using Nexo.Feature.Platform.Tests.Services.CrossPlatform;
using Nexo.Feature.Platform.Tests.Services.ErrorHandling;
using Nexo.Feature.Platform.Tests.Services.Performance;

namespace Nexo.Feature.Platform.Tests.Services
{
    // All platform feature detector test classes are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable test classes
} 