using Nexo.Core.Application.Testing.Models;

namespace Nexo.Core.Application.Testing.Abstractions;

/// <summary>
/// Base class for unit tests with common assertions.
/// 
/// Provides:
/// - Common assertion methods (AssertTrue, AssertEqual, etc.)
/// - Exception assertion helpers
/// - Async assertion support
/// 
/// Inherits from TestBase for test discovery and execution.
/// Used by unit tests throughout the application.
/// </summary>
public abstract class UnitTestBase : TestBase
{
    protected void AssertTrue(bool condition, string? message = null)
    {
        if (!condition)
            throw new AssertionException(message ?? "Assertion failed: expected true");
    }

    protected void AssertFalse(bool condition, string? message = null)
    {
        if (condition)
            throw new AssertionException(message ?? "Assertion failed: expected false");
    }

    protected void AssertEqual<T>(T expected, T actual, string? message = null)
    {
        if (!Equals(expected, actual))
            throw new AssertionException(message ?? $"Assertion failed: expected {expected}, got {actual}");
    }

    protected void AssertNotNull(object? obj, string? message = null)
    {
        if (obj == null)
            throw new AssertionException(message ?? "Assertion failed: expected non-null value");
    }

    protected void AssertNull(object? obj, string? message = null)
    {
        if (obj != null)
            throw new AssertionException(message ?? "Assertion failed: expected null value");
    }

    protected void AssertThrows<TException>(Action action, string? message = null) where TException : Exception
    {
        try
        {
            action();
            throw new AssertionException(message ?? $"Assertion failed: expected {typeof(TException).Name} to be thrown");
        }
        catch (TException)
        {
            // Expected
        }
        catch (Exception ex) when (ex is not AssertionException)
        {
            throw new AssertionException(
                message ?? $"Assertion failed: expected {typeof(TException).Name}, but {ex.GetType().Name} was thrown: {ex.Message}",
                ex);
        }
    }

    protected async Task AssertThrowsAsync<TException>(Func<Task> action, string? message = null) where TException : Exception
    {
        try
        {
            await action();
            throw new AssertionException(message ?? $"Assertion failed: expected {typeof(TException).Name} to be thrown");
        }
        catch (TException)
        {
            // Expected
        }
        catch (Exception ex) when (ex is not AssertionException)
        {
            throw new AssertionException(
                message ?? $"Assertion failed: expected {typeof(TException).Name}, but {ex.GetType().Name} was thrown: {ex.Message}",
                ex);
        }
    }
}

/// <summary>
/// Exception thrown when an assertion fails.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
    public AssertionException(string message, Exception innerException) : base(message, innerException) { }
}

