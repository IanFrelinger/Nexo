using FluentAssertions;
using Xunit;

namespace CsvColumnInferrer.Properties;

/// <summary>
/// Load-bearing: every frozen acceptance criterion must hold against the implementation.
/// </summary>
[Trait("Category", "SpikeProperty")]
public sealed class SpecAcceptancePropertyTests
{
    [Fact]
    public void Implementation_satisfies_all_frozen_acceptance_criteria()
    {
        var workspace = FindWorkspaceRoot();
        var examples = SpecAcceptanceLoader.Load(workspace);
        examples.Should().NotBeEmpty("frozen spec must declare acceptance criteria");

        foreach (var example in examples)
        {
            var actual = ColumnTypeInferrer.InferType(example.Values);
            actual.Should().Be(example.Expected,
                because: $"acceptance criterion {Format(example)}");
        }
    }

    private static string Format(SpecAcceptanceLoader.Example example) =>
        $"[{string.Join(", ", example.Values.Select(v => $"\"{v}\""))}] => {example.Expected}";

    private static string FindWorkspaceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "spec.frozen.json")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate workspace root containing spec.frozen.json");
    }
}
