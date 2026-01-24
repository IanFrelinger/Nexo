using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nexo.Adapters.GeoTerrain.Processing;

public static class ProceduralTextureGenerator
{
    public static byte[] MakeCheckerPng(int size, byte ar, byte ag, byte ab, byte aa, byte br, byte bg, byte bb, byte ba, int cell)
    {
        using var img = new Image<Rgba32>(size, size);
        img.Mutate(ctx =>
        {
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var on = ((x / cell) + (y / cell)) % 2 == 0;
                    img[x, y] = on ? new Rgba32(ar, ag, ab, aa) : new Rgba32(br, bg, bb, ba);
                }
            }
        });
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    public static byte[] MakeRoadAsphaltPng(int size)
    {
        using var img = new Image<Rgba32>(size, size);
        img.Mutate(ctx =>
        {
            // Base asphalt noise-ish.
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var v = 40 + ((x * 17 + y * 31) % 25);
                    img[x, y] = new Rgba32((byte)v, (byte)v, (byte)v, 255);
                }
            }

            // Center dashed line.
            var lineW = Math.Max(2, size / 32);
            var center = size / 2;
            for (var y = 0; y < size; y++)
            {
                var dash = ((y / (size / 16)) % 2) == 0;
                if (!dash) continue;
                for (var x = center - lineW; x <= center + lineW; x++)
                {
                    if (x < 0 || x >= size) continue;
                    img[x, y] = new Rgba32(240, 220, 40, 255);
                }
            }
        });
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }
}

