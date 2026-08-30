using System.Text.RegularExpressions;
using FluentAssertions;
using Ashlar.Core.Application.Paths;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// <c>deploy/node.yml</c> is THE node — the one deployable, restart-durable unit
/// (CLOSING-PLAN Phase 1). These assertions are the convention that keeps it one:
///
/// <para>(a) a restart policy that is present and not <c>no</c>; (b) a NAMED volume mounted at
/// exactly the <c>ASHLAR_STATE_DIR</c> value; (c) no <c>build:</c> key — a node runs a
/// published artifact, never the working tree it stands on; (d) log rotation configured;
/// (e) an image pinned by <c>@sha256:</c> digest; (f) every <c>ASHLAR_*_DIR/_PATH/_ROOT</c>
/// the image or the compose file sets resolves under the state dir; (g) an EXPLICIT clause for
/// the gate store — <c>working_dir</c> under the state dir — because clause (f) is
/// structurally blind to it (no environment variable reaches <c>GateStore</c>: every call site
/// passes a projectDir); (h) exactly ONE compose file in the repository claims THE node's state
/// volume — a named volume at the state dir carrying the pinned literal name
/// <c>ashlar-state</c>, the name the host wrapper addresses — so this test cannot silently
/// pass on a renamed or deleted file, and a second claimant cannot appear —
/// the exact pattern behind the Fleet.Host incident that <c>ci/test-ownership.tsv</c> exists
/// to prevent.</para>
///
/// <para><b>Deliberately NOT asserted:</b> which image the node runs, or which process IS the
/// node. That is open owner decision 1 (CLOSING-PLAN), and a required check is the most
/// expensive place in the repository to encode the wrong answer.</para>
///
/// <para>Hermetic — pure file reads, no build, no Docker, no network — so it costs cert-gate
/// milliseconds and cannot flake.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class NodeUnitConventionTests
{
    private const string NodeYmlRelativePath = "deploy/node.yml";
    private const string DockerfileRelativePath = ".docker/Dockerfile.cli";

    // ---------------------------------------------------------------- (a)
    [Fact]
    public void RestartPolicy_IsPresent_AndNotNo()
    {
        var restart = Scalar(NodeYmlLines(), "restart");
        restart.Should().NotBeNull("a node with no restart policy stays down after the reboot nobody was present for");
        restart!.Trim('"', '\'').Should().NotBe("no", "restart: no turns every host reboot into a silent fleet loss");
    }

    // ---------------------------------------------------------------- (b)
    [Fact]
    public void StateDir_IsBackedByANamedVolume()
    {
        var stateDir = StateDirFromImage();
        var mapping = NamedVolumeMapping(NodeYmlLines(), stateDir);
        mapping.Should().NotBeNull(
            "the node's state ({0}) must live on a NAMED volume — a bind mount inherits host "
            + "ownership traps and an anonymous volume is destroyed by `docker compose down -v` "
            + "with nothing to warn you", stateDir);
    }

    // ---------------------------------------------------------------- (c)
    [Fact]
    public void NoBuildKey_ANodeRunsAPublishedArtifact()
    {
        NodeYmlLines().Should().NotContain(
            l => Regex.IsMatch(l, @"^\s*build\s*:"),
            "a node runs a published, CI-produced artifact — not whatever happens to be in the "
            + "working tree of the machine it is standing on");
    }

    // ---------------------------------------------------------------- (d)
    [Fact]
    public void LogRotation_IsConfigured()
    {
        var text = NodeYmlText();
        text.Should().MatchRegex(@"max-size\s*:", "the default json-file driver grows without bound on a box nobody is watching");
        text.Should().MatchRegex(@"max-file\s*:", "size without a file cap only defers the same full disk");
    }

    // ---------------------------------------------------------------- (e)
    [Fact]
    public void Image_IsPinnedByDigest()
    {
        var image = Scalar(NodeYmlLines(), "image");
        image.Should().NotBeNull("the node service must name an image");
        image.Should().Contain("@sha256:",
            "a tag pin moves silently and :latest's previous manifest becomes GC-bait on GHCR — "
            + "observed to move twice inside one working session (deploy/node.yml's own header). "
            + "Update the pin with scripts/node-update.sh");
    }

    // ---------------------------------------------------------------- (f)
    [Fact]
    public void EveryAshlarStateVariable_ResolvesUnderTheStateDir()
    {
        var stateDir = StateDirFromImage();
        var offenders = new List<string>();

        // Image ENV: strictly KEY=value lines, so the HEALTHCHECK's shell default
        // (${ASHLAR_STATE_DIR:-/data/state}) can never be misread as a declaration.
        foreach (Match m in Regex.Matches(DockerfileText(),
                     @"(ASHLAR_[A-Z_]*(?:_DIR|_PATH|_ROOT))=([^\s\\]+)"))
            CheckUnderStateDir(m.Groups[1].Value, m.Groups[2].Value, stateDir, offenders);

        // Compose environment: KEY: value / - KEY=value forms.
        foreach (var line in NodeYmlLines())
        {
            var m = Regex.Match(line, @"^\s*(?:-\s*)?(ASHLAR_[A-Z_]*(?:_DIR|_PATH|_ROOT))\s*[:=]\s*(\S+)");
            if (m.Success)
                CheckUnderStateDir(m.Groups[1].Value, m.Groups[2].Value, stateDir, offenders);
        }

        offenders.Should().BeEmpty(
            "every ASHLAR_* directory the node reads must land on the state volume, or "
            + "`docker rm` destroys it while this test still passes. Offenders: {0}",
            string.Join(", ", offenders));
    }

    // ---------------------------------------------------------------- (g)
    [Fact]
    public void GateStore_IsExplicitlyPersisted_ViaWorkingDir()
    {
        var stateDir = StateDirFromImage();
        var workingDir = Scalar(NodeYmlLines(), "working_dir");
        workingDir.Should().NotBeNull(
            "the gate store lands at <working_dir>/.ashlar/gates and NO environment variable "
            + "reaches it — GateStore's constructor takes a stateRoot and every call site passes "
            + "a projectDir — so clause (f) is structurally blind to the directory holding every "
            + "durable trust decision, the held queue and the whole admission history");
        workingDir.Should().StartWith(stateDir + "/",
            "a working_dir off the state volume puts the gate store in the container layer, "
            + "where `docker rm` erases the node's entire trust history");
    }

    // ---------------------------------------------------------------- (h)
    [Fact]
    public void ExactlyOneComposeFile_DeclaresTheStateVolume()
    {
        var root = RepoPathResolver.FindRepoRoot();
        var stateDir = StateDirFromImage();
        var declaring = new List<string>();

        foreach (var file in EnumerateComposeCandidates(root))
        {
            string[] lines;
            try { lines = File.ReadAllLines(file); }
            catch (IOException) { continue; }

            if (!lines.Any(l => Regex.IsMatch(l, @"^services\s*:")))
                continue;
            if (NamedVolumeMapping(lines, stateDir) is null)
                continue;
            // Lab stacks legitimately mount their OWN (project-prefixed) volumes at the same
            // in-container path. THE node's volume is the one whose literal name is pinned —
            // that pin is what the host wrapper's fallback addresses.
            if (!lines.Any(l => Regex.IsMatch(l, @"^\s*name\s*:\s*ashlar-state\s*$")))
                continue;
            declaring.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
        }

        declaring.Should().Equal(new[] { NodeYmlRelativePath },
            "exactly ONE compose file may pin the literal volume name ashlar-state: a second "
            + "claimant is two things believing they are THE node, and zero means this suite is "
            + "asserting against a file that no longer exists — it must fail loudly, not pass "
            + "emptily");
    }

    // ---------------------------------------------------------------- helpers

    private static void CheckUnderStateDir(string name, string value, string stateDir, List<string> offenders)
    {
        value = value.TrimEnd('\\').Trim('"', '\'');
        if (value.StartsWith("$", StringComparison.Ordinal))
            return; // interpolated (e.g. ${ASHLAR_OLLAMA_BASE_URL:-...}) — not a literal path decision
        var ok = name == "ASHLAR_STATE_DIR"
            ? value == stateDir
            : value.StartsWith(stateDir + "/", StringComparison.Ordinal);
        if (!ok)
            offenders.Add($"{name}={value}");
    }

    /// <summary>A service-level list entry mapping a NAMED volume (no path prefix) to exactly the state dir.</summary>
    private static string? NamedVolumeMapping(IReadOnlyList<string> lines, string stateDir)
    {
        foreach (var line in lines)
        {
            var m = Regex.Match(line, @"^\s*-\s*([A-Za-z0-9][A-Za-z0-9._-]*)\s*:\s*(\S+?)(?::(?:ro|rw|z|Z)(?:,[a-zA-Z]+)*)?\s*$");
            if (m.Success && m.Groups[2].Value == stateDir)
                return m.Groups[1].Value;
        }
        return null;
    }

    private static string? Scalar(IReadOnlyList<string> lines, string key)
    {
        foreach (var line in lines)
        {
            var m = Regex.Match(line, $@"^\s*{Regex.Escape(key)}\s*:\s*(.+?)\s*$");
            if (m.Success)
                return m.Groups[1].Value;
        }
        return null;
    }

    private static string StateDirFromImage()
    {
        var m = Regex.Match(DockerfileText(), @"ASHLAR_STATE_DIR=([^\s\\]+)");
        m.Success.Should().BeTrue("{0} must declare ASHLAR_STATE_DIR — it is the anchor every other clause measures against", DockerfileRelativePath);
        return m.Groups[1].Value;
    }

    private static string[] NodeYmlLines() => File.ReadAllLines(Locate(NodeYmlRelativePath));
    private static string NodeYmlText() => File.ReadAllText(Locate(NodeYmlRelativePath));
    private static string DockerfileText() => File.ReadAllText(Locate(DockerfileRelativePath));

    private static string Locate(string relative)
    {
        var path = Path.Combine(RepoPathResolver.FindRepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue("{0} is asserted by this suite and may not be moved or deleted without moving these assertions with it", relative);
        return path;
    }

    /// <summary>
    /// Every .yml/.yaml outside pruned territory. Pruning mirrors TestOwnershipConventionTests:
    /// nested checkouts, worktrees and build output belong to other trees; .github is workflow
    /// YAML whose <c>services:</c> key is a runner concept, not compose.
    /// </summary>
    private static IEnumerable<string> EnumerateComposeCandidates(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var dir = pending.Pop();
            var name = Path.GetFileName(dir);
            if (name is ".git" or ".claude" or ".github" or "bin" or "obj" or "node_modules" or "TestResults")
                continue;
            if (dir != root && (File.Exists(Path.Combine(dir, ".git")) || Directory.Exists(Path.Combine(dir, ".git"))))
                continue; // a nested checkout carries its own copies, and they belong to that tree

            foreach (var sub in Directory.EnumerateDirectories(dir))
                pending.Push(sub);
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                if (file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                    yield return file;
            }
        }
    }
}
