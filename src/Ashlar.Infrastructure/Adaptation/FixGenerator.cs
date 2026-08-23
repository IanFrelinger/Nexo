using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Infrastructure.Adaptation;

/// <summary>
/// Generates candidate fixes from failure context. Rule-based heuristics.
/// </summary>
public sealed class FixGenerator : IFixGenerator
{
    /// <inheritdoc />
    public Task<IReadOnlyList<BrickManifest>> GenerateFixesAsync(
        FailureContext context,
        BrickManifest? baseManifest = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fixes = new List<BrickManifest>();

        if (baseManifest != null && context.FailureType == "EmptyCatch")
        {
            fixes.Add(baseManifest with
            {
                Config = new Dictionary<string, object>(baseManifest.Config)
                {
                    ["fix:emptyCatch"] = "Add logging in catch blocks",
                },
            });
        }

        if (baseManifest != null && context.FailureType == "MissingOutput")
        {
            var inputs = baseManifest.Interface.Inputs.ToList();
            fixes.Add(baseManifest with
            {
                Config = new Dictionary<string, object>(baseManifest.Config)
                {
                    ["fix:missingOutput"] = "Verify output mapping",
                },
            });
        }

        if (fixes.Count == 0 && baseManifest != null)
        {
            fixes.Add(baseManifest with
            {
                Version = IncrementVersion(baseManifest.Version),
                Config = new Dictionary<string, object>(baseManifest.Config)
                {
                    ["fix:generic"] = context.Message ?? "Unknown fix",
                },
            });
        }

        return Task.FromResult<IReadOnlyList<BrickManifest>>(fixes);
    }

    private static string IncrementVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length >= 3 && int.TryParse(parts[^1], out var patch))
            return string.Join(".", parts[..^1]) + "." + (patch + 1);
        return version + ".1";
    }
}
