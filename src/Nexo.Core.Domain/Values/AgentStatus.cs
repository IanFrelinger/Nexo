namespace Nexo.Core.Domain.Values;

/// <summary>
/// Represents the status of an agent as a value object.
/// 
/// Provides predefined status values:
/// - Idle: Agent is not currently active
/// - Active: Agent is currently executing
/// 
/// Inherits from BaseTypeValue for value/display pair representation.
/// Used throughout the domain to represent agent status.
/// </summary>
public sealed class AgentStatus : BaseTypeValue
{
    public AgentStatus(string value, string display) : base(value, display) { }
    
    public static readonly AgentStatus Idle   = new("idle",   "Idle");
    public static readonly AgentStatus Active = new("active", "Active");
}