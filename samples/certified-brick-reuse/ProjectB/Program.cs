using System.Reflection;
using System.Text.Json;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Certified.DamageResolver;

namespace CertifiedBrickReuse.ProjectB;

/// <summary>Program.</summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var brickSourcePath = args.ElementAtOrDefault(0)
            /// <summary>Invalid operation exception.</summary>
            /// <param name="<path-to-certification-record.json>""><path-to-certification-record.json>".</param>
            ?? throw new InvalidOperationException("Usage: ProjectB <path-to-DamageResolverBrick.cs> <path-to-certification-record.json>");
        var recordPath = args.ElementAtOrDefault(1)
            /// <summary>Invalid operation exception.</summary>
            /// <param name="<path-to-certification-record.json>""><path-to-certification-record.json>".</param>
            ?? throw new InvalidOperationException("Usage: ProjectB <path-to-DamageResolverBrick.cs> <path-to-certification-record.json>");

        var source = await File.ReadAllTextAsync(brickSourcePath).ConfigureAwait(false);
        var recordJson = await File.ReadAllTextAsync(recordPath).ConfigureAwait(false);
        var record = JsonSerializer.Deserialize<CertificationRecordData>(
            recordJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            /// <summary>Invalid operation exception.</summary>
            /// <param name="JSON."">Json.".</param>
            ?? throw new InvalidOperationException("Invalid certification record JSON.");

        var artifactPath = args.ElementAtOrDefault(2);
        CertificationTrustResult trust;
        if (!string.IsNullOrWhiteSpace(artifactPath))
        {
            var artifactBytes = await File.ReadAllBytesAsync(artifactPath).ConfigureAwait(false);
            trust = CertificationTrustVerifier.Verify(
                record,
                source,
                artifactBytes,
                options: CertificationVerifyOptions.Strict);
        }
        else
        {
            trust = CertificationTrustVerifier.Verify(
                record,
                source,
                options: CertificationVerifyOptions.Strict);
        }
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
