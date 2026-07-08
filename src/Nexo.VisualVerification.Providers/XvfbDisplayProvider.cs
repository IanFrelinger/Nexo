using System.Diagnostics;

namespace Nexo.VisualVerification.Providers;

/// <summary>Spawns Xvfb and captures frames via xwd conversion. Integration-only.</summary>
public sealed class XvfbDisplayProvider : IDisplayProvider
{
    private Process? _xvfbProcess;
    private DisplayConfig _config = new(0, 0, PixelFormats.Rgba8888, "xvfb");
    private string _display = ":99";
    private int _width;
    private int _height;

    public static bool IsAvailable()
    {
        try
        {
            return !string.IsNullOrWhiteSpace(Which("Xvfb"))
                   && !string.IsNullOrWhiteSpace(Which("xwd"))
                   && !string.IsNullOrWhiteSpace(Which("convert"));
        }
        catch
        {
            return false;
        }
    }

    public async Task<DisplayConfig> StartAsync(int width, int height, CancellationToken ct)
    {
        if (!IsAvailable())
            throw new InvalidOperationException("Xvfb, xwd, or ImageMagick convert is not available.");

        _width = width;
        _height = height;
        _display = $":{Random.Shared.Next(100, 200)}";

        var screen = $"{width}x{height}x24";
        _xvfbProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "Xvfb",
            Arguments = $"{_display} -screen 0 {screen} -ac +extension GLX +render -noreset",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Failed to start Xvfb.");

        await Task.Delay(250, ct).ConfigureAwait(false);
        _config = new DisplayConfig(width, height, PixelFormats.Rgba8888, "xvfb");
        return _config;
    }

    public async Task<byte[]> CaptureFrameAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var tempXwd = Path.Combine(Path.GetTempPath(), $"nexo-eye-{Guid.NewGuid():N}.xwd");
        var tempPng = Path.ChangeExtension(tempXwd, ".png");

        try
        {
            await RunProcessAsync("xwd", $"-display {_display} -root -out {tempXwd}", ct).ConfigureAwait(false);
            await RunProcessAsync("convert", $"{tempXwd} {tempPng}", ct).ConfigureAwait(false);
            return await RgbaLoader.LoadPngAsRgba8888Async(tempPng, _width, _height, ct).ConfigureAwait(false);
        }
        finally
        {
            if (File.Exists(tempXwd))
                File.Delete(tempXwd);
            if (File.Exists(tempPng))
                File.Delete(tempPng);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_xvfbProcess is null)
            return;

        try
        {
            if (!_xvfbProcess.HasExited)
                _xvfbProcess.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort cleanup.
        }
        finally
        {
            _xvfbProcess.Dispose();
            _xvfbProcess = null;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public string Display => _display;

    private static async Task RunProcessAsync(string fileName, string arguments, CancellationToken ct)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException($"Failed to start {fileName}.");

        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"{fileName} failed with exit code {process.ExitCode}: {stderr}");
        }
    }

    private static string? Which(string command)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sh",
            Arguments = $"-c \"command -v {command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        if (process is null)
            return null;

        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return process.ExitCode == 0 ? output : null;
    }
}

internal static class RgbaLoader
{
    public static async Task<byte[]> LoadPngAsRgba8888Async(
        string pngPath,
        int expectedWidth,
        int expectedHeight,
        CancellationToken ct)
    {
        await using var stream = File.OpenRead(pngPath);
        using var reader = new BinaryReader(stream);

        // Minimal PNG loader for integration tests: requires truecolor RGBA/RGB PNG from ImageMagick.
        if (reader.ReadUInt32() != 0x89504E47)
            throw new InvalidDataException("Not a PNG file.");

        stream.Seek(0, SeekOrigin.Begin);
        var bytes = await File.ReadAllBytesAsync(pngPath, ct).ConfigureAwait(false);
        return PngDecoder.DecodeToRgba8888(bytes, expectedWidth, expectedHeight);
    }
}

/// <summary>Minimal PNG decoder for Xvfb integration captures.</summary>
internal static class PngDecoder
{
    public static byte[] DecodeToRgba8888(byte[] pngBytes, int expectedWidth, int expectedHeight)
    {
        // Use built-in System.Drawing if available is heavy; implement scan for IDAT chunks with zlib.
        // For integration path we rely on ImageMagick producing uncompressed-friendly PNG and parse via simple path.
        using var ms = new MemoryStream(pngBytes);
        using var reader = new BinaryReader(ms);
        reader.ReadUInt32(); // signature part 1
        reader.ReadUInt32(); // signature part 2

        int width = 0;
        int height = 0;
        byte colorType = 0;
        var idatChunks = new List<byte[]>();

        while (ms.Position < ms.Length)
        {
            var length = ReadInt32BigEndian(reader);
            var type = new string(reader.ReadChars(4));
            var data = reader.ReadBytes(length);
            reader.ReadUInt32(); // crc

            if (type == "IHDR")
            {
                width = ReadInt32BigEndian(new BinaryReader(new MemoryStream(data)));
                height = ReadInt32BigEndian(new BinaryReader(new MemoryStream(data.Skip(4).ToArray())));
                colorType = data[9];
            }
            else if (type == "IDAT")
            {
                idatChunks.Add(data);
            }
        }

        if (width != expectedWidth || height != expectedHeight)
            throw new InvalidDataException($"PNG size {width}x{height} does not match expected {expectedWidth}x{expectedHeight}.");

        var compressed = idatChunks.SelectMany(c => c).ToArray();
        var raw = ZlibInflate(compressed);
        return ConvertToRgba8888(raw, width, height, colorType);
    }

    private static byte[] ConvertToRgba8888(byte[] raw, int width, int height, byte colorType)
    {
        var stride = 1 + (width * BytesPerPixel(colorType));
        var output = new byte[width * height * 4];
        var offset = 0;

        for (var y = 0; y < height; y++)
        {
            var rowStart = y * stride;
            var filter = raw[rowStart];
            if (filter != 0)
                throw new NotSupportedException($"PNG filter type {filter} is not supported.");

            for (var x = 0; x < width; x++)
            {
                var pixelStart = rowStart + 1 + (x * BytesPerPixel(colorType));
                byte r, g, b, a;
                switch (colorType)
                {
                    case 2:
                        r = raw[pixelStart];
                        g = raw[pixelStart + 1];
                        b = raw[pixelStart + 2];
                        a = 255;
                        break;
                    case 6:
                        r = raw[pixelStart];
                        g = raw[pixelStart + 1];
                        b = raw[pixelStart + 2];
                        a = raw[pixelStart + 3];
                        break;
                    default:
                        throw new NotSupportedException($"PNG color type {colorType} is not supported.");
                }

                output[offset++] = r;
                output[offset++] = g;
                output[offset++] = b;
                output[offset++] = a;
            }
        }

        return output;
    }

    private static int BytesPerPixel(byte colorType) => colorType switch
    {
        2 => 3,
        6 => 4,
        _ => throw new NotSupportedException($"PNG color type {colorType} is not supported.")
    };

    private static int ReadInt32BigEndian(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    private static byte[] ZlibInflate(byte[] data)
    {
        // Skip zlib header (2 bytes) and use DeflateStream on remainder minus adler32.
        using var input = new MemoryStream(data, 2, data.Length - 6);
        using var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }
}
