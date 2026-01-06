namespace Nexo.Core.Domain.Values;

/// <summary>
/// Represents security severity as a value object.
/// 
/// Provides predefined security severity values:
/// - Low: Low security severity
/// - Medium: Medium security severity
/// - High: High security severity
/// 
/// Inherits from BaseTypeValue for value/display pair representation.
/// Used in security analysis and vulnerability reporting.
/// </summary>
public sealed class SecuritySeverity : BaseTypeValue
{
    public SecuritySeverity(string value, string display) : base(value, display) { }
    
    public static readonly SecuritySeverity Low    = new("low","Low");
    public static readonly SecuritySeverity Medium = new("med","Medium");
    public static readonly SecuritySeverity High   = new("high","High");
}