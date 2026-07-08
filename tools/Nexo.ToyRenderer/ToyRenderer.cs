namespace Nexo.ToyRenderer;

/// <summary>Software renderer for the deterministic toy scene.</summary>
public sealed class ToyRenderer
{
    public void Render(ToyScene scene, Span<byte> rgba8888, int width, int height)
    {
        var expected = checked(width * height * 4);
        if (rgba8888.Length != expected)
            throw new ArgumentException($"Expected {expected} bytes, got {rgba8888.Length}.", nameof(rgba8888));

        rgba8888.Clear();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = ((y * width) + x) * 4;
                rgba8888[offset] = 16;
                rgba8888[offset + 1] = 20;
                rgba8888[offset + 2] = 32;
                rgba8888[offset + 3] = 255;
            }
        }

        foreach (var entity in scene.Entities.Where(e => e.Alive))
        {
            var px = (int)(entity.X * (width - 8));
            var py = (int)(entity.Y * (height - 8));
            DrawSquare(rgba8888, width, height, px, py, 8, entity.R, entity.G, entity.B);
        }
    }

  private static void DrawSquare(
    Span<byte> rgba8888,
    int width,
    int height,
    int x,
    int y,
    int size,
    byte r,
    byte g,
    byte b)
  {
    for (var dy = 0; dy < size; dy++)
    {
      var row = y + dy;
      if (row < 0 || row >= height)
        continue;

      for (var dx = 0; dx < size; dx++)
      {
        var col = x + dx;
        if (col < 0 || col >= width)
          continue;

        var offset = ((row * width) + col) * 4;
        rgba8888[offset] = r;
        rgba8888[offset + 1] = g;
        rgba8888[offset + 2] = b;
        rgba8888[offset + 3] = 255;
      }
    }
  }
}
