namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when configuration operations fail.
/// 
/// Contains:
/// - ErrorCode: Structured error code for programmatic handling
/// - Suggestion: Optional suggestion for resolving the error
/// 
/// Used when configuration loading, saving, or validation fails.
/// </summary>
public class ConfigurationException : DomainException
{
    /// <summary>Optional structured error code from <see cref="ErrorCodes"/>.</summary>
    public string? ErrorCode { get; }

    /// <summary>Optional operator suggestion for resolving the configuration failure.</summary>
    public string? Suggestion { get; }

    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public ConfigurationException(string message, string errorCode, string? suggestion = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }

    public ConfigurationException(string message, string errorCode, Exception innerException, string? suggestion = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }
}

