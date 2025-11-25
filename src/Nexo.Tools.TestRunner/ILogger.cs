namespace Nexo.Tools.TestRunner;

/// <summary>
/// Simple logging interface for the test runner.
/// Allows logging to be swapped out via dependency injection or configuration.
/// </summary>
public interface ILogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogDebug(string message);
}

/// <summary>
/// Console-based logger implementation.
/// </summary>
public sealed class ConsoleLogger : ILogger
{
    private readonly bool _verbose;

    public ConsoleLogger(bool verbose = false)
    {
        _verbose = verbose;
    }

    public void LogInformation(string message)
    {
        Console.WriteLine(message);
    }

    public void LogWarning(string message)
    {
        Console.WriteLine($"[WARN] {message}");
    }

    public void LogError(string message)
    {
        Console.Error.WriteLine($"[ERROR] {message}");
    }

    public void LogDebug(string message)
    {
        if (_verbose)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }
    }
}

