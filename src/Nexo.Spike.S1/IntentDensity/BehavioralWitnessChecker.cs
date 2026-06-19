using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.Spike.S1.IntentDensity;

/// <summary>
/// Compares two impl sources on frozen acceptance witnesses (behavioral equivalence).
/// </summary>
public static class BehavioralWitnessChecker
{
    private static readonly Regex CriterionRegex = new(
        @"^\[(?<values>.*?)\]\s*=>\s*(?<type>\w+)$",
        RegexOptions.Compiled);

    public sealed record Witness(string[] Values, string ExpectedType);

    public static IReadOnlyList<Witness> LoadWitnesses(string intentPath)
    {
        using var stream = File.OpenRead(intentPath);
        using var doc = JsonDocument.Parse(stream);
        if (!doc.RootElement.TryGetProperty("acceptanceCriteria", out var criteria) &&
            !doc.RootElement.TryGetProperty("AcceptanceCriteria", out criteria))
            return [];

        var witnesses = new List<Witness>();
        foreach (var item in criteria.EnumerateArray())
        {
            var text = item.GetString();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (text.StartsWith("Mixed numeric and text", StringComparison.OrdinalIgnoreCase))
            {
                witnesses.Add(new Witness(["1", "hello"], "String"));
                continue;
            }

            var match = CriterionRegex.Match(text.Trim());
            if (!match.Success)
                continue;

            var values = JsonSerializer.Deserialize<string[]>("[" + match.Groups["values"].Value + "]")
                         ?? [];
            witnesses.Add(new Witness(values, match.Groups["type"].Value));
        }

        return witnesses;
    }

    public static async Task<bool> IsBehaviorallyIdenticalAsync(
        string honestImplSource,
        string candidateImplSource,
        string intentPath,
        CancellationToken ct = default)
    {
        var witnesses = LoadWitnesses(intentPath);
        if (witnesses.Count == 0)
            return true;

        var honest = await EvaluateAsync(honestImplSource, witnesses, ct).ConfigureAwait(false);
        var candidate = await EvaluateAsync(candidateImplSource, witnesses, ct).ConfigureAwait(false);
        return honest.SequenceEqual(candidate);
    }

    private static async Task<IReadOnlyList<string>> EvaluateAsync(
        string implSource,
        IReadOnlyList<Witness> witnesses,
        CancellationToken ct)
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"nexo-witness-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(workspace);
            await File.WriteAllTextAsync(Path.Combine(workspace, "ColumnTypeInferrer.cs"), implSource, ct)
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(workspace, "Program.cs"), BuildProgram(witnesses), ct)
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(
                Path.Combine(workspace, "WitnessCheck.csproj"),
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
                Arguments = "run --project WitnessCheck.csproj",
                WorkingDirectory = workspace,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start witness checker");

            var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            if (process.ExitCode != 0)
                return witnesses.Select(_ => "?").ToList();

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

    private static string BuildProgram(IReadOnlyList<Witness> witnesses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using CsvColumnInferrer;");
        sb.AppendLine("var outputs = new List<string>();");
        foreach (var witness in witnesses)
        {
            var args = string.Join(", ", witness.Values.Select(EscapeCSharpString));
            sb.AppendLine($"outputs.Add(ColumnTypeInferrer.InferType(new[] {{ {args} }}).ToString());");
        }

        sb.AppendLine("Console.WriteLine(string.Join('\\n', outputs));");
        return sb.ToString();
    }

    private static string EscapeCSharpString(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
