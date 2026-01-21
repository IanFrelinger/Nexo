namespace Nexo.GeoVector.Models;

/// <summary>
/// A set of vector features.
/// </summary>
public sealed class GeoFeatureSet
{
    public IReadOnlyList<GeoFeature> Features { get; }

    public GeoFeatureSet(IReadOnlyList<GeoFeature> features)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(features);
#else
        if (features is null) throw new ArgumentNullException(nameof(features));
#endif
        Features = features;
    }
}

