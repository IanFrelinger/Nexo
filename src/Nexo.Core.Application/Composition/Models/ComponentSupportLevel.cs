namespace Nexo.Core.Application.Composition.Models;

/// <summary>Maturity level of a registered composition component.</summary>
public enum ComponentSupportLevel
{
    /// <summary>Production-ready and supported for general use.</summary>
    Stable = 0,

    /// <summary>Available for early adopters; behavior may change.</summary>
    Experimental = 1,

    /// <summary>Stub or scaffold component not yet implemented.</summary>
    Placeholder = 2,
}
