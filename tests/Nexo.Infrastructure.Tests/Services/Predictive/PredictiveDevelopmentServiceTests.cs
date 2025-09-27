// This file has been refactored into specialized test classes:
// - Core/PredictiveDevelopmentServiceTests.cs - Main test orchestrator
// - Success/SuccessCaseTests.cs - Tests for successful execution scenarios
// - ErrorHandling/ErrorHandlingTests.cs - Tests for error handling scenarios
// - Cancellation/CancellationTests.cs - Tests for cancellation token behavior

// Re-export all test classes for backward compatibility
using Nexo.Infrastructure.Tests.Services.Predictive.Core;
using Nexo.Infrastructure.Tests.Services.Predictive.Success;
using Nexo.Infrastructure.Tests.Services.Predictive.ErrorHandling;
using Nexo.Infrastructure.Tests.Services.Predictive.Cancellation;

namespace Nexo.Infrastructure.Tests.Services.Predictive
{
    // All predictive development service test classes are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable test classes
}
