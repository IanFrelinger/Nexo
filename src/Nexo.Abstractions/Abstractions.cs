using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Core abstractions for the Nexo agent framework.
/// 
/// Defines the fundamental interfaces and types:
/// - IAgent: Agents that can think and make decisions
/// - ITool: Tools that agents can invoke
/// - IPolicy: Policies that approve/reject tool calls
/// - IModel: LLM models for AI operations
/// - IToolbox: Tool registry and memory provider
/// - IAgentMemory: Agent event storage and retrieval
/// 
/// These abstractions form the foundation of the agent execution model.
/// </summary>

/// <summary>
/// Interface for agents that can observe the world and make decisions.
/// 
/// Agents observe the world through AgentObservation and decide on actions
/// (ToolCalls) using ThinkAsync. They have access to a toolbox and memory.
/// </summary>
public interface IAgent
{
    string Name { get; }
    Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct);
}

/// <summary>
/// Interface for tools that agents can invoke to perform actions.
/// 
/// Tools are capabilities that agents can use to interact with the world.
/// Each tool has:
/// - A unique identifier
/// - A schema describing its inputs
/// - An invocation method that takes a tool call and world state
/// 
/// Tools are registered with IToolbox and can be discovered by agents.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique identifier for this tool.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Schema describing this tool's inputs and behavior.
    /// </summary>
    ToolSchema Schema { get; }
    
    /// <summary>
    /// Invokes the tool with the given call and world state.
    /// </summary>
    /// <param name="toolCall">The tool call containing arguments.</param>
    /// <param name="s">The current world snapshot.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tool result containing the action delta and optional payload.</returns>
    Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct);
}

/// <summary>
/// Interface for policies that approve or reject tool calls.
/// 
/// Policies enforce security and operational constraints by evaluating
/// tool calls against the current world state.
/// </summary>
public interface IPolicy
{
    bool Approve(ToolCall toolCall, WorldSnapshot s, out string reason);
}

/// <summary>
/// Interface for LLM models used for AI operations.
/// 
/// Models take ModelInput (messages) and return ModelOutput (text).
/// Used by agents for reasoning and decision-making.
/// </summary>
public interface IModel
{
    Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct);
}

/// <summary>
/// Represents a delta (change) to the world state resulting from tool execution.
/// 
/// Contains:
/// - Tick range (from/to) indicating when the change occurred
/// - Cryptographic signature for integrity verification
/// - Log messages describing what happened
/// 
/// Action deltas are merged together to form the complete world state change.
/// Used by PolicyEngine for signing and verification.
/// </summary>
public interface IActionDelta
{
    /// <summary>
    /// Starting tick of this delta.
    /// </summary>
    int TickFrom { get; }
    
    /// <summary>
    /// Ending tick of this delta.
    /// </summary>
    int TickTo { get; }
    
    /// <summary>
    /// Cryptographic signature (SHA256 hash) for integrity verification.
    /// </summary>
    IReadOnlyList<byte>? Signature { get; set; }
    
    /// <summary>
    /// Log messages describing the actions taken.
    /// </summary>
    IReadOnlyList<string> Log { get; }
}

/// <summary>
/// Concrete implementation of IActionDelta representing a world state change.
/// 
/// Contains tick range, log messages, and optional cryptographic signature.
/// Provides a static Merge method to combine multiple deltas into one.
/// </summary>
/// <param name="TickFrom">Starting tick of this delta.</param>
/// <param name="TickTo">Ending tick of this delta.</param>
/// <param name="Log">Log messages describing the actions taken.</param>
public sealed record ActionDelta(int TickFrom, int TickTo, IReadOnlyList<string> Log) : IActionDelta
{
    /// <summary>
    /// Cryptographic signature (SHA256 hash) for integrity verification.
    /// </summary>
    public IReadOnlyList<byte>? Signature { get; set; }
    
    /// <summary>
    /// Merges multiple action deltas into a single delta.
    /// Combines tick ranges and concatenates log messages.
    /// </summary>
    /// <param name="deltas">The deltas to merge.</param>
    /// <returns>A merged action delta, or an empty delta if the input is empty.</returns>
    public static IActionDelta Merge(IEnumerable<IActionDelta> deltas)
    {
        var list = deltas.ToList();
        if (list.Count == 0) return new ActionDelta(0, 0, Array.Empty<string>());
        var from = list.Min(d => d.TickFrom);
        var to = list.Max(d => d.TickTo);
        var log = list.SelectMany(d => d.Log).ToList();
        return new ActionDelta(from, to, log);
    }
}

/// <summary>
/// Schema describing a tool's capabilities and inputs.
/// 
/// Contains:
/// - Tool identifier
/// - Human-readable description
/// - JSON schema for input validation
/// 
/// Used by agents to discover and understand available tools.
/// </summary>
/// <param name="Id">Unique identifier for the tool.</param>
/// <param name="Description">Human-readable description of what the tool does.</param>
/// <param name="InputJsonSchema">JSON schema describing the tool's input parameters (null if no schema).</param>
public sealed record ToolSchema(string Id, string Description, string? InputJsonSchema);

