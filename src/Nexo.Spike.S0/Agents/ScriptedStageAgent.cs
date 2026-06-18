using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;

namespace Nexo.Spike.S0.Agents;

/// <summary>
/// Test harness agent that copies fixture files into the workspace per stage.
/// </summary>
public sealed class ScriptedStageAgent : IStageAgent
{
    private readonly string _roleName;
    private readonly IReadOnlyDictionary<SpikeStage, IReadOnlyList<(string Source, string DestRelative)>> _scripts;

    public ScriptedStageAgent(
        string roleName,
        IReadOnlyDictionary<SpikeStage, IReadOnlyList<(string Source, string DestRelative)>> scripts)
    {
        _roleName = roleName;
        _scripts = scripts;
    }

    public string RoleName => _roleName;

    public Task<StageAgentResult> ExecuteAsync(
        SpikeStage stage,
        RoleView scope,
        string workspaceRoot,
        IReadOnlyList<string>? mutationFeedback,
        CancellationToken ct = default)
    {
        if (!_scripts.TryGetValue(stage, out var copies))
            return Task.FromResult(StageAgentResult.Ok($"No script for {stage}."));

        foreach (var (source, destRelative) in copies)
        {
            var dest = Path.Combine(workspaceRoot, destRelative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(source, dest, overwrite: true);
        }

        if (stage == SpikeStage.Red)
        {
            var placeholder = Path.Combine(workspaceRoot, "CsvColumnInferrer.Tests", "ColumnTypeInferrerPlaceholderTests.cs");
            if (File.Exists(placeholder))
                File.Delete(placeholder);
        }

        var marker = Path.Combine(workspaceRoot, ".stage", $"{stage}.done");
        Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
        File.WriteAllText(marker, DateTimeOffset.UtcNow.ToString("O"));

        return Task.FromResult(StageAgentResult.Ok($"Scripted {stage} for {_roleName}."));
    }
}
