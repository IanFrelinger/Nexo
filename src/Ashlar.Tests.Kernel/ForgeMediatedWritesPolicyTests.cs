using System.Text.Json;
using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Policies.Dev;
using Xunit;

namespace Ashlar.Tests.Kernel;

public class ForgeMediatedWritesPolicyTests
{
    private static ToolCall Write(string path) =>
        new("repo.fs.write", JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["path"] = path })).RootElement);
    private static ToolCall SearchReplace(string path) =>
        new("repo.fs.search_replace", JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["path"] = path })).RootElement);
    private static ToolCall Other(string id) => new(id, JsonDocument.Parse("{}").RootElement);

    private static readonly WorldSnapshot Snap = new(0, new Dictionary<string, object?>());

    [Fact]
    public void Constructor_rejects_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new ForgeMediatedWritesPolicy((IAggressivenessModeStore)null!));
        Assert.Throws<ArgumentNullException>(() => new ForgeMediatedWritesPolicy((Func<BackgroundAgentAggressivenessMode>)null!));
    }

    [Fact]
    public void Non_write_calls_always_pass()
    {
        var p = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        p.Approve(Other("dotnet.build"), Snap, out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Theory]
    [InlineData(BackgroundAgentAggressivenessMode.Active)]
    [InlineData(BackgroundAgentAggressivenessMode.Ambient)]
    public void High_trust_modes_are_no_ops(BackgroundAgentAggressivenessMode mode)
    {
        var p = new ForgeMediatedWritesPolicy(() => mode);
        p.Approve(Write("src/Foo.cs"), Snap, out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Theory]
    [InlineData(BackgroundAgentAggressivenessMode.Passive)]
    [InlineData(BackgroundAgentAggressivenessMode.SemiActive)]
    public void Low_trust_modes_block_source_writes(BackgroundAgentAggressivenessMode mode)
    {
        var p = new ForgeMediatedWritesPolicy(() => mode);
        p.Approve(Write("src/Foo.cs"), Snap, out var r).Should().BeFalse();
        r.Should().Contain("forge-mediated writes");
        p.Approve(SearchReplace("tests/Bar.cs"), Snap, out var r2).Should().BeFalse();
        r2.Should().Contain("forge-mediated writes");
    }

    [Fact]
    public void Low_trust_modes_allow_non_source_paths()
    {
        var p = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        p.Approve(Write("docs/readme.md"), Snap, out _).Should().BeTrue();
        p.Approve(Write(".ashlar/state.json"), Snap, out _).Should().BeTrue();
    }

    [Fact]
    public void Empty_or_missing_path_passes()
    {
        var p = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        var emptyPath = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"path":""}""").RootElement);
        var missingPath = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"other":"x"}""").RootElement);
        var notObject = new ToolCall("repo.fs.write", JsonDocument.Parse("[]").RootElement);
        var nonStringPath = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"path":123}""").RootElement);
        p.Approve(emptyPath, Snap, out _).Should().BeTrue();
        p.Approve(missingPath, Snap, out _).Should().BeTrue();
        p.Approve(notObject, Snap, out _).Should().BeTrue();
        p.Approve(nonStringPath, Snap, out _).Should().BeTrue();
    }

    [Fact]
    public void Mode_store_is_consulted_on_each_call()
    {
        var store = new Mock<IAggressivenessModeStore>();
        store.SetupSequence(s => s.GetMode())
            .Returns(BackgroundAgentAggressivenessMode.Active)
            .Returns(BackgroundAgentAggressivenessMode.Passive);

        var p = new ForgeMediatedWritesPolicy(store.Object);
        p.Approve(Write("src/A.cs"), Snap, out _).Should().BeTrue();
        p.Approve(Write("src/A.cs"), Snap, out var r).Should().BeFalse();
        r.Should().Contain("forge-mediated writes");
    }
}
