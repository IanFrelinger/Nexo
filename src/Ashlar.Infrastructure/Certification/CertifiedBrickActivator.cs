using System.Reflection;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The single certify-from-disk activation site. Discovery is metadata-only; this type
/// is the frozen inventory entry for <c>Assembly.Load</c> + <c>Activator.CreateInstance</c>
/// on the loader path (B5).
/// </summary>
public static class CertifiedBrickActivator
{
    /// <summary>Loads <paramref name="artifact"/> bytes and constructs the discovered brick type.</summary>
    public static DomainBrick Activate(GateEmittedArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        var actual = BrickContentHasher.ComputeSha256(artifact.AssemblyBytes);
        if (!string.Equals(actual, artifact.AssemblySha256, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "gate-emitted artifact hash does not match the supplied bytes.");
        IlImportFence.Inspect(artifact.AssemblyBytes);
        var assembly = Assembly.Load(artifact.AssemblyBytes);
        var type = assembly.GetType(artifact.BrickTypeName, throwOnError: false)
            ?? throw new InvalidOperationException(
                $"gate-emitted artifact type '{artifact.BrickTypeName}' was not found after load");
        if (!typeof(DomainBrick).IsAssignableFrom(type) || type.IsAbstract)
            throw new InvalidOperationException(
                $"gate-emitted artifact type '{artifact.BrickTypeName}' is not a concrete DomainBrick");

        return (DomainBrick)Activator.CreateInstance(type)!;
    }
}
