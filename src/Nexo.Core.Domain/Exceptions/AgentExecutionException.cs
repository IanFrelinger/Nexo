namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when agent execution fails.
/// 
/// Contains:
/// - AgentName: Name of the agent that failed
/// - ErrorCode: Structured error code for programmatic handling
/// - Suggestion: Optional suggestion for resolving the error
/// 
/// Used throughout the application layer when agent execution encounters errors.
/// </summary>
public class AgentExecutionException : DomainException
{
    public string AgentName { get; }
    public string? ErrorCode { get; }
    public string? Suggestion { get; }

    public AgentExecutionException(string agentName, string message) 
        : base(message)
    {
        AgentName = agentName;
    }

    public AgentExecutionException(string agentName, string message, Exception innerException) 
        : base(message, innerException)
    {
        AgentName = agentName;
    }

    public AgentExecutionException(string agentName, string message, string errorCode, string? suggestion = null) 
        : base(message)
    {
        AgentName = agentName;
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }

    public AgentExecutionException(string agentName, string message, string errorCode, Exception innerException, string? suggestion = null) 
        : base(message, innerException)
    {
        AgentName = agentName;
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }
}

