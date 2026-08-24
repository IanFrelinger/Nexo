using System.Security.Cryptography;
using NSec.Cryptography;

namespace Ashlar.Manifest.Signing;

/// <summary>
/// SPEC-006 v1: the single local operator Ed25519 keypair.
///
/// <para><c>Generate</c> writes <c>operator.key</c> (raw 32-byte seed, base64, owner-only on
/// POSIX) and <c>operator.pub</c> under the key directory — <c>ASHLAR_KEY_DIR</c> or
/// <c>~/.ashlar/keys</c>. <c>TryLoad</c> returns null when no key exists: signing is
/// presence-activated, and absence degrades to today's honest unsigned behaviour exactly
/// (rule S-2). The private key never appears inside a repository, bundle, or output.</para>
/// </summary>
public static class OperatorKey
{
    private static readonly SignatureAlgorithm Alg = SignatureAlgorithm.Ed25519;

    /// <summary>Resolves the key directory: <c>ASHLAR_KEY_DIR</c>, else <c>~/.ashlar/keys</c>.</summary>
    public static string ResolveKeyDir() =>
        Environment.GetEnvironmentVariable("ASHLAR_KEY_DIR") is { Length: > 0 } dir
            ? Path.GetFullPath(dir)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ashlar", "keys");

    /// <summary>
    /// Generates a new operator keypair. Refuses to overwrite an existing key unless
    /// <paramref name="rotate"/>; rotation moves the old PUBLIC key to <c>trusted/</c> so
    /// old records still verify (revocation is v2, and v1 says so).
    /// </summary>
    public static SigningIdentity Generate(string? keyDir = null, bool rotate = false)
    {
        var dir = keyDir ?? ResolveKeyDir();
        Directory.CreateDirectory(dir);
        var privPath = Path.Combine(dir, "operator.key");
        var pubPath = Path.Combine(dir, "operator.pub");

        if (File.Exists(privPath))
        {
            if (!rotate)
            {
                throw new InvalidOperationException(
                    $"An operator key already exists at {privPath}. Use --rotate to replace it; "
                    + "the old public key is kept in trusted/ so existing records still verify.");
            }
            var trustedDir = Path.Combine(dir, "trusted");
            Directory.CreateDirectory(trustedDir);
            if (File.Exists(pubPath))
            {
                var oldPub = File.ReadAllText(pubPath).Trim();
                var oldPrint = Fingerprint(Convert.FromBase64String(oldPub)).Replace(':', '-');
                File.WriteAllText(Path.Combine(trustedDir, oldPrint + ".pub"), oldPub);
            }
        }

        using var key = Key.Create(Alg, new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport,
        });
        var seed = key.Export(KeyBlobFormat.RawPrivateKey);
        var pub = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);

        // Write each file crash-safely (temp then rename), and set owner-only permissions on
        // the seed's temp BEFORE it is moved into place, so the private key never exists at
        // its final path with default permissions even for an instant. The two files are not
        // moved as one atomic unit — a crash between the moves can still strand a new seed
        // beside an old pub — but that mismatch is caught loudly at load by the
        // SigningIdentity invariant below, rather than silently producing unverifiable records.
        WriteFileAtomic(privPath, Convert.ToBase64String(seed), ownerOnly: true);
        WriteFileAtomic(pubPath, Convert.ToBase64String(pub), ownerOnly: false);
        // Windows: the profile directory's ACL is the v1 protection; DPAPI is v2 (SPEC-006 §3).

        return new SigningIdentity(Convert.ToBase64String(pub), seed);
    }

    /// <summary>Loads the operator identity, or null when no key exists (S-2 degrade).</summary>
    public static SigningIdentity? TryLoad(string? keyDir = null)
    {
        var dir = keyDir ?? ResolveKeyDir();
        var privPath = Path.Combine(dir, "operator.key");
        var pubPath = Path.Combine(dir, "operator.pub");
        if (!File.Exists(privPath) || !File.Exists(pubPath))
        {
            return null;
        }
        return new SigningIdentity(
            File.ReadAllText(pubPath).Trim(),
            Convert.FromBase64String(File.ReadAllText(privPath).Trim()));
    }

    /// <summary>SPEC-006 §3 fingerprint: <c>ed25519:</c> + first 16 lowercase hex chars of
    /// SHA-256 over the raw public key.</summary>
    public static string Fingerprint(byte[] rawPublicKey) =>
        "ed25519:" + Convert.ToHexString(SHA256.HashData(rawPublicKey))[..16].ToLowerInvariant();

    /// <summary>Verifies a signature made by <see cref="SigningIdentity.Sign"/>.</summary>
    public static bool Verify(string publicKeyBase64, byte[] data, string signatureBase64)
    {
        try
        {
            var pub = PublicKey.Import(Alg, Convert.FromBase64String(publicKeyBase64), KeyBlobFormat.RawPublicKey);
            return Alg.Verify(pub, data, Convert.FromBase64String(signatureBase64));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Writes a file crash-safely via a temp-then-rename, applying owner-only Unix
    /// permissions to the temp before the move so a private key is never briefly world-readable
    /// at its final path.</summary>
    private static void WriteFileAtomic(string path, string content, bool ownerOnly)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        if (ownerOnly && !OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(tmp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        File.Move(tmp, path, overwrite: true);
    }
}

/// <summary>A loaded operator identity: the public half, its fingerprint, and the ability
/// to sign. Holds the seed in memory only for the process lifetime.</summary>
public sealed class SigningIdentity
{
    private readonly byte[] _seed;

    /// <summary>Base64 raw 32-byte public key.</summary>
    public string PublicKeyBase64 { get; }

    /// <summary>Display fingerprint, e.g. <c>ed25519:9f3c…</c> without the ellipsis.</summary>
    public string Fingerprint { get; }

    internal SigningIdentity(string publicKeyBase64, byte[] seed)
    {
        // Invariant: a SigningIdentity NEVER exists with a public half its seed does not
        // produce. TryLoad builds this from two independently-written files, and a crash
        // between those writes — or during --rotate, which destroys the old seed — can leave
        // operator.key and operator.pub disagreeing. Catch it HERE, loudly, rather than
        // signing every record with one key while advertising another, which would stamp
        // honest verdicts with signatures that can never verify and poison the store on the
        // next fail-closed read. Ed25519's public key is a deterministic function of the seed,
        // so this is an exact check; the public key is not secret, so an ordinal compare is fine.
        using (var check = Key.Import(SignatureAlgorithm.Ed25519, seed, KeyBlobFormat.RawPrivateKey))
        {
            var derived = Convert.ToBase64String(check.PublicKey.Export(KeyBlobFormat.RawPublicKey));
            if (!string.Equals(derived, publicKeyBase64, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "operator.key and operator.pub disagree: the private seed does not derive the stored public "
                    + "key. This is the fingerprint of a crash during key rotation. Re-run key generation with "
                    + "rotation to write a fresh, consistent keypair; records signed by earlier keys keep verifying "
                    + "via the public keys retained under trusted/.");
            }
        }

        PublicKeyBase64 = publicKeyBase64;
        _seed = seed;
        Fingerprint = OperatorKey.Fingerprint(Convert.FromBase64String(publicKeyBase64));
    }

    /// <summary>Signs canonical bytes; returns base64.</summary>
    public string Sign(byte[] data)
    {
        using var key = Key.Import(SignatureAlgorithm.Ed25519, _seed, KeyBlobFormat.RawPrivateKey);
        return Convert.ToBase64String(SignatureAlgorithm.Ed25519.Sign(key, data));
    }
}
