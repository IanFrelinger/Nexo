namespace Nexo.VisualVerification;

/// <summary>
/// Difference hash (dHash) perceptual fingerprint for RGBA8888 frames.
/// Produces a 64-bit lowercase hex string (16 chars).
/// </summary>
public static class PerceptualHash
{
    public const int DefaultWidth = 9;
    public const int DefaultHeight = 8;

    /// <summary>Default Hamming-distance epsilon for perceptual match tier.</summary>
    public const int DefaultEpsilon = 5;

    public static string ComputeDHashHex(ReadOnlySpan<byte> rgba8888, int width, int height, string pixelFormat)
    {
        if (!string.Equals(pixelFormat, PixelFormats.Rgba8888, StringComparison.Ordinal))
            throw new ArgumentException($"Unsupported pixel format: {pixelFormat}", nameof(pixelFormat));

        var expectedLength = checked(width * height * 4);
        if (rgba8888.Length != expectedLength)
            throw new ArgumentException(
                $"Expected {expectedLength} bytes for {width}x{height} RGBA8888, got {rgba8888.Length}.",
                nameof(rgba8888));

        var gray = DownsampleGrayscale(rgba8888, width, height, DefaultWidth, DefaultHeight);
        ulong hash = 0;
        var bit = 0;

        for (var y = 0; y < DefaultHeight; y++)
        {
            for (var x = 0; x < DefaultWidth - 1; x++)
            {
                var left = gray[(y * DefaultWidth) + x];
                var right = gray[(y * DefaultWidth) + x + 1];
                if (left > right)
                    hash |= 1UL << bit;
                bit++;
            }
        }

        return hash.ToString("x16");
    }

    public static int HammingDistance(string leftHex, string rightHex)
    {
        if (leftHex.Length != 16 || rightHex.Length != 16)
            throw new ArgumentException("dHash hex strings must be 16 characters.");

        var left = Convert.ToUInt64(leftHex, 16);
        var right = Convert.ToUInt64(rightHex, 16);
        return System.Numerics.BitOperations.PopCount(left ^ right);
    }

    private static byte[] DownsampleGrayscale(
        ReadOnlySpan<byte> rgba8888,
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        int targetHeight)
    {
        var result = new byte[targetWidth * targetHeight];
        for (var y = 0; y < targetHeight; y++)
        {
            var sourceY = (y * sourceHeight) / targetHeight;
            for (var x = 0; x < targetWidth; x++)
            {
                var sourceX = (x * sourceWidth) / targetWidth;
                var offset = ((sourceY * sourceWidth) + sourceX) * 4;
                var r = rgba8888[offset];
                var g = rgba8888[offset + 1];
                var b = rgba8888[offset + 2];
                result[(y * targetWidth) + x] = (byte)((r * 299 + g * 587 + b * 114) / 1000);
            }
        }

        return result;
    }
}

/// <summary>Supported pixel format identifiers.</summary>
public static class PixelFormats
{
    public const string Rgba8888 = "RGBA8888";
}
