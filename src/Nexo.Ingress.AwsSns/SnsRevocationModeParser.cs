using System.Security.Cryptography.X509Certificates;

namespace Nexo.Ingress.AwsSns;

/// <summary>Sns revocation mode parser.</summary>
public static class SnsRevocationModeParser
{
    /// <summary>Parse.</summary>
    /// <param name="value">Value.</param>
    public static X509RevocationMode Parse(string? value) =>
        value?.Trim().ToUpperInvariant() switch
        {
            "ONLINE" => X509RevocationMode.Online,
            "OFFLINE" => X509RevocationMode.Offline,
            _ => X509RevocationMode.NoCheck,
        };
}
