namespace Nexo.Certification.Physical;

/// <summary>
/// Determines which optional certificate fields are load-bearing for verification.
/// </summary>
public enum BindingScope
{
    Design = 0,
    Instance = 1,
    Batch = 2
}
