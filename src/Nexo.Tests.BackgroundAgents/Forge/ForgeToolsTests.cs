using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.Policies.Dev;
using Nexo.Tools.Dev;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Forge;

/// <summary>Forge tool + policy integration (Streams C).</summary>
[Trait("Category", "RuntimeStudio")]
public class ForgeToolsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoRoot;
    private readonly ChangeProposalStore _store;

    public ForgeToolsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-forge-tools-" + Guid.NewGuid().ToString("N"));
        _repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(Path.Combine(_repoRoot, "src"));
        _store = new ChangeProposalStore(Path.Combine(_tempDir, "forge"));
    }

    public void Dispose() { try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ } }

    [Fact]
    public async Task Propose_change_creates_proposal_with_base_sha_when_file_exists()
    {
        var existing = Path.Combine(_repoRoot, "src", "A.cs");
        File.WriteAllText(existing, "// before");
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["RepoRoot"] = _repoRoot });
        var tool = new ForgeProposeChangeTool(_store, agentIdProvider: () => "planner");

        var args = JsonSerializer.SerializeToElement(new
        {
            target_path = "src/A.cs",
            new_content = "// after",
            summary = "rename",
            reason = "clarity"
        });
        var result = await tool.InvokeAsync(new ToolCall(tool.Id, args), snap, default);

        var json = JsonSerializer.Serialize(result.Payload);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        var id = doc.RootElement.GetProperty("id").GetString()!;
        var stored = _store.Find(id)!;
        stored.AgentId.Should().Be("planner");
        stored.TargetPath.Should().Be("src/A.cs");
        stored.NewContent.Should().Be("// after");
        stored.BaseSha256.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Propose_change_returns_error_for_missing_arguments()
    {
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>());
        var tool = new ForgeProposeChangeTool(_store);
        var args = JsonSerializer.SerializeToElement(new { target_path = "", new_content = "", summary = "" });
        var result = await tool.InvokeAsync(new ToolCall(tool.Id, args), snap, default);
        var json = JsonSerializer.Serialize(result.Payload);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Check_pr_returns_status_for_known_id_and_error_for_missing()
    {
        var saved = _store.Add(new ChangeProposal { Id = "abc", TargetPath = "src/X", NewContent = "x", Summary = "s" });
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>());
        var tool = new ForgeCheckPrTool(_store);

        var ok = await tool.InvokeAsync(new ToolCall(tool.Id, JsonSerializer.SerializeToElement(new { id = saved.Id })), snap, default);
        var okDoc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Payload));
        okDoc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        okDoc.RootElement.GetProperty("status").GetString().Should().Be("Proposed");

        var miss = await tool.InvokeAsync(new ToolCall(tool.Id, JsonSerializer.SerializeToElement(new { id = "nope" })), snap, default);
        var missDoc = JsonDocument.Parse(JsonSerializer.Serialize(miss.Payload));
        missDoc.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
    }

    /// <summary>
    /// Low-trust mode blocks <c>repo.fs.write</c> to <c>src/</c>, but <c>forge.propose_change</c>
    /// still records an operator-reviewable proposal (full mediated-write story).
    /// </summary>
    [Fact]
    public async Task Passive_mode_blocks_src_write_forge_propose_change_still_succeeds()
    {
        var policy = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["RepoRoot"] = _repoRoot });
        var directWrite = new ToolCall("repo.fs.write", JsonSerializer.SerializeToElement(new { path = "src/Blocked.cs", content = "// no" }));
        policy.Approve(directWrite, snap, out var deny).Should().BeFalse();
        deny.Should().Contain("forge.propose_change");

        var existing = Path.Combine(_repoRoot, "src", "Mediated.cs");
        File.WriteAllText(existing, "// base");
        var forge = new ForgeProposeChangeTool(_store, agentIdProvider: () => "planner");
        var args = JsonSerializer.SerializeToElement(new
        {
            target_path = "src/Mediated.cs",
            new_content = "// proposed",
            summary = "phase2 forge",
            reason = "smoke"
        });
        var result = await forge.InvokeAsync(new ToolCall(forge.Id, args), snap, default);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(result.Payload));
        doc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        var id = doc.RootElement.GetProperty("id").GetString()!;
        _store.Find(id)!.TargetPath.Should().Be("src/Mediated.cs");
        _store.Find(id)!.NewContent.Should().Be("// proposed");
    }

    [Fact]
    public async Task Forge_build_matches_dotnet_build_contract_on_disk()
    {
        var projDir = Path.Combine(_repoRoot, "App");
        Directory.CreateDirectory(projDir);
        File.WriteAllText(
            Path.Combine(projDir, "App.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(projDir, "Program.cs"), "System.Console.WriteLine(1);");
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["RepoRoot"] = _repoRoot });
        var tool = new ForgeBuildTool();
        var result = await tool.InvokeAsync(
            new ToolCall(tool.Id, JsonSerializer.SerializeToElement(new { root = projDir })),
            snap,
            default);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(result.Payload));
        doc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
    }
}
