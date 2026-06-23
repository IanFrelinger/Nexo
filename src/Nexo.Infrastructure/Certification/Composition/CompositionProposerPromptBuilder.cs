using System.Text;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Builds model prompts from <see cref="CompositionProposerInput"/> only — no witness cases.
/// </summary>
public static class CompositionProposerPromptBuilder
{
    public const string SystemPrompt = """
You propose brick-composition wiring for Nexo certification.
Rules:
- Use ONLY brick IDs from the certified catalog.
- Wire composition-level inputs/outputs via __input__ and __output__ node ids.
- Match port names and types exactly as declared on bricks and the target signature.
- Return ONLY a JSON object with keys: compositionId, nodes, edges, inputs, outputs.
- nodes: [{ "nodeId": "...", "brickId": "..." }]
- edges: [{ "sourceNodeId": "...", "sourcePort": "...", "targetNodeId": "...", "targetPort": "..." }]
- inputs/outputs: [{ "name": "...", "type": "..." }]
- Do not include witness values or expected outputs.
""";

    public static string BuildUserPrompt(CompositionProposerInput input)
    {
        var target = input.Target;
        var builder = new StringBuilder();
        builder.AppendLine($"CompositionId: {target.CompositionId}");
        builder.AppendLine("Target inputs: " + FormatPorts(target.Inputs));
        builder.AppendLine("Target outputs: " + FormatPorts(target.Outputs));
        builder.AppendLine("Certified bricks:");

        foreach (var brick in input.CertifiedBricks)
        {
            builder.AppendLine($"  - {brick.BrickId} (cert: {brick.AtomCertificationReference})");
            builder.AppendLine($"      inputs: {FormatIoFields(brick.Inputs)}");
            builder.AppendLine($"      outputs: {FormatIoFields(brick.Outputs)}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatPorts(IReadOnlyList<CompositionPort> ports) =>
        string.Join(", ", ports.Select(p => $"{p.Name}:{p.Type}"));

    private static string FormatIoFields(IReadOnlyList<WitnessIoField> fields) =>
        string.Join(", ", fields.Select(f => $"{f.Name}:{f.Type}"));
}
