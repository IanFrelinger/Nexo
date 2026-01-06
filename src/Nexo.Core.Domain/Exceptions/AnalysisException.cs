namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when analysis operations fail.
/// 
/// Contains:
/// - ErrorCode: Structured error code for programmatic handling
/// - Suggestion: Optional suggestion for resolving the error
/// 
/// Used when code or assembly analysis operations fail.
/// </summary>
public class AnalysisException : DomainException
{
    public string? ErrorCode { get; }
    public string? Suggestion { get; }

    public AnalysisException(string message) : base(message)
    {
    }

    public AnalysisException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public AnalysisException(string message, string errorCode, string? suggestion = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }

    public AnalysisException(string message, string errorCode, Exception innerException, string? suggestion = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }
}

