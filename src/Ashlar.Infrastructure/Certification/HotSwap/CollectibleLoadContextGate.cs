namespace Ashlar.Infrastructure.Certification.HotSwap;

/// <summary>
/// Process-wide gate serializing every collectible <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
/// transition (create/load, unload, forced collection) across subsystems.
/// </summary>
/// <remarks>
/// Finalizing overlapping collectible <c>LoaderAllocator</c>s crashes the runtime
/// (<c>LoaderAllocatorScout.Finalize</c> / <c>0x80131506</c>) and takes the whole process
/// down rather than failing the caller. The mutation engine learned this first, back when it
/// loaded mutants into collectible contexts in-process (it now replays them in a child
/// process and loads nothing); the hot-swap host performs the same class of transitions, so
/// every subsystem that still does must share this ONE gate.
/// A context that is merely alive and serving does not hold the gate; only transitions do.
/// </remarks>
internal static class CollectibleLoadContextGate
{
    /// <summary>The single process-wide transition gate.</summary>
    public static SemaphoreSlim Instance { get; } = new(1, 1);
}
