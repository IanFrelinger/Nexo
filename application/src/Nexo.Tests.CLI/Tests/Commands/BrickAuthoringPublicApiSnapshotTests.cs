using FluentAssertions;
using PublicApiGenerator;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for brick authoring public api snapshot.</summary>
[Trait("Category", "CLI")]
public sealed class BrickAuthoringPublicApiSnapshotTests
{
    [Fact]
    public void Supported_extension_surface_matches_approved_snapshot()
    {
        var snapshots = new Dictionary<string, string>
        {
            ["Nexo.Authoring.approved.txt"] = typeof(Nexo.Authoring.NexoAuthoringServiceCollectionExtensions).Assembly.GeneratePublicApi(),
            ["Nexo.Sdk.approved.txt"] = typeof(Nexo.Sdk.Client.NexoClientSdkBuilder).Assembly.GeneratePublicApi(),
            ["Nexo.Framework.Sdk.approved.txt"] = typeof(Nexo.Framework.Sdk.NexoFrameworkOptions).Assembly.GeneratePublicApi()
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
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Nexo.sln")))
            {
                return Path.Combine(
                    current.FullName,
                    "application",
                    "src",
                    "Nexo.Tests.CLI",
                    "PublicApi");
            }

            current = current.Parent;
        }

        /// <summary>Invalid operation exception.</summary>
        /// <param name="found."">Found.".</param>
        throw new InvalidOperationException("Repository root not found.");
    }
}
