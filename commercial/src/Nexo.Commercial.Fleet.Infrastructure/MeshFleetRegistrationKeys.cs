using System.Security.Cryptography;
using System.Text;

namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>Mesh fleet registration keys.</summary>
public static class MeshFleetRegistrationKeys
{
    /// <summary>Fingerprint operation.</summary>
    public static string? Fingerprint(string? peerRegistrationKey)
    {
        if (string.IsNullOrWhiteSpace(peerRegistrationKey))
            return null;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(peerRegistrationKey.Trim()));
        return Convert.ToHexString(hash)[..16];
    }

    /// <summary>Is distinct from director key operation.</summary>
    public static bool IsDistinctFromDirectorKey(string peerRegistrationKey, string? directorApiKey)
    {
        if (string.IsNullOrWhiteSpace(directorApiKey))
            return true;
        return !string.Equals(peerRegistrationKey.Trim(), directorApiKey.Trim(), StringComparison.Ordinal);
    }
}
