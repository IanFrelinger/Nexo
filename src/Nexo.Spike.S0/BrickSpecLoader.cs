using System.Text.Json;
using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S0;

public static class BrickSpecLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static async Task<BrickSpec> LoadAsync(string path, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(path);
        var spec = await JsonSerializer.DeserializeAsync<BrickSpec>(stream, JsonOptions, ct)
            .ConfigureAwait(false);
        return spec ?? throw new InvalidOperationException($"Could not deserialize brick spec: {path}");
    }

    public static async Task WriteFrozenAsync(string workspaceRoot, BrickSpec spec, CancellationToken ct = default)
    {
        var path = Path.Combine(workspaceRoot, "spec.frozen.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, spec, new JsonSerializerOptions { WriteIndented = true }, ct)
            .ConfigureAwait(false);
    }
}
