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
            current.ReplaceLineEndings("\n").Should().Be(approved.ReplaceLineEndings("\n"), fileName);
        }
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
