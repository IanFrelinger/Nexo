namespace Nexo.Core.Domain.Values;

public sealed class SecuritySeverity : BaseTypeValue
{
    public SecuritySeverity(string value, string display) : base(value, display) { }
    
    public static readonly SecuritySeverity Low    = new("low","Low");
    public static readonly SecuritySeverity Medium = new("med","Medium");
    public static readonly SecuritySeverity High   = new("high","High");
}