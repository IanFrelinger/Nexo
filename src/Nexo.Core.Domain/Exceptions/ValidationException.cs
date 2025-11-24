namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when validation operations fail.
/// </summary>
public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

