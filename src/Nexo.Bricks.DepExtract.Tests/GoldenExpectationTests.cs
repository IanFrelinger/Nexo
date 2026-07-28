using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Bricks.DepExtract;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>Locks fixture contracts from Fixtures/stress/golden-expectations.json.</summary>
public sealed class GoldenExpectationTests : IDisposable
{
    private readonly string _stress = Path.Combine(AppContext.BaseDirectory, "Fixtures", "stress");
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), "dep-extract-golden-" + Guid.NewGuid());
    private readonly CppDependencyExtractorBrick _brick = new(NullLogger<CppDependencyExtractorBrick>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_outDir)) Directory.Delete(_outDir, true);
    }

    [Fact]
    public async Task Golden_expectations_match_extract_graphs_and_symbols()
    {
        // Unit goldens must not inherit a compose/agent WORKSPACE_ROOT (or a parallel test's).
        var prevRoot = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
        Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
        try
        {
            var path = Path.Combine(_stress, "golden-expectations.json");
            File.Exists(path).Should().BeTrue();
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            foreach (var tier in doc.RootElement.EnumerateObject())
            {
                var dir = Path.Combine(_stress, tier.Name);
                Directory.Exists(dir).Should().BeTrue(tier.Name);
                var entries = tier.Value.GetProperty("entries").EnumerateArray().Select(e => e.GetString()!).ToArray();
                var values = new Dictionary<string, object>
                {
                    ["srcDir"] = dir,
                    ["outDir"] = Path.Combine(_outDir, tier.Name),
                    ["entries"] = entries,
                    ["manifestOnly"] = true
                };
                if (tier.Value.TryGetProperty("includeDirs", out var ids))
                    values["includeDirs"] = ids.EnumerateArray().Select(e => e.GetString()!).ToArray();

                var output = await _brick.ExecuteAsync(new BrickInput(values), ImplementationType.Deterministic, new AirGappedTestContext());
                var lower = string.Join('\n', output.Get<string[]>("lower")).Replace('\\', '/');

                if (tier.Value.TryGetProperty("requiredLowerFragments", out var frags))
                {
                    foreach (var f in frags.EnumerateArray())
                        lower.Should().Contain(f.GetString()!, because: tier.Name);
                }
                if (tier.Value.TryGetProperty("requiredSymbols", out var syms))
                {
                    var sourceBlob = string.Join('\n', entries.Select(e => File.ReadAllText(Path.Combine(dir, e))));
                    foreach (var s in syms.EnumerateArray())
                        sourceBlob.Should().Contain(s.GetString()!, because: $"{tier.Name} source must expose symbol");
                }
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", prevRoot);
        }
    }
}
