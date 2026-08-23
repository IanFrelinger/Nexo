namespace Ashlar.Commercial.GameDomain.Aesthetics;

/// <summary>
/// A single level-of-detail tier within an <see cref="AestheticPack"/>.
/// </summary>
/// <param name="Level">
/// Zero-based LOD index. <c>0</c> is the highest-detail tier rendered at close range.
/// </param>
/// <param name="DetailFactor">
/// Normalised detail multiplier in <c>[0, 1]</c>.
/// Interpretation varies by geometry strategy:
/// <list type="bullet">
///   <item><c>voxel</c> — voxel resolution relative to max grid density.</item>
///   <item><c>low_poly</c> — triangle budget as a fraction of the base mesh.</item>
///   <item><c>pixel_art</c> — sprite resolution multiplier.</item>
///   <item><c>pbr</c> — texture mip level / mesh decimation factor.</item>
///   <item><c>wireframe</c> — edge density ratio.</item>
///   <item><c>sketch</c> — stroke density / hatching frequency.</item>
/// </list>
/// </param>
public sealed record LodLevel(
    int Level = 0,
    double DetailFactor = 1.0);
