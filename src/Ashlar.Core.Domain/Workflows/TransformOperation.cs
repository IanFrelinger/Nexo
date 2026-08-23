namespace Ashlar.Core.Domain.Workflows;

/// <summary>Data transformation operation applied by a <see cref="TransformNode"/>.</summary>
public enum TransformOperation
{
    /// <summary>Map each element through an expression.</summary>
    Map,

    /// <summary>Filter elements matching a predicate expression.</summary>
    Filter,

    /// <summary>Reduce a collection to a single aggregated value.</summary>
    Reduce,

    /// <summary>Sort elements by an expression.</summary>
    Sort,

    /// <summary>Group elements by a key expression.</summary>
    GroupBy,

    /// <summary>Merge multiple input streams into one.</summary>
    Merge
}
