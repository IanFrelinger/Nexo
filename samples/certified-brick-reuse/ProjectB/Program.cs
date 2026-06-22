using System.Reflection;
using System.Text.Json;
using Nexo.Certification.Contracts;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using GeneratedBricks;

namespace CertifiedBrickReuse.ProjectB;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var brickSourcePath = args.ElementAtOrDefault(0)
            ?? throw new InvalidOperationException("Usage: ProjectB <path-to-DamageResolverBrick.cs> <path-to-certification-record.json>");
        var recordPath = args.ElementAtOrDefault(1)
            ?? throw new InvalidOperationException("Usage: ProjectB <path-to-DamageResolverBrick.cs> <path-to-certification-record.json>");

        var source = await File.ReadAllTextAsync(brickSourcePath).ConfigureAwait(false);
        var recordJson = await File.ReadAllTextAsync(recordPath).ConfigureAwait(false);
        var record = JsonSerializer.Deserialize<CertificationRecordData>(
            recordJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Invalid certification record JSON.");

        var trust = CertificationTrustVerifier.Verify(record, source);
        if (!trust.Trusted)
        {
            Console.Error.WriteLine($"UNTRUSTED: {trust.FailureCode} — {trust.Reason}");
            return 2;
        }

        var brick = new DamageResolverBrick();
        var output = await brick.ExecuteAsync(
            new BrickInput(new Dictionary<string, object>
            {
                ["baseDamage"] = 50,
                ["critMultiplierPercent"] = 100,
                ["armor"] = 10,
                ["isCrit"] = false
            }),
            ImplementationType.Deterministic,
            new ProjectBExecutionContext()).ConfigureAwait(false);

        Console.WriteLine($"TRUSTED finalDamage={output.Get<int>("finalDamage")}");
        return 0;
    }
}

internal sealed class ProjectBExecutionContext : IExecutionContext
{
    public string AgentId { get; } = "project-b";
    public string BehaviorId { get; } = "certified-brick-smoke";
    public bool IsAirGapped { get; } = true;
    public bool AuditMode { get; } = true;
    public string Provider { get; } = "deterministic";
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}
