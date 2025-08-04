using System;

namespace Nexo.Core.Application.Interfaces.Logging
{
    /// <summary>
    /// Represents a factory for creating loggers.
    /// This interface abstracts logger creation to allow for different logging implementations.
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Creates a logger for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to create a logger for.</typeparam>
        /// <returns>A logger instance.</returns>
        ILogger<T> CreateLogger<T>();

        /// <summary>
        /// Creates a logger for the specified type.
        /// </summary>
        /// <param name="type">The type to create a logger for.</param>
        /// <returns>A logger instance.</returns>
        ILogger CreateLogger(Type type);

        /// <summary>
        /// Creates a logger with the specified category name.
        /// </summary>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>A logger instance.</returns>
        ILogger CreateLogger(string categoryName);

        /// <summary>
        /// Adds a logger provider to the factory.
        /// </summary>
        /// <param name="provider">The logger provider to add.</param>
        void AddProvider(ILoggerProvider provider);

        /// <summary>
        /// Disposes the factory and all its providers.
        /// </summary>
        void Dispose();
    }
} 