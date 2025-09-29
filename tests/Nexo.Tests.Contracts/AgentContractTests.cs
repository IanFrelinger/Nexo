using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Contracts;

/// <summary>
/// Contract tests for agent behavior
/// </summary>
public class AgentContractTests
{
    [Fact(DisplayName = "Agent operations are idempotent")]
    [Trait(Traits.Category, Traits.Contract)]
    public async Task Idempotent_runs_produce_same_outputs()
    {
        using var ws = new TempWorkspace();
        var input = CreateMinimalValidRequest();
        
        // TODO: call your AgentFactory, inject ws.Path
        var r1 = await RunAgent(input, ws.Path);
        var r2 = await RunAgent(input, ws.Path);
        
        Normalize(r1.Output).Should().Be(Normalize(r2.Output));
    }

    [Fact(DisplayName = "Agent enforces timeouts")]
    [Trait(Traits.Category, Traits.Contract)]
    public async Task Enforces_timeout()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            RunAgent(longRunning: true, ct: cts.Token));
    }

    [Fact(DisplayName = "PathAllowlist blocks traversal")]
    [Trait(Traits.Category, Traits.Contract)]
    public async Task Blocks_path_traversal()
    {
        using var ws = new TempWorkspace();
        var outside = System.IO.Path.Combine(ws.Path, "..", "..", "forbidden.txt");
        
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            RunToolWrite(outside, "data"));
    }

    private static string CreateMinimalValidRequest()
    {
        // TODO: Create a minimal valid agent request
        return "{}";
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").Trim();

    // TODO: wire RunAgent/RunToolWrite to your real services
    private static async Task<(string Output, bool Success)> RunAgent(string input, string workspacePath)
    {
        // Placeholder implementation
        await Task.Delay(100);
        return ("placeholder output", true);
    }

    private static async Task RunAgent(bool longRunning, CancellationToken ct)
    {
        if (longRunning)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), ct);
        }
    }

    private static async Task RunToolWrite(string path, string data)
    {
        // Placeholder implementation - should check PathAllowlist
        await Task.Delay(100);
        throw new UnauthorizedAccessException("Path traversal blocked");
    }
}
