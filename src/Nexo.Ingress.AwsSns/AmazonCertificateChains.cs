using System.Security.Cryptography.X509Certificates;

namespace Nexo.Ingress.AwsSns;

/// <summary>
/// Validates that an SNS signing leaf certificate chains to Amazon public CAs (defense beyond RSA signature checks).
/// </summary>
public static class AmazonCertificateChains
{
    private static readonly X509Certificate2 AmazonRootCa1 = LoadEmbeddedRoot();

    /// <summary>
    /// Returns true when the leaf looks AWS-issued and builds a trusted chain to an Amazon public CA.
    /// </summary>
    /// <param name="leaf">SNS signing certificate parsed from <c>SigningCertURL</c>.</param>
    /// <param name="revocationMode">Passed to <see cref="X509Chain.ChainPolicy"/> (Online can add latency / flakiness).</param>
    public static bool TryValidateSnsSigningCertificate(X509Certificate2 leaf, X509RevocationMode revocationMode)
    {
        if (!LooksAwsIssued(leaf))
            return false;

        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = revocationMode;
        if (chain.Build(leaf))
        {
            using var root = chain.ChainElements[^1].Certificate;
            return SubjectImpliesAmazonPublicCa(root);
        }

        using var custom = new X509Chain();
        custom.ChainPolicy.RevocationMode = revocationMode;
        custom.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        custom.ChainPolicy.CustomTrustStore.Add(new X509Certificate2(AmazonRootCa1.RawData));
        return custom.Build(leaf);
    }

    private static bool LooksAwsIssued(X509Certificate2 cert) =>
        cert.Subject.Contains("Amazon", StringComparison.OrdinalIgnoreCase)
        || cert.Issuer.Contains("Amazon", StringComparison.OrdinalIgnoreCase);

    private static bool SubjectImpliesAmazonPublicCa(X509Certificate2 root) =>
        root.Subject.Contains("Amazon", StringComparison.OrdinalIgnoreCase)
        || root.Subject.Contains("Starfield", StringComparison.OrdinalIgnoreCase);

    private static X509Certificate2 LoadEmbeddedRoot()
    {
        var assembly = typeof(AmazonCertificateChains).Assembly;
        const string resourceName = "Nexo.Ingress.AwsSns.Resources.AmazonRootCA1.pem";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return X509Certificate2.CreateFromPem(reader.ReadToEnd());
    }
}
