using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BackgroundAgents.Forge;
using Nexo.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Commands;

public class ProposalsBackgroundAgentCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoRoot;
    private readonly ChangeProposalStore _store;

    public ProposalsBackgroundAgentCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-prop-cli-" + Guid.NewGuid().ToString("N"));
        _repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(Path.Combine(_repoRoot, "src"));
        _store = new ChangeProposalStore(Path.Combine(_tempDir, "forge"));
    }

    public void Dispose() { try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ } }

    private ProposalsBackgroundAgentCommand NewCmd() =>
        new(_store, NullLogger<ProposalsBackgroundAgentCommand>.Instance);

    [Fact]
    public async Task Approve_then_apply_writes_file_and_marks_applied()
    {
        var saved = _store.Add(new ChangeProposal
        {
            Id = "to-apply",
            TargetPath = "src/A.cs",
            NewContent = "// hello",
            Summary = "first"
        });

        var cmd = NewCmd();
        var stdout = new StringWriter(); var stderr = new StringWriter();
        (await cmd.ApproveAsync(saved.Id, "alice", "lgtm", formatJson: false)).Should().Be(0);

        var rc = await cmd.ApplyAsync(saved.Id, _repoRoot, force: false, formatJson: false, stdout, stderr);
        rc.Should().Be(0);
        File.ReadAllText(Path.Combine(_repoRoot, "src", "A.cs")).Should().Be("// hello");
        _store.Find(saved.Id)!.Status.Should().Be(ChangeProposalStatus.Applied);
    }

    [Fact]
    public async Task Apply_refuses_when_base_sha_drifted_unless_forced()
    {
        var path = Path.Combine(_repoRoot, "src", "B.cs");
        File.WriteAllText(path, "// original");
        var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("// original")));

        _store.Add(new ChangeProposal
        {
            Id = "drift",
            TargetPath = "src/B.cs",
            NewContent = "// rewritten",
            Summary = "rewrite",
            BaseSha256 = sha
        });
        _store.Approve("drift");

        File.WriteAllText(path, "// drifted by someone else");
        var cmd = NewCmd();
        var stdout = new StringWriter(); var stderr = new StringWriter();
        var rc = await cmd.ApplyAsync("drift", _repoRoot, force: false, formatJson: false, stdout, stderr);
        rc.Should().Be(3);
        stderr.ToString().Should().Contain("Drift");

        var rcForce = await cmd.ApplyAsync("drift", _repoRoot, force: true, formatJson: false, new StringWriter(), new StringWriter());
        rcForce.Should().Be(0);
        File.ReadAllText(path).Should().Be("// rewritten");
    }

    [Fact]
    public async Task Janitor_stales_old_proposals_and_reports_ids()
    {
        // Add an Approved proposal and force its UpdatedAt to be 100 hours ago.
        _store.Add(new ChangeProposal { Id = "stale-me", TargetPath = "src/C.cs", NewContent = "x", Summary = "old" });
        _store.Approve("stale-me");
        var approvedPath = Path.Combine(_tempDir, "forge", "approved", "stale-me.json");
        var doc = JsonSerializer.Deserialize<ChangeProposal>(File.ReadAllText(approvedPath))!;
        File.WriteAllText(approvedPath, JsonSerializer.Serialize(doc with { UpdatedAt = DateTimeOffset.UtcNow.AddHours(-100) }, new JsonSerializerOptions { WriteIndented = true }));

        var cmd = NewCmd();
        var stdout = new StringWriter(); var stderr = new StringWriter();
        var rc = await cmd.JanitorAsync(proposedTtlHours: null, approvedTtlHours: 24, formatJson: true, stdout, stderr);
        rc.Should().Be(0);

        using var json = JsonDocument.Parse(stdout.ToString());
        json.RootElement.GetProperty("count").GetInt32().Should().Be(1);
        _store.Find("stale-me")!.Status.Should().Be(ChangeProposalStatus.Stale);
    }

    [Fact]
    public async Task List_filters_by_status_and_target_prefix()
    {
        _store.Add(new ChangeProposal { Id = "a", TargetPath = "src/A.cs", NewContent = "x", Summary = "a" });
        _store.Add(new ChangeProposal { Id = "b", TargetPath = "tests/B.cs", NewContent = "x", Summary = "b" });
        _store.Approve("b");

        var cmd = NewCmd();
        var stdout = new StringWriter(); var stderr = new StringWriter();
        var rc = await cmd.ListAsync(status: "Approved", targetPrefix: null, formatJson: true, stdout, stderr);
        rc.Should().Be(0);

        using var json = JsonDocument.Parse(stdout.ToString());
        json.RootElement.GetProperty("count").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("proposals")[0].GetProperty("id").GetString().Should().Be("b");
    }
}
