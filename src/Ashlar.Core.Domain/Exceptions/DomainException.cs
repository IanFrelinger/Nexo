namespace Ashlar.Core.Domain.Exceptions;

/// <summary>
/// Base class for all domain exceptions.
/// 
/// Provides a common base for all domain-specific exceptions, following
/// the Domain-Driven Design pattern. All domain exceptions inherit from
/// this class to enable consistent exception handling across the domain layer.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Creates a domain exception with a message.</summary>
    protected DomainException(string message) : base(message)
    {
    }

    /// <summary>Creates a domain exception with a message and inner cause.</summary>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

