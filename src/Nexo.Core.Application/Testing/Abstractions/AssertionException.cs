using Nexo.Core.Application.Testing.Models;

namespace Nexo.Core.Application.Testing.Abstractions;

/// <summary>Exception thrown when an assertion fails.</summary>
public class AssertionException : Exception
{
    /// <summary>Creates an assertion exception with the given message.</summary>
    /// <param name="message">Description of the failed assertion.</param>
    public AssertionException(string message) : base(message) { }

    /// <summary>Creates an assertion exception with message and inner cause.</summary>
    /// <param name="message">Description of the failed assertion.</param>
    /// <param name="innerException">Underlying exception, if any.</param>
    public AssertionException(string message, Exception innerException) : base(message, innerException) { }
}
