using Nexo.Shared.Values;
namespace Nexo.Core.Domain.Values;
public sealed record AgentStatus(string Value, string Display) : BaseTypeValue(Value, Display)
{
    public static readonly AgentStatus Idle   = new("idle",   "Idle");
    public static readonly AgentStatus Active = new("active", "Active");
}