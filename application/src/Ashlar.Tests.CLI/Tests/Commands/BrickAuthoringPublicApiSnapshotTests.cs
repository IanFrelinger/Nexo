using FluentAssertions;
using PublicApiGenerator;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for brick authoring public api snapshot.</summary>
[Trait("Category", "CLI")]
public sealed class BrickAuthoringPublicApiSnapshotTests
{
    [Fact]
    public void Supported_extension_surface_matches_approved_snapshot()
    {
        var snapshots = new Dictionary<string, string>
        {
            ["Ashlar.Authoring.approved.txt"] = typeof(Ashlar.Authoring.AshlarAuthoringServiceCollectionExtensions).Assembly.GeneratePublicApi(),
            ["Ashlar.Sdk.approved.txt"] = typeof(Ashlar.Sdk.Client.AshlarClientSdkBuilder).Assembly.GeneratePublicApi(),
            ["Ashlar.Framework.Sdk.approved.txt"] = typeof(Ashlar.Framework.Sdk.AshlarFrameworkOptions).Assembly.GeneratePublicApi()
        };

        var snapshotRoot = FindSnapshotRoot();
        Directory.CreateDirectory(snapshotRoot);

        foreach (var (fileName, current) in snapshots)
        {
            var path = Path.Combine(snapshotRoot, fileName);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, current);
                /// <summary>Invalid operation exception.</summary>
                /// <param name="it."">It.".</param>
                throw new InvalidOperationException($"Created missing public API snapshot: {path}. Review and commit it.");
            }

            var approved = File.ReadAllText(path);
            Normalize(current).Should().Be(Normalize(approved), fileName);
        }
    }

    /// <summary>Makes PublicApiGenerator output comparable across toolchains. Different
    /// CodeDOM versions emit namespaces in a different order and wrap long attribute string
    /// literals into "…" + "…" concatenations at different widths while the member surface
    /// is identical, so both the generated and the approved text pass through this.</summary>
    private static string Normalize(string api)
        => SortNamespaceBlocks(JoinWrappedStringLiterals(api.ReplaceLineEndings("\n")));

    /// <summary>Rejoins string literals that CodeDOM wrapped across lines
    /// ("abc" + newline "def" becomes "abcdef").</summary>
    private static string JoinWrappedStringLiterals(string text)
    {
        var lines = new List<string>(text.Split('\n'));
        for (var i = 0; i < lines.Count - 1;)
        {
            var next = lines[i + 1].TrimStart();
            if (lines[i].EndsWith("\" +", StringComparison.Ordinal) && next.StartsWith('"'))
            {
                lines[i] = lines[i][..^3] + next[1..];

                // Do not advance: a literal wrapped over three or more lines merges fully.
                lines.RemoveAt(i + 1);
            }
            else
            {
                i++;
            }
        }

        return string.Join('\n', lines);
    }

    /// <summary>Reassembles the text with the top-level namespace blocks in ordinal order,
    /// keeping the assembly-attribute preamble in place and pinning a single trailing
    /// newline.</summary>
    private static string SortNamespaceBlocks(string text)
    {
        var preamble = new List<string>();
        var blocks = new List<(string Name, List<string> Lines)>();
        List<string>? block = null;

        foreach (var line in text.TrimEnd('\n').Split('\n'))
        {
            if (line.StartsWith("namespace ", StringComparison.Ordinal))
            {
                block = new List<string> { line };
                blocks.Add((line["namespace ".Length..], block));
            }
            else if (block is null)
            {
                preamble.Add(line);
            }
            else
            {
                block.Add(line);
                if (line == "}")
                {
                    block = null;
                }
            }
        }

        var ordered = preamble.Concat(
            blocks.OrderBy(b => b.Name, StringComparer.Ordinal).SelectMany(b => b.Lines));
        return string.Join('\n', ordered) + "\n";
    }

    private static string FindSnapshotRoot()
    {
        // Anchored on the test assembly's own location, NOT Environment.CurrentDirectory.
        // CWD is process-global and other suites in this assembly (WorkflowCommandTests
        // sets it in ~17 tests) legitimately move it; anything that outlives its test —
        // an async continuation, background work started by a command under test — can
        // leave it stranded, and this walk then fails with "Repository root not found".
        // AppContext.BaseDirectory is fixed for the life of the process.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Ashlar.sln")))
            {
                return Path.Combine(
                    current.FullName,
                    "application",
                    "src",
                    "Ashlar.Tests.CLI",
                    "PublicApi");
            }

            current = current.Parent;
        }

        /// <summary>Invalid operation exception.</summary>
        /// <param name="found."">Found.".</param>
        throw new InvalidOperationException("Repository root not found.");
    }
}
