namespace Nexo.VisualVerification;

/// <summary>Content-addressed frame byte storage.</summary>
public sealed class FrameContentStore
{
    private readonly string _rootDirectory;

    public FrameContentStore(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new ArgumentException("Root directory is required.", nameof(rootDirectory));

        _rootDirectory = rootDirectory;
        Directory.CreateDirectory(_rootDirectory);
    }

    public string Store(ReadOnlySpan<byte> frameBytes)
    {
        var hash = ContentSha256.ComputeHex(frameBytes);
        var path = Path.Combine(_rootDirectory, $"{hash}.raw");
        if (!File.Exists(path))
            File.WriteAllBytes(path, frameBytes.ToArray());
        return path;
    }

    public byte[] Load(string storagePath)
    {
        if (!File.Exists(storagePath))
            throw new FileNotFoundException("Frame bytes not found.", storagePath);

        return File.ReadAllBytes(storagePath);
    }

    public byte[] LoadByHash(string sha256Hex)
    {
        var path = Path.Combine(_rootDirectory, $"{sha256Hex}.raw");
        return Load(path);
    }
}
