using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Bricks.DepExtract;

namespace Nexo.Bricks.DepExtract.Gui;

/// <summary>Persists adapt plans under TMPDIR/WORKSPACE_ROOT/.dep-extract-out/plans so approve survives restarts.</summary>
public static class PlanSessionStore
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static string ResolvePlansDirectory()
    {
        var tmpBase = Environment.GetEnvironmentVariable("TMPDIR");
        if (string.IsNullOrWhiteSpace(tmpBase))
        {
            tmpBase = DepExtractPathRules.TryGetWorkspaceRoot() is { } ws
                ? Path.Combine(ws, ".dep-extract-out")
                : Path.Combine(Path.GetTempPath(), "dep-extract-out");
        }
        var dir = Path.Combine(tmpBase, "plans");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void Save(PlanSession session)
    {
        var path = Path.Combine(ResolvePlansDirectory(), session.PlanId + ".json");
        var json = JsonSerializer.Serialize(session, JsonOpts);
        File.WriteAllText(path, json);
    }

    public static PlanSession? TryLoad(string planId)
    {
        if (string.IsNullOrWhiteSpace(planId)) return null;
        var path = Path.Combine(ResolvePlansDirectory(), planId + ".json");
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PlanSession>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public static void LoadInto(ConcurrentDictionary<string, PlanSession> plans)
    {
        var dir = ResolvePlansDirectory();
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var session = JsonSerializer.Deserialize<PlanSession>(json, JsonOpts);
                if (session is null || string.IsNullOrWhiteSpace(session.PlanId)) continue;
                plans[session.PlanId] = session;
            }
            catch
            {
                // skip corrupt plan files
            }
        }
    }
}
