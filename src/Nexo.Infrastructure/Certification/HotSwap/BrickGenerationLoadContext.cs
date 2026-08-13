using System.Reflection;
using System.Runtime.Loader;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>
/// Collectible load context owning one generation of the certified brick set.
/// </summary>
/// <remarks>
/// Mirrors <see cref="MutantAssemblyLoadContext"/>: the context is named so a leaked or
/// stuck generation is attributable in <see cref="AssemblyLoadContext.All"/> (an unnamed
/// context shows up as <c>name=&lt;null&gt;</c>, which is impossible to diagnose), and
/// <see cref="Load"/> returns null so every dependency — including the brick contract
/// types — resolves through the default context. That fallback is what makes the host's
/// <c>(Brick)</c> cast valid across the ALC boundary: the loaded assembly binds to the
/// host's own <c>Nexo.Brick.Contracts</c> rather than a private copy.
/// </remarks>
internal sealed class BrickGenerationLoadContext : AssemblyLoadContext
{
    /// <summary>Creates a collectible, named generation context.</summary>
    public BrickGenerationLoadContext(string name)
        : base(isCollectible: true, name: name)
    {
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName) => null;
}
