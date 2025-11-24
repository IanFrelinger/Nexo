namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when analysis operations fail.
/// </summary>
public class AnalysisException : DomainException
{
    public AnalysisException(string message) : base(message)
    {
    }

    public AnalysisException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

