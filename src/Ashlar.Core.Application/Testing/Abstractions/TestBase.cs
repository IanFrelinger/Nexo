using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Core.Application.Testing.Abstractions;

/// <summary>
/// Base class for all tests in the Ashlar framework.
/// 
/// Provides:
/// - Test name and category (derived from type)
/// - Abstract ExecuteAsync method for test logic
/// - Virtual SetupAsync and CleanupAsync hooks
/// 
/// Used by ITestRunner to discover and execute tests.
/// All test classes should inherit from this base class.
/// </summary>
public abstract class TestBase
{
    /// <summary>Test name derived from the concrete test type.</summary>
    public string TestName => GetType().Name;

    /// <summary>Test category derived from the type namespace.</summary>
    public string Category => GetType().Namespace?.Split('.').LastOrDefault() ?? "Unknown";

    /// <summary>
    /// Executes the test and returns the result.
    /// </summary>
    public abstract Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets up test fixtures before execution.
    /// </summary>
    public virtual Task SetupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Cleans up test fixtures after execution.
    /// </summary>
    public virtual Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

