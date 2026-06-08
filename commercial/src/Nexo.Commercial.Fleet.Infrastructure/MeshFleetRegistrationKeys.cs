using System.Security.Cryptography;
using System.Text;

namespace Nexo.Commercial.Fleet.Infrastructure;

public static class MeshFleetRegistrationKeys
{
    public static string? Fingerprint(string? peerRegistrationKey)
    {
        if (string.IsNullOrWhiteSpace(peerRegistrationKey))
            return null;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(peerRegistrationKey.Trim()));
        return Convert.ToHexString(hash)[..16];
    }

    public static bool IsDistinctFromDirectorKey(string peerRegistrationKey, string? directorApiKey)
    {
        if (string.IsNullOrWhiteSpace(directorApiKey))
            return true;
        return !string.Equals(peerRegistrationKey.Trim(), directorApiKey.Trim(), StringComparison.Ordinal);
    }
}
