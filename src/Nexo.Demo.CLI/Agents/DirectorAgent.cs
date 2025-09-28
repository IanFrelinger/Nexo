using Nexo.Abstractions;
using System.Text.Json;

namespace Nexo.Demo.CLI.Agents;

public sealed class DirectorAgent : IAgent
{
    public string Name { get; }
    private bool _issued;

    public DirectorAgent(string name = "director") => Name = name;

    public Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct)
    {
        if (_issued) return Task.FromResult(AgentActions.None);

        var data = obs.Snapshot.Data;
        var input = data.TryGetValue("InputPath", out var ip) ? ip as string : "";
        var outputRoot = data.TryGetValue("OutputRoot", out var o) ? o as string : "./out";

        var calls = new List<ToolCall>
        {
            new("assembly.analyze", JsonDocument.Parse($$"""{"path":"{{input}}"}""").RootElement),
            new("assembly.decompile", JsonDocument.Parse($$"""{"path":"{{input}}","output":"{{outputRoot}}/decompile"}""").RootElement),
            new("assembly.security_scan", JsonDocument.Parse($$"""{"path":"{{input}}"}""").RootElement),
        };

        _issued = true;
        mem.Write(new EventRecord(DateTimeOffset.UtcNow, Name, "plan", $"analyze->decompile->security_scan for {input}"));
        return Task.FromResult(new AgentActions(calls));
    }
}
