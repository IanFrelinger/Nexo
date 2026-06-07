namespace Nexo.CLI.Unity.Pipeline;

using System.Text.Json;

/// <summary>
/// Manages pinned field overrides for generated descriptors. Each descriptor
/// file can have a companion .overrides.json that locks specific values
/// across regeneration cycles.
/// </summary>
public static class DescriptorOverrides
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string GetOverridePath(string descriptorPath) =>
        Path.ChangeExtension(descriptorPath, ".overrides.json");

    public static Dictionary<string, object> Load(string descriptorPath)
    {
        var path = GetOverridePath(descriptorPath);
        if (!File.Exists(path))
            return new Dictionary<string, object>();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, Options)
               ?? new Dictionary<string, object>();
    }

    public static void Pin(string descriptorPath, string field, object value)
    {
        var overrides = Load(descriptorPath);
        overrides[field] = value;
        var path = GetOverridePath(descriptorPath);
        File.WriteAllText(path, JsonSerializer.Serialize(overrides, Options));
    }

    public static void Unpin(string descriptorPath, string field)
    {
        var overrides = Load(descriptorPath);
        overrides.Remove(field);
        var path = GetOverridePath(descriptorPath);
        if (overrides.Count == 0)
            File.Delete(path);
        else
            File.WriteAllText(path, JsonSerializer.Serialize(overrides, Options));
    }

    public static string ToPromptFragment(string descriptorPath)
    {
        var overrides = Load(descriptorPath);
        if (overrides.Count == 0) return "";
        var lines = overrides.Select(kv => $"  {kv.Key}: {kv.Value} (PINNED — do not change)");
        return "\nPinned values (do not modify these):\n" + string.Join("\n", lines);
    }
}
