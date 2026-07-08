namespace Nexo.VisualVerification;

/// <summary>Stored golden frame fingerprints from a prior certified session.</summary>
public sealed record GoldenFrameFingerprint(int Step, string Sha256, string PerceptualHash);

/// <summary>Golden comparison input derived from a certified session record.</summary>
public sealed record GoldenEyeTestRecord(
    string ScenarioHash,
    string BuildArtifactSha256,
    DisplayConfig Display,
    IReadOnlyList<GoldenFrameFingerprint> Frames);

/// <summary>
/// Witness-only golden storage. Target providers must never receive this path or type.
/// </summary>
public sealed class GoldenRecordStore
{
    private readonly string _rootDirectory;

    public GoldenRecordStore(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new ArgumentException("Root directory is required.", nameof(rootDirectory));

        _rootDirectory = rootDirectory;
        Directory.CreateDirectory(_rootDirectory);
    }

    public string RootDirectory => _rootDirectory;

    public void Save(GoldenEyeTestRecord record, string name)
    {
        var path = Path.Combine(_rootDirectory, $"{name}.golden.json");
        var json = GoldenRecordCodec.Serialize(record);
        File.WriteAllText(path, json);
    }

    public GoldenEyeTestRecord Load(string name)
    {
        var path = Path.Combine(_rootDirectory, $"{name}.golden.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("Golden record not found.", path);

        return GoldenRecordCodec.Deserialize(File.ReadAllText(path));
    }
}