/// <summary>
/// Represents a call to a tool with arguments.
/// 
/// Contains:
/// - Tool identifier
/// - Arguments as JSON
/// 
/// Provides a ParseArgs method to deserialize arguments to a specific type.
/// </summary>
/// <param name="Id">Identifier of the tool to call.</param>
/// <param name="Arguments">Tool arguments as a JSON element.</param>
public sealed record ToolCall(string Id, JsonElement Arguments)
{
    /// <summary>
    /// Parses the arguments JSON to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized arguments object.</returns>
    public T ParseArgs<T>() => JsonSerializer.Deserialize<T>(Arguments)!;
}

/// <summary>
/// Result of a tool invocation.
/// 
/// Contains:
/// - Action delta representing the world state change
/// - Optional payload with additional data
/// 
/// Returned by ITool.InvokeAsync after tool execution.
/// </summary>
/// <param name="Delta">The action delta representing the world state change.</param>
/// <param name="Payload">Optional payload with additional tool-specific data.</param>
public sealed record ToolResult(IActionDelta Delta, object? Payload);

/// <summary>
/// Represents actions that an agent wants to take.
/// 
/// Contains a list of tool calls that the agent proposes to execute.
/// Provides a static None property for agents that don't want to take any actions.
/// </summary>
/// <param name="ToolCalls">List of tool calls the agent wants to execute.</param>
public sealed record AgentActions(IReadOnlyList<ToolCall> ToolCalls)
{
    /// <summary>
    /// Represents no actions (empty tool call list).
    /// </summary>
    public static AgentActions None { get; } = new AgentActions(Array.Empty<ToolCall>());
}

/// <summary>
/// Snapshot of the world state at a specific tick.
/// 
/// Contains:
/// - Current tick number
/// - Dictionary of world state data (key-value pairs)
/// 
/// Used by agents to observe the current world state and by tools to access state.
/// </summary>
/// <param name="Tick">The current tick number.</param>
/// <param name="Data">Dictionary containing world state data as key-value pairs.</param>
public sealed record WorldSnapshot(int Tick, IReadOnlyDictionary<string, object?> Data)
{
    /// <summary>
    /// Creates a world snapshot for repo-based tool execution (RepoRoot and OutputRoot).
    /// </summary>
    /// <param name="repoRoot">Repository root path.</param>
    /// <param name="outputRoot">Output root path; if null, uses Path.Combine(repoRoot, "out").</param>
    /// <param name="tick">Tick number (default 0).</param>
    public static WorldSnapshot ForRepo(string repoRoot, string? outputRoot = null, int tick = 0)
    {
        var output = outputRoot ?? Path.Combine(repoRoot, "out");
        return new WorldSnapshot(tick, new Dictionary<string, object?>
        {
            ["RepoRoot"] = repoRoot,
            ["OutputRoot"] = output
        });
    }
}

/// <summary>
/// Represents an agent's observation of the world.
/// 
/// Contains a world snapshot that the agent can use to make decisions.
/// Passed to IAgent.ThinkAsync for agent decision-making.
/// </summary>
/// <param name="Snapshot">The world snapshot the agent is observing.</param>
public sealed record AgentObservation(WorldSnapshot Snapshot);

/// <summary>
/// Record of an event that occurred in the agent system.
/// 
/// Contains:
/// - Timestamp when the event occurred
/// - Agent that generated the event
/// - Event type/category
/// - Event message/description
/// 
/// Stored in IAgentMemory for agent event history and querying.
/// </summary>
/// <param name="At">Timestamp when the event occurred.</param>
/// <param name="Agent">Name or ID of the agent that generated the event.</param>
/// <param name="EventType">Type or category of the event.</param>
/// <param name="Message">Event message or description.</param>
public sealed record EventRecord(DateTimeOffset At, string Agent, string EventType, string Message);

/// <summary>
/// Interface for agent event storage and retrieval.
/// 
/// Agents can write EventRecords and query them using filters.
/// Provides memory capabilities for agents.
/// </summary>
public interface IAgentMemory
{
    void Write(EventRecord e);
    IReadOnlyList<EventRecord> Query(string filter, int k);
}

/// <summary>
/// Interface for tool registry and memory provider.
/// 
/// Provides:
/// - Tool schemas for discovery
/// - Tool invocation capability
/// - Agent memory instances
/// 
/// Used by agents to access tools and memory.
/// </summary>
public interface IToolbox
{
    IEnumerable<ToolSchema> Schemas();
    Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct);
    IAgentMemory MemoryFor(IAgent agent);
}

/// <summary>
/// Input to an LLM model for text generation.
/// 
/// Contains a list of messages with roles (e.g., "system", "user", "assistant")
/// and content. Used by IModel.CompleteAsync for LLM interactions.
/// </summary>
/// <param name="Messages">List of messages, each with a role and content string.</param>
public sealed record ModelInput(IReadOnlyList<(string role, string content)> Messages);

/// <summary>
/// Output from an LLM model after text generation.
/// 
/// Contains the generated text from the model.
/// Returned by IModel.CompleteAsync after LLM processing.
/// </summary>
/// <param name="Text">The generated text output from the model.</param>
public sealed record ModelOutput(string Text);
