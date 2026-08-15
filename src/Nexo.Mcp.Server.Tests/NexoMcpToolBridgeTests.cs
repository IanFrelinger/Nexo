using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Nexo.Abstractions;
using Xunit;

namespace Nexo.Mcp.Server.Tests;

public sealed class NexoMcpToolBridgeTests
{
    private sealed class FakeContributor : IMcpToolContributor
    {
        private readonly List<NexoMcpToolDescriptor> _descriptors = new();
        private readonly Func<string, JsonElement, Task<NexoMcpToolOutcome>> _invoke;

        public FakeContributor(Func<string, JsonElement, Task<NexoMcpToolOutcome>>? invoke = null)
            => _invoke = invoke ?? ((id, _) => Task.FromResult(new NexoMcpToolOutcome(false, $"ran {id}")));

        public JsonElement? LastArguments { get; private set; }

        public FakeContributor Offering(string mcpName, string canonicalId, string description = "d")
        {
            _descriptors.Add(new NexoMcpToolDescriptor(mcpName, canonicalId, description, Json.Object("""{"type":"object"}""")));
            return this;
        }

        public Task<IReadOnlyList<NexoMcpToolDescriptor>> DescribeAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<NexoMcpToolDescriptor>>(_descriptors);

        public Task<NexoMcpToolOutcome> InvokeAsync(string canonicalId, JsonElement arguments, CancellationToken ct)
        {
            LastArguments = arguments;
            return _invoke(canonicalId, arguments);
        }
    }

    private sealed class RecordingGate : IMcpInvocationGate
    {
        private readonly McpGateDecision _decision;

        public RecordingGate(McpGateDecision? decision = null) => _decision = decision ?? McpGateDecision.Allow();

        public ToolCall? LastCall { get; private set; }

        public McpGateDecision Authorize(ToolCall call, WorldSnapshot snapshot)
        {
            LastCall = call;
            return _decision;
        }
    }

    private sealed class FixedSnapshotFactory : IMcpWorldSnapshotFactory
    {
        public WorldSnapshot Create(string canonicalToolId) => WorldSnapshot.ForRepo("X:/repo");
    }

    private static NexoMcpToolBridge Create(
        IEnumerable<IMcpToolContributor> contributors,
        IMcpInvocationGate? gate = null,
        NexoMcpServerOptions? options = null)
        => new(
            contributors,
            gate ?? new RecordingGate(),
            new FixedSnapshotFactory(),
            Options.Create(options ?? new NexoMcpServerOptions()),
            NullLogger<NexoMcpToolBridge>.Instance);

    private static CallToolRequestParams Params(string name, Dictionary<string, JsonElement>? args = null)
        => new() { Name = name, Arguments = args };

    [Fact]
    public async Task ListTools_projects_descriptors_onto_protocol_tools()
    {
        var bridge = Create(new[] { new FakeContributor().Offering("repo_fs_read", "repo.fs.read", "reads") });

        var result = await bridge.ListToolsAsync(CancellationToken.None);

        var tool = result.Tools.Should().ContainSingle().Subject;
        tool.Name.Should().Be("repo_fs_read");
        tool.Title.Should().Be("repo.fs.read", "sanitized names surface the canonical id as display title");
        tool.Description.Should().Be("reads");
    }

    [Fact]
    public async Task ListTools_title_is_omitted_when_name_is_unchanged()
    {
        var bridge = Create(new[] { new FakeContributor().Offering("echo", "echo") });

        var result = await bridge.ListToolsAsync(CancellationToken.None);

        result.Tools.Single().Title.Should().BeNull();
    }

    [Fact]
    public async Task Name_collisions_across_contributors_fail_validation()
    {
        var bridge = Create(new IMcpToolContributor[]
        {
            new FakeContributor().Offering("repo_fs_read", "repo.fs.read"),
            new FakeContributor().Offering("repo_fs_read", "repo.fs_read"),
        });

        var act = () => bridge.ValidateCatalogAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*repo_fs_read*", "ambiguous catalogs must not boot");
    }

    [Fact]
    public async Task Unknown_tool_is_a_protocol_error()
    {
        var bridge = Create(new[] { new FakeContributor().Offering("known", "known") });

        var act = () => bridge.CallToolAsync(Params("unknown"), user: null, CancellationToken.None).AsTask();

        (await act.Should().ThrowAsync<McpProtocolException>())
            .Which.ErrorCode.Should().Be(McpErrorCode.InvalidParams);
    }

