using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexo.Certification.Physical.Resolution.Http;

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

    public static byte[] Serialize(PhysicalAtomCertificate certificate) =>
        JsonSerializer.SerializeToUtf8Bytes(certificate, Options);
}
