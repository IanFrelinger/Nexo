using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// TLS/mTLS for the federated mesh — the private-fleet option. Transport security is DEFENCE IN DEPTH,
/// not a replacement for the package seal: the Ed25519 trust root still decides what runs. What mTLS
/// adds is access control and confidentiality on the wire — only fleet members holding a cert the
/// fleet CA signed can even list or download a node's published packages, and the bytes are encrypted.
///
/// <para>Certs are PEM (cert + key files, a CA bundle) — the format Caddy, step-ca, and openssl emit —
/// so it drops into an existing PKI. Loading is fail-loud: a configured-but-unloadable cert throws at
/// startup rather than silently serving plaintext. Chain validation uses CustomRootTrust against the
/// fleet CA only (not the machine store), so a public CA cannot mint a fleet identity.</para>
/// </summary>
public static class MeshTls
{
    /// <summary>
    /// Loads a PEM cert+key into a Kestrel/HttpClient-usable certificate. Re-exports through PKCS#12 so
    /// the private key is materialized in a key container every platform's TLS stack accepts (the raw
    /// ephemeral key from a PEM load is rejected by SChannel on Windows).
    /// </summary>
    public static X509Certificate2 LoadCertWithKey(string certPemPath, string keyPemPath)
    {
        using var fromPem = X509Certificate2.CreateFromPemFile(certPemPath, keyPemPath);
        return X509CertificateLoader.LoadPkcs12(fromPem.Export(X509ContentType.Pkcs12), password: null);
    }

    /// <summary>Loads one or more CA certificates from a PEM bundle.</summary>
    public static X509Certificate2Collection LoadCaBundle(string caPemPath)
    {
        var bundle = new X509Certificate2Collection();
        bundle.ImportFromPemFile(caPemPath);
        if (bundle.Count == 0)
        {
            throw new InvalidOperationException($"CA bundle '{caPemPath}' contained no certificates.");
        }
        return bundle;
    }

    /// <summary>
    /// A validator that trusts a leaf certificate ONLY if it chains to the given fleet CA(s). Used both
    /// server-side (validate the client's cert in mTLS) and client-side (validate the server's cert).
    /// CustomRootTrust means the machine's own trust store is not consulted — a fleet identity must be
    /// signed by the fleet CA, nothing else.
    /// </summary>
    public static bool ChainsToCa(X509Certificate2? leaf, X509Certificate2Collection ca)
    {
        if (leaf is null)
        {
            return false;
        }
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.CustomTrustStore.AddRange(ca);
        return chain.Build(leaf);
    }

    /// <summary>Converts the base certificate a validation callback hands back into an X509Certificate2.</summary>
    public static X509Certificate2? AsCert2(System.Security.Cryptography.X509Certificates.X509Certificate? cert) =>
        cert switch
        {
            null => null,
            X509Certificate2 c2 => c2,
            _ => X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Cert)),
        };
}
