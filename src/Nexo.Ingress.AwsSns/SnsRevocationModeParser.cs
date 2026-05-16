using System.Security.Cryptography.X509Certificates;

namespace Nexo.Ingress.AwsSns;

public static class SnsRevocationModeParser
{
    public static X509RevocationMode Parse(string? value) =>
        value?.Trim().ToUpperInvariant() switch
        {
            "ONLINE" => X509RevocationMode.Online,
            "OFFLINE" => X509RevocationMode.Offline,
            _ => X509RevocationMode.NoCheck,
        };
}
