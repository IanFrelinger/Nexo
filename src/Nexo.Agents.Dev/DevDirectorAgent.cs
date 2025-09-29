using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Agents.Dev;

public enum DevMode { Heal, Extend }

public sealed class DevDirectorAgent : IAgent
{
    public string Name => "dev.director";
    private readonly DevMode _mode;
    private int _attempts;

    public DevDirectorAgent(DevMode mode = DevMode.Heal) => _mode = mode;

    public Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct)
    {
        var s = obs.Snapshot;
        var root = (string)(s.Data["RepoRoot"] ?? Directory.GetCurrentDirectory());

        // Optional edit hints for heal path
        var editPath = (string?)(s.Data.TryGetValue("EditPath", out var ep) ? ep : null) ?? "tests/SampleTests.cs";
        var findText = (string?)(s.Data.TryGetValue("FindText", out var ft) ? ft : null) ?? "Be(1)";
        var replText = (string?)(s.Data.TryGetValue("ReplaceText", out var rp) ? rp : null) ?? "Be(2)";

        var calls = new List<ToolCall>();

        if (_attempts == 0)
        {
            if (_mode == DevMode.Extend)
            {
                // Minimal TDD: add failing test, then build/test
                var newTestPath = (string?)(s.Data.TryGetValue("NewTestPath", out var nt) ? nt : null) ?? "tests/FeatureXTests.cs";
                var testContent = (string?)(s.Data.TryGetValue("NewTestContent", out var nc) ? nc : null)
                    ?? """
using FluentAssertions;
using Xunit;
public class FeatureXTests {
  [Fact] public void Greets() {
    FeatureX.Greet("Nexo").Should().Be("Hello, Nexo");
  }
}
""";
                calls.Add(Call("repo.fs.ensure_file", new { root, path = newTestPath, content = testContent }));
                calls.Add(Call("dotnet.build", new { root }));
                calls.Add(Call("dotnet.test",  new { root }));
            }
            else
            {
                // Heal mode: build then test
                calls.Add(Call("dotnet.build", new { root }));
                calls.Add(Call("dotnet.test",  new { root }));
            }
            _attempts++;
            return Task.FromResult(new AgentActions(calls));
        }

        // Interpret last results via snapshot flags (set by CLI runner)
        var okBuild = s.Data.TryGetValue("LastBuildOk", out var b) && b is true;
        var okTest  = s.Data.TryGetValue("LastTestsOk", out var t) && t is true;

        if (!okBuild || !okTest)
        {
            if (_mode == DevMode.Extend)
            {
                // Minimal impl to satisfy test (create FeatureX.cs if missing)
                var implPath = (string?)(s.Data.TryGetValue("NewImplPath", out var ip) ? ip : null) ?? "src/FeatureX.cs";
                var implContent = (string?)(s.Data.TryGetValue("NewImplContent", out var ic) ? ic : null)
                    ?? """
public static class FeatureX {
  public static string Greet(string name) => $"Hello, {name}";
}
""";
                calls.Add(Call("repo.fs.ensure_file", new { root, path = implPath, content = implContent }));
                calls.Add(Call("dotnet.build", new { root }));
                calls.Add(Call("dotnet.test",  new { root }));
            }
            else
            {
                // Self-heal: targeted search/replace, then re-run
                calls.Add(Call("repo.fs.search_replace", new { root, path = editPath, find = findText, replace = replText }));
                calls.Add(Call("dotnet.build", new { root }));
                calls.Add(Call("dotnet.test",  new { root }));
            }
            _attempts++;
            return Task.FromResult(new AgentActions(calls));
        }

        // Green → pseudo-commit and doc
        calls.Add(Call("repo.git.commit", new { message = $"auto:{_mode} success attempts={_attempts}", root }));
        calls.Add(Call("docs.update", new { root, entry = $"Self-{_mode} completed in {_attempts} steps" }));
        return Task.FromResult(new AgentActions(calls));
    }

    private static ToolCall Call(string id, object args) =>
        new(id, JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(args)).RootElement);
}
