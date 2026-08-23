using System.Text;

namespace Ashlar.Certification.Physical;

/// <summary>Encoding compat.</summary>
internal static class EncodingCompat
{
    internal static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);
}
