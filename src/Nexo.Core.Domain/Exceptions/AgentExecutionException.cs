namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when agent execution fails.
/// </summary>
public class AgentExecutionException : DomainException
{
    public string AgentName { get; }

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
}

