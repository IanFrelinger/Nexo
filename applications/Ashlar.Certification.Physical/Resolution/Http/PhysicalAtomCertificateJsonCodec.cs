using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ashlar.Certification.Physical.Resolution.Http;

/// <summary>Physical atom certificate json codec.</summary>
internal static class PhysicalAtomCertificateJsonCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    static PhysicalAtomCertificateJsonCodec()
    {
        Options.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>Serialize.</summary>
    /// <param name="certificate">Certificate.</param>
    public static byte[] Serialize(PhysicalAtomCertificate certificate) =>
        JsonSerializer.SerializeToUtf8Bytes(certificate, Options);
}
