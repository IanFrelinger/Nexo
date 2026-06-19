using System.Diagnostics;
using System.Text;
using Nexo.Spike.S2.Adversary;

namespace Nexo.Spike.S2.Oracle;

/// <summary>
/// Judges candidate implementations against the frozen reference oracle (held-out from adversary).
/// </summary>
public sealed class ReferenceOracleJudge
{
    public async Task<OracleJudgment> JudgeAsync(
        string implementationSource,
        ReferenceOracle oracle,
        CancellationToken ct = default)
    {
        var actual = await EvaluateImplementationAsync(implementationSource, oracle.Labels, ct)
            .ConfigureAwait(false);

        var disagreements = new List<OracleDisagreement>();
        for (var i = 0; i < oracle.Labels.Count; i++)
        {
            var label = oracle.Labels[i];
            var actualType = actual[i];
            if (!string.Equals(actualType, label.ExpectedType, StringComparison.Ordinal))
            {
                disagreements.Add(new OracleDisagreement(label.Inputs, label.ExpectedType, actualType));
            }
        }

        return new OracleJudgment(disagreements.Count == 0, disagreements);
    }

    public IReadOnlyList<OracleDisagreement> HeldOutDisagreements(OracleJudgment judgment, ReferenceOracle oracle)
    {
        var heldOutKeys = oracle.HeldOutLabels
            .Select(l => ReferenceOracleLoader.SerializeInputKey(l.Inputs))
            .ToHashSet(StringComparer.Ordinal);

        return judgment.Disagreements
            .Where(d => heldOutKeys.Contains(ReferenceOracleLoader.SerializeInputKey(d.Inputs)))
            .ToList();
    }

    private static async Task<IReadOnlyList<string>> EvaluateImplementationAsync(
        string implementationSource,
        IReadOnlyList<ReferenceLabel> labels,
        CancellationToken ct)
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"nexo-s2-oracle-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(workspace);
            await File.WriteAllTextAsync(Path.Combine(workspace, "ColumnTypeInferrer.cs"), implementationSource, ct)
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(workspace, "Program.cs"), BuildProgram(labels), ct)
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(
                Path.Combine(workspace, "OracleJudge.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                </Project>
                """,
                ct).ConfigureAwait(false);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project OracleJudge.csproj",
                WorkingDirectory = workspace,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start oracle judge process.");

            var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            if (process.ExitCode != 0)
                return labels.Select(_ => "?").ToList();

            return stdout.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
        finally
        {
            if (Directory.Exists(workspace))
            {
                try { Directory.Delete(workspace, recursive: true); }
                catch { /* best-effort */ }
            }
        }
    }

    private static string BuildProgram(IReadOnlyList<ReferenceLabel> labels)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using CsvColumnInferrer;");
        sb.AppendLine("var outputs = new List<string>();");
        foreach (var label in labels)
        {
            var args = string.Join(", ", label.Inputs.Select(EscapeCSharpString));
            sb.AppendLine($"outputs.Add(ColumnTypeInferrer.InferType(new[] {{ {args} }}).ToString());");
        }

        sb.AppendLine("Console.WriteLine(string.Join('\\n', outputs));");
        return sb.ToString();
    }

    private static string EscapeCSharpString(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
