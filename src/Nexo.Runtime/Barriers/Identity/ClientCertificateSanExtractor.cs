#if NET8_0_OR_GREATER
using System.Security.Cryptography.X509Certificates;

namespace Nexo.Runtime.Barriers.Identity;

/// <summary>Extracts DNS subject alternative names from client X.509 certificates for PKI barrier resolution.</summary>
internal static class ClientCertificateSanExtractor
{
    internal static void ExtractDnsSans(X509Certificate2 certificate, ICollection<string> certSans)
    {
        if (certificate is null)
            return;

        ExtractDnsSans(certificate.Extensions, certSans);
    }

    internal static void ExtractDnsSans(IEnumerable<X509Extension> extensions, ICollection<string> certSans)
    {
        try
        {
            foreach (var ext in extensions)
            {
                if (ext is X509SubjectAlternativeNameExtension sanExt)
                {
                    foreach (var dns in sanExt.EnumerateDnsNames())
                        certSans.Add(dns);
                }
            }
        }
        catch
        {
            // SAN parsing is best-effort
        }
    }
}
#endif