    [Fact]
    public async Task Gate_denial_returns_isError_result_not_protocol_error()
    {
        var gate = new RecordingGate(McpGateDecision.Deny("policy said no"));
        var bridge = Create(new[] { new FakeContributor().Offering("echo", "echo") }, gate);

        var result = await bridge.CallToolAsync(Params("echo"), user: null, CancellationToken.None);

        result.IsError.Should().BeTrue();
        var text = result.Content.OfType<TextContentBlock>().Single().Text;
        text.Should().Contain("policy said no");
    }

    [Fact]
    public async Task Gate_sees_the_canonical_tool_id_not_the_sanitized_name()
    {
        var gate = new RecordingGate();
        var bridge = Create(new[] { new FakeContributor().Offering("repo_fs_read", "repo.fs.read") }, gate);

        await bridge.CallToolAsync(Params("repo_fs_read"), user: null, CancellationToken.None);

        gate.LastCall!.Id.Should().Be("repo.fs.read", "policies are written against canonical Nexo ids");
    }

    [Fact]
    public async Task Success_with_structured_payload_sets_structured_content()
    {
        var contributor = new FakeContributor((_, _) => Task.FromResult(
            new NexoMcpToolOutcome(false, """{"answer":42}""", new { answer = 42 })))
            .Offering("echo", "echo");
        var bridge = Create(new[] { contributor });

        var result = await bridge.CallToolAsync(Params("echo"), user: null, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Content.OfType<TextContentBlock>().Single().Text.Should().Contain("42");
        result.StructuredContent.HasValue.Should().BeTrue();
        result.StructuredContent!.Value.GetProperty("answer").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task Success_with_plain_text_has_no_structured_content()
    {
        var contributor = new FakeContributor((_, _) => Task.FromResult(
            new NexoMcpToolOutcome(false, "plain text"))).Offering("echo", "echo");
        var bridge = Create(new[] { contributor });

        var result = await bridge.CallToolAsync(Params("echo"), user: null, CancellationToken.None);

        result.StructuredContent.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task Contributor_crash_returns_generic_isError_result()
    {
        var contributor = new FakeContributor((_, _) => throw new InvalidOperationException("secret internals"))
            .Offering("echo", "echo");
        var bridge = Create(new[] { contributor });

        var result = await bridge.CallToolAsync(Params("echo"), user: null, CancellationToken.None);

        result.IsError.Should().BeTrue();
        var text = result.Content.OfType<TextContentBlock>().Single().Text;
        text.Should().NotContain("secret internals", "internal details stay in the host log");
    }

    [Fact]
    public async Task Argument_overrides_replace_caller_supplied_values()
    {
        var options = new NexoMcpServerOptions();
        options.ArgumentOverrides["repo.fs.read"] = new Dictionary<string, string> { ["root"] = "X:/pinned" };
        var contributor = new FakeContributor().Offering("repo_fs_read", "repo.fs.read");
        var bridge = Create(new[] { contributor }, options: options);

        var args = new Dictionary<string, JsonElement>
        {
            ["root"] = Json.Object("""{"ignored":true}"""),
            ["path"] = JsonSerializer.SerializeToElement("readme.md"),
        };
        await bridge.CallToolAsync(Params("repo_fs_read", args), user: null, CancellationToken.None);

        var seen = contributor.LastArguments!.Value;
        seen.GetProperty("root").GetString().Should().Be("X:/pinned", "operator pins beat caller-supplied values");
        seen.GetProperty("path").GetString().Should().Be("readme.md", "untouched arguments pass through");
    }

    [Fact]
    public async Task Null_arguments_become_empty_object_and_still_receive_overrides()
    {
        var options = new NexoMcpServerOptions();
        options.ArgumentOverrides["echo"] = new Dictionary<string, string> { ["root"] = "X:/pinned" };
        var contributor = new FakeContributor().Offering("echo", "echo");
        var bridge = Create(new[] { contributor }, options: options);

        await bridge.CallToolAsync(Params("echo"), user: null, CancellationToken.None);

        contributor.LastArguments!.Value.GetProperty("root").GetString().Should().Be("X:/pinned");
    }
}
