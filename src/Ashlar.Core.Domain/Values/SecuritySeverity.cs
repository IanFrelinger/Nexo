namespace Ashlar.Core.Domain.Values;

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
    /// <summary>Creates a security severity value.</summary>
    public SecuritySeverity(string value, string display) : base(value, display) { }

    /// <summary>Low-severity finding.</summary>
    public static readonly SecuritySeverity Low    = new("low","Low");

    /// <summary>Medium-severity finding.</summary>
    public static readonly SecuritySeverity Medium = new("med","Medium");

    /// <summary>High-severity finding.</summary>
    public static readonly SecuritySeverity High   = new("high","High");
}