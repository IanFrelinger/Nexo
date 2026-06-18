using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;

namespace Nexo.Spike.S0.Agents;

/// <summary>
/// File-based stage agent for spike runs: reads prompts from workspace/prompts/ and
/// expects artifacts already placed by an external operator or scripted harness.
/// </summary>
public sealed class FileBasedStageAgent : IStageAgent
{
    public string RoleName { get; }

    public FileBasedStageAgent(string roleName) => RoleName = roleName;

    public async Task<StageAgentResult> ExecuteAsync(
        SpikeStage stage,
        RoleView scope,
        string workspaceRoot,
        IReadOnlyList<string>? mutationFeedback,
        CancellationToken ct = default)
    {
        var promptDir = Path.Combine(workspaceRoot, "prompts", scope.RoleName);
        var stageMarker = Path.Combine(workspaceRoot, ".stage", $"{stage}.done");

        if (scope.VisibleSpec.HasOpenQuestions)
            return StageAgentResult.Blocked("Spec has open questions", scope.VisibleSpec.OpenQuestions.ToArray());

        // If external agent already marked stage complete, accept.
        if (File.Exists(stageMarker))
            return StageAgentResult.Ok($"Stage {stage} marked complete by external agent.");

        var expectedArtifacts = stage switch
        {
            SpikeStage.Spec => new[] { Path.Combine(workspaceRoot, "spec.frozen.json") },
            SpikeStage.Red => Directory.GetFiles(
                Path.Combine(workspaceRoot, "CsvColumnInferrer.Tests"),
                "*Tests.cs",
                SearchOption.AllDirectories),
            SpikeStage.Green => new[] { Path.Combine(workspaceRoot, "CsvColumnInferrer", "CsvColumnInferrer.cs") },
            SpikeStage.Verify => Array.Empty<string>(),
            _ => Array.Empty<string>()
        };

        foreach (var artifact in expectedArtifacts)
        {
            if (!File.Exists(artifact))
            {
                var promptPath = Path.Combine(promptDir, $"{stage}.md");
                var hint = File.Exists(promptPath)
                    ? await File.ReadAllTextAsync(promptPath, ct).ConfigureAwait(false)
                    : $"Place artifacts for {stage} and create {stageMarker} when done.";
                return StageAgentResult.Blocked(
                    $"Missing artifact for {stage}: {artifact}",
                    hint.Trim());
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(stageMarker)!);
        await File.WriteAllTextAsync(stageMarker, DateTimeOffset.UtcNow.ToString("O"), ct).ConfigureAwait(false);
        return StageAgentResult.Ok($"Stage {stage} artifacts present.");
    }
}
