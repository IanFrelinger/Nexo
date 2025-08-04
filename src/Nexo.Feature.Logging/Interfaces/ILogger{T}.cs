namespace Nexo.Core.Application.Interfaces.Logging
{
    /// <summary>
    /// Represents a logger that can be used to log messages for a specific type.
    /// This interface provides type-safe logging functionality.
    /// </summary>
    /// <typeparam name="T">The type that this logger is associated with.</typeparam>
    public interface ILogger<out T> : ILogger
    {
        // This interface inherits all functionality from ILogger
        // The generic type parameter provides type safety and context
    }
} 