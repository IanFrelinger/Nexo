namespace Nexo.Core.Domain.Values;

public sealed class AgentStatus : BaseTypeValue
{
    public AgentStatus(string value, string display) : base(value, display) { }
    
    public static readonly AgentStatus Idle   = new("idle",   "Idle");
    public static readonly AgentStatus Active = new("active", "Active");
}