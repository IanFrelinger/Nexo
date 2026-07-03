using System.Text;

namespace Nexo.Certification.Physical;

internal static class EncodingCompat
{
    internal static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);
}
