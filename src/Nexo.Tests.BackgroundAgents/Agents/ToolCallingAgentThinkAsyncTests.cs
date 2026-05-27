using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Agents;
using Nexo.Runtime;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Agents;

public sealed class ToolCallingAgentThinkAsyncTests
{
    [Fact]
    public async Task ThinkAsync_returns_none_when_no_tools_available()
    {
        var model = new MockModel("unused");
        var agent = new ToolCallingAgent("planner", model, NullLogger<ToolCallingAgent>.Instance);
        var obs = new AgentObservation(WorldSnapshot.ForRepo("/repo", "/repo/out"));

        var actions = await agent.ThinkAsync(obs, new EmptyToolbox(), new InMemoryAgentMemory(), CancellationToken.None);

        actions.ToolCalls.Should().BeEmpty();
        model.CompleteCount.Should().Be(0);
    }

    [Fact]
    public async Task ThinkAsync_parses_markdown_json_and_returns_tool_calls()
    {
        var model = new MockModel("""
            ```json
            {"tool_calls":[{"id":"noop","arguments":{"q":"a"}}],"rationale":"list first"}
            ```
            """);
        var (tools, noop) = BuildHarness();
        var agent = new ToolCallingAgent(
            "planner",
            model,
            NullLogger<ToolCallingAgent>.Instance,
            objective: "inspect repo",
            modelProvider: "ollama",
            modelName: "llama3");

        var actions = await agent.ThinkAsync(
            new AgentObservation(WorldSnapshot.ForRepo("/repo", "/repo/out")),
            tools,
            new InMemoryAgentMemory(),
            CancellationToken.None);

        actions.ToolCalls.Should().ContainSingle(c => c.Id == "noop");
        noop.Invocations.Should().Be(0);
        model.LastInput!.Messages.Should().Contain(m => m.content.Contains("nexo.model.provider=ollama"));
        model.LastInput.Messages.Should().Contain(m => m.content.Contains("inspect repo"));
    }

    [Fact]
    public async Task ThinkAsync_returns_none_on_model_failure_and_records_memory()
    {
        var model = new ThrowingModel();
        var (tools, _) = BuildHarness();
        var memory = new InMemoryAgentMemory();
        var agent = new ToolCallingAgent("planner", model, NullLogger<ToolCallingAgent>.Instance);

        var actions = await agent.ThinkAsync(
            new AgentObservation(WorldSnapshot.ForRepo("/repo", "/repo/out")),
            tools,
            memory,
            CancellationToken.None);

        actions.ToolCalls.Should().BeEmpty();
        memory.Events.Should().ContainSingle(e => e.EventType == "think.error");
    }

    [Fact]
    public void Constructor_rejects_null_name_or_model()
    {
        var actName = () => new ToolCallingAgent(null!, new MockModel("{}"), NullLogger<ToolCallingAgent>.Instance);
        var actModel = () => new ToolCallingAgent("x", null!, NullLogger<ToolCallingAgent>.Instance);
        actName.Should().Throw<ArgumentNullException>();
        actModel.Should().Throw<ArgumentNullException>();
    }

    private static (CapabilityRegistry tools, RecordingTool noop) BuildHarness()
    {
        var noop = new RecordingTool();
        var tb = new CapabilityRegistry();
        tb.Register(noop);
        return (tb, noop);
    }

    private sealed class EmptyToolbox : IToolbox
    {
        public IEnumerable<ToolSchema> Schemas() => Array.Empty<ToolSchema>();
        public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct) =>
            throw new NotSupportedException();
        public IAgentMemory MemoryFor(IAgent agent) => new InMemoryAgentMemory();
    }

    private sealed class InMemoryAgentMemory : IAgentMemory
    {
        public List<EventRecord> Events { get; } = new();
        public void Write(EventRecord record) => Events.Add(record);
        public IReadOnlyList<EventRecord> Query(string filter, int k) =>
            Events.Where(e => e.EventType.Contains(filter, StringComparison.Ordinal)).Take(k).ToList();
    }

    private sealed class MockModel : IModel
    {
        private readonly string _text;
        public int CompleteCount { get; private set; }
        public ModelInput? LastInput { get; private set; }

        public MockModel(string text) => _text = text;

        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
        {
            CompleteCount++;
            LastInput = input;
            return Task.FromResult(new ModelOutput(_text));
        }
    }

    private sealed class ThrowingModel : IModel
    {
        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct) =>
            throw new InvalidOperationException("model down");
    }

    private sealed class RecordingTool : ITool
    {
        public int Invocations;
        public string Id => "noop";
        public ToolSchema Schema => new(Id, "noop tool", "{\"type\":\"object\"}");
        public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
        {
            Invocations++;
            var delta = new SimpleDelta(s.Tick, s.Tick + 1, "noop");
            return Task.FromResult(new ToolResult(delta, new { ok = true }));
        }
    }

    private sealed class SimpleDelta : IActionDelta
    {
        public SimpleDelta(int from, int to, params string[] log)
        {
            TickFrom = from;
            TickTo = to;
            _log = log.ToList();
        }

        private readonly List<string> _log;
        public int TickFrom { get; }
        public int TickTo { get; }
        public IReadOnlyList<string> Log => _log;
        public IReadOnlyList<byte>? Signature { get; set; }
    }
}
