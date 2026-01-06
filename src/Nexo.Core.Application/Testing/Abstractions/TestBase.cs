using Nexo.Core.Application.Testing.Models;

namespace Nexo.Core.Application.Testing.Abstractions;

/// <summary>
/// Base class for all tests in the Nexo framework.
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
    public string TestName => GetType().Name;
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

