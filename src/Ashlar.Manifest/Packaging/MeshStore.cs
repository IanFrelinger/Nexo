using System.Security.Cryptography;
using System.Text;

namespace Ashlar.Manifest.Packaging;

/// <summary>
/// The mesh store: a directory of certified <c>.ashpkg</c> files that peers pull from. This is
/// the kernel half of sharing — resolving WHERE packages are published, and placing a verified
/// package THERE — so every producer (the <c>pkg</c> verbs, a self-extend cycle's auto-share)
/// leaves through one door with one rule: nothing lands in a store without verifying first.
///
/// <para>Deliberately transport-naive: a store is a directory, and "publish" is a file write.
/// Anything that can sync a directory (a shared drive, rsync, an artifact bucket) is thereby a
/// mesh transport, and the trust model never depends on it — receivers verify each package
/// intrinsically and run it through their own gate regardless of how the bytes arrived.</para>
/// </summary>
public static class MeshStore
{
    /// <summary>
    /// Resolves the published-package directory: an explicit directory wins; else
    /// <c>$ASHLAR_MESH_DIR/published</c>; else <c>~/.ashlar/mesh/published</c>.
    /// </summary>
    public static string Resolve(string? explicitDir = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitDir))
        {
            return Path.GetFullPath(explicitDir);
        }
        if (Environment.GetEnvironmentVariable("ASHLAR_MESH_DIR") is { } env && !string.IsNullOrWhiteSpace(env))
        {
            return Path.Combine(Path.GetFullPath(env), "published");
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ashlar", "mesh", "published");
    }

    /// <summary>
    /// Verifies a sealed package and places it in the store, returning the destination path.
    /// Named by content hash so identical packages dedupe across republishes, prefixed by the
    /// proposal id so a human browsing the store can read what is there. Throws
    /// <see cref="InvalidOperationException"/> with the verifier's reason when the package does
    /// not verify — the mesh carries certified packages only, so a forged one is refused at the
    /// source rather than propagated to every peer.
    /// </summary>
    public static string Publish(string storeDir, string packageJson)
    {
        if (!ExtensionPackaging.TryOpen(packageJson, out var pkg, out var reason))
        {
            throw new InvalidOperationException(reason);
        }
        Directory.CreateDirectory(storeDir);
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(packageJson)))[..12].ToLowerInvariant();
        var dest = Path.Combine(storeDir, $"{Safe(pkg!.Record.Proposal.Id)}-{sha}.ashpkg");
        // Content-hash naming makes presence proof of identity: an existing file IS this package,
        // so a re-share is a true no-op rather than a rewrite racing a peer's concurrent pull.
        if (File.Exists(dest))
        {
            return dest;
        }
        // Write-then-move, same discipline as GateStore and OperatorKey: a torn write must never
        // leave a half-package under its final name for a peer to pull and refuse as forged.
        var tmp = dest + ".tmp";
        File.WriteAllText(tmp, packageJson);
        File.Move(tmp, dest, overwrite: true);
        return dest;
    }

    private static string Safe(string id)
    {
        var chars = id.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray();
        return chars.Length > 0 ? new string(chars) : "pkg";
    }
}
