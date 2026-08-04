using FluentAssertions;
using Nexo.Core.Application.Paths;
using Xunit;

namespace Nexo.Tests.Application.Tests.Paths;

/// <summary>
/// <see cref="RepoPathResolver"/> locates the repo root and the Block 1 observation
/// folder. Both methods fall back to the current directory when they find nothing,
/// which makes their PRECEDENCE and their failure modes the things worth pinning —
/// a silent wrong-directory answer is far worse than a throw.
///
/// Every test passes an explicit start/root and works inside its own temp tree, so
/// nothing here mutates the process-global current directory. That matters: doing
/// so is exactly what made Nexo.Tests.CLI flaky (see ConsoleCapture / 5fc20270).
/// </summary>
public sealed class RepoPathResolverTests : IDisposable
{
    private readonly string _root;

    public RepoPathResolverTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"nexo-repopath-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    // ------------------------------------------------------------ FindRepoRoot

    [Fact]
    public void Finds_the_directory_containing_Nexo_sln()
    {
        var repo = Dir("repo");
        File.WriteAllText(Path.Combine(repo, "Nexo.sln"), "");

        RepoPathResolver.FindRepoRoot(repo).Should().Be(repo);
    }

    [Fact]
    public void Walks_upward_from_a_nested_directory()
    {
        var repo = Dir("repo");
        File.WriteAllText(Path.Combine(repo, "Nexo.sln"), "");
        var nested = Dir("repo", "src", "Some.Project", "Deep");

        RepoPathResolver.FindRepoRoot(nested).Should().Be(repo,
            "the resolver must find the root from anywhere inside the tree, not just the top");
    }

    [Fact]
    public void Falls_back_to_the_current_directory_when_no_solution_is_found()
    {
        // A temp tree with no Nexo.sln anywhere above it. The documented fallback is
        // the CWD — asserted explicitly so a future change to throw (or to return
        // the start dir) is a visible decision rather than a silent one.
        var orphan = Dir("no-solution-here", "nested");

        RepoPathResolver.FindRepoRoot(orphan).Should().Be(Directory.GetCurrentDirectory());
    }

    [Fact]
    public void Stops_at_the_nearest_solution_when_trees_are_nested()
    {
        var outer = Dir("outer");
        File.WriteAllText(Path.Combine(outer, "Nexo.sln"), "");
        var inner = Dir("outer", "vendored", "inner");
        File.WriteAllText(Path.Combine(inner, "Nexo.sln"), "");
        var deep = Dir("outer", "vendored", "inner", "src");

        RepoPathResolver.FindRepoRoot(deep).Should().Be(inner,
            "the nearest enclosing solution wins; walking past it would resolve a vendored copy to the host repo");
    }

    // ------------------------------------------- FindBlock1ObservationPath

    [Fact]
    public void Prefers_the_Infrastructure_observation_folder()
    {
        var repo = RepoWithSln();
        var infra = Dir("repo", "src", "Nexo.Infrastructure", "Observation");
        Dir("repo", "src", "Nexo.BackgroundAgents", "Observation");

        RepoPathResolver.FindBlock1ObservationPath(repo).Should().Be(infra,
            "Infrastructure is documented as the preferred source when both exist");
    }

    [Fact]
    public void Falls_back_to_the_BackgroundAgents_observation_folder()
    {
        var repo = RepoWithSln();
        var bg = Dir("repo", "src", "Nexo.BackgroundAgents", "Observation");

        RepoPathResolver.FindBlock1ObservationPath(repo).Should().Be(bg);
    }

    [Fact]
    public void Falls_back_to_the_current_directory_when_neither_folder_exists()
    {
        var repo = RepoWithSln();

        RepoPathResolver.FindBlock1ObservationPath(repo)
            .Should().Be(Directory.GetCurrentDirectory());
    }

    [Fact]
    public void Returns_the_current_directory_when_the_root_is_not_a_Nexo_repo()
    {
        // No Nexo.sln at the supplied root: the resolver must not go on to hand back
        // a plausible-looking src/...\Observation path from an unrelated tree.
        var notARepo = Dir("not-a-repo");
        Dir("not-a-repo", "src", "Nexo.Infrastructure", "Observation");

        RepoPathResolver.FindBlock1ObservationPath(notARepo)
            .Should().Be(Directory.GetCurrentDirectory());
    }

    private string RepoWithSln()
    {
        var repo = Dir("repo");
        File.WriteAllText(Path.Combine(repo, "Nexo.sln"), "");
        return repo;
    }

    private string Dir(params string[] parts)
    {
        var path = Path.Combine(new[] { _root }.Concat(parts).ToArray());
        Directory.CreateDirectory(path);
        return path;
    }
}
