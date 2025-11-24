namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when validation operations fail.
/// </summary>
public class ValidationException : DomainException
{
    public string? ErrorCode { get; }
    public string? Suggestion { get; }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public ValidationException(string message, string errorCode, string? suggestion = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }

    public ValidationException(string message, string errorCode, Exception innerException, string? suggestion = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }
}

