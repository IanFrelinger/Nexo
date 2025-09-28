using Nexo.Shared.Values;
namespace Nexo.Core.Domain.Values;
public sealed record SecuritySeverity(string Value, string Display) : BaseTypeValue(Value, Display)
{
    public static readonly SecuritySeverity Low    = new("low","Low");
    public static readonly SecuritySeverity Medium = new("med","Medium");
    public static readonly SecuritySeverity High   = new("high","High");
}