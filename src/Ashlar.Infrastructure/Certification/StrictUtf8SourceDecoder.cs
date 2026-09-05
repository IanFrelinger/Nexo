using System.Text;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Strict UTF-8 decode for every hashed source input. UTF-16 BOMs, invalid sequences, and
/// non-UTF-8 encodings are refusals — a mis-decoded source would be a different program
/// than the bytes the author saved.
/// </summary>
public static class StrictUtf8SourceDecoder
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Decodes <paramref name="raw"/> as strict UTF-8.</summary>
    /// <exception cref="InvalidOperationException">When the bytes are not strict UTF-8.</exception>
    public static string Decode(byte[] raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE)
            throw new InvalidOperationException("source is UTF-16LE; the certifier decodes strict UTF-8 only");
        if (raw.Length >= 2 && raw[0] == 0xFE && raw[1] == 0xFF)
            throw new InvalidOperationException("source is UTF-16BE; the certifier decodes strict UTF-8 only");

        try
        {
            return Utf8.GetString(raw);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidOperationException("source is not strict UTF-8; the certifier refuses a guessed encoding", ex);
        }
    }

    /// <summary>Reads a file as strict UTF-8.</summary>
    public static async Task<string> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var raw = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return Decode(raw);
    }
}
