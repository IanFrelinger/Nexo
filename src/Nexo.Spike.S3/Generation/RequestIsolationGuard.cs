using Nexo.Spike.S0;
using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S3.Generation;

/// <summary>
/// Ensures generator prompts/requests never include oracle, test, property, or acceptance content.
/// </summary>
public static class RequestIsolationGuard
{
    public sealed record Violation(string Category, string Excerpt);

    public static IReadOnlyList<Violation> FindViolations(string content)
    {
        var violations = new List<Violation>();
        foreach (var sample in LoadForbiddenSamples())
        {
            if (ContainsDistinctiveFragment(content, sample.Value))
            {
                violations.Add(new Violation(sample.Key, Truncate(sample.Value)));
            }
        }

        return violations;
    }

    public static void AssertIsolated(string content, string contextLabel)
    {
        var violations = FindViolations(content);
        if (violations.Count == 0)
            return;

        var details = string.Join(
            "; ",
            violations.Select(v => $"{v.Category}: '{v.Excerpt}'"));

        throw new InvalidOperationException(
            $"Request isolation violated for {contextLabel}: {details}");
    }

    private static bool ContainsDistinctiveFragment(string haystack, string needle)
    {
        if (string.IsNullOrWhiteSpace(needle))
            return false;

        foreach (var fragment in DistinctiveFragments(needle))
        {
            if (haystack.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> DistinctiveFragments(string sample)
    {
        var lines = sample.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (line.Length >= 12)
                yield return line;
        }

        if (sample.Length >= 12)
            yield return sample[..Math.Min(sample.Length, 48)];
    }

    private static Dictionary<string, string> LoadForbiddenSamples()
    {
        var samples = new Dictionary<string, string>(StringComparer.Ordinal);

        var intentPath = GenerationRequestSealer.ResolveIntentSpecPath();
        var intentJson = File.ReadAllText(intentPath);
        var spec = BrickSpecLoader.LoadAsync(intentPath).GetAwaiter().GetResult();
        samples["acceptance-criteria"] = string.Join('\n', spec.AcceptanceCriteria);

        foreach (var path in ResolveForbiddenPaths())
        {
            if (!File.Exists(path))
                continue;

            var text = File.ReadAllText(path);
            var key = Path.GetFileName(path);
            samples[key] = text;
        }

        return samples;
    }

    private static IEnumerable<string> ResolveForbiddenPaths()
    {
        var relative = new[]
        {
            Path.Combine("src", "Nexo.Spike.S1", "Fixtures", "honest-tests.cs"),
            Path.Combine("src", "Nexo.Spike.S1", "Fixtures", "honest-impl.cs"),
            Path.Combine("samples", "spike-s0", "template", "CsvColumnInferrer.Properties", "MetamorphicPropertyTests.cs"),
            Path.Combine("samples", "spike-s0", "template", "CsvColumnInferrer.Properties", "SpecAcceptancePropertyTests.cs"),
            Path.Combine("artifacts", "s2", "reference-oracle.json")
        };

        var roots = new List<string> { Directory.GetCurrentDirectory() };
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            roots.Add(dir.FullName);
            dir = dir.Parent;
        }

        foreach (var root in roots.Distinct())
        {
            foreach (var rel in relative)
            {
                var full = Path.Combine(root, rel);
                if (File.Exists(full))
                    yield return full;
            }
        }
    }

    private static string Truncate(string value, int max = 60) =>
        value.Length <= max ? value : value[..max] + "...";
}
