using System.Globalization;
using Ashlar.BackgroundAgents.Configuration;

namespace Ashlar.BackgroundAgents.Extending;

/// <summary>
/// SX-AUDIT invariant D — the cross-cycle ceilings on extender-triggered extension. Enforced by
/// <c>BackgroundAgentRegistry</c> at the same point invariant C is (before the self-extend runner
/// is invoked), so an extender that is Active still cannot extend without bound.
///
/// <para>Three ceilings, each refusing a cycle on its own:</para>
/// <list type="bullet">
///   <item><description><see cref="MaxLineageDepth"/> — recursion. How many <c>ParentId</c> hops
///   below a human-authored root a machine-spawned extender may sit and still run extend cycles.
///   Extension that spawns extenders that extend is exactly the runaway this bounds.</description></item>
///   <item><description><see cref="MaxUnattendedCycles"/> — runaway. Extend cycles an extender may
///   run since a human last armed it before it holds. Re-arm is an operator act: process
///   restart or <c>BackgroundAgentRegistry.RearmExtension</c>; re-registration does NOT re-arm,
///   because agents can re-register themselves.</description></item>
///   <item><description><see cref="MaxCyclesPerHour"/> — rate. Extend cycles in any trailing hour.</description></item>
/// </list>
///
/// <para>Overrides may only LOWER a ceiling, never raise it — the same posture as the certified
/// loop's <c>RecursionDiscipline</c>: the environment may be stricter than this file, and an
/// agent's own <c>Parameters</c> may be stricter than the environment, so no configuration an
/// agent can write extends its own authority (I-1). Raising a default is a code change.</para>
/// </summary>
public sealed record ExtensionCeiling(int MaxLineageDepth, int MaxUnattendedCycles, int MaxCyclesPerHour)
{
    /// <summary>Environment variable that may LOWER <see cref="MaxLineageDepth"/>.</summary>
    public const string LineageDepthEnvVar = "ASHLAR_EXTENSION_MAX_LINEAGE_DEPTH";

    /// <summary>Environment variable that may LOWER <see cref="MaxUnattendedCycles"/>.</summary>
    public const string UnattendedCyclesEnvVar = "ASHLAR_EXTENSION_MAX_UNATTENDED_CYCLES";

    /// <summary>Environment variable that may LOWER <see cref="MaxCyclesPerHour"/>.</summary>
    public const string CyclesPerHourEnvVar = "ASHLAR_EXTENSION_MAX_CYCLES_PER_HOUR";

    /// <summary>Per-agent <c>Parameters</c> key that may LOWER <see cref="MaxLineageDepth"/>.</summary>
    public const string LineageDepthParameter = "MaxLineageDepth";

    /// <summary>Per-agent <c>Parameters</c> key that may LOWER <see cref="MaxUnattendedCycles"/>.</summary>
    public const string UnattendedCyclesParameter = "MaxUnattendedCycles";

    /// <summary>Per-agent <c>Parameters</c> key that may LOWER <see cref="MaxCyclesPerHour"/>.</summary>
    public const string CyclesPerHourParameter = "MaxCyclesPerHour";

    /// <summary>The trailing window <see cref="MaxCyclesPerHour"/> is measured over.</summary>
    public static readonly TimeSpan RateWindow = TimeSpan.FromHours(1);

    /// <summary>
    /// The defaults. Roots and their direct children may extend; grandchildren may not. Eight
    /// unattended cycles, then hold for a human. Four cycles in any hour. Changing these
    /// constants is a reviewed code change, deliberately.
    /// </summary>
    public static ExtensionCeiling Default { get; } = new(MaxLineageDepth: 1, MaxUnattendedCycles: 8, MaxCyclesPerHour: 4);

    /// <summary>The defaults, lowered by whatever the environment sets — never raised.</summary>
    public static ExtensionCeiling Resolve() => Resolve(Environment.GetEnvironmentVariable);

    /// <summary>Testable form of <see cref="Resolve()"/>.</summary>
    public static ExtensionCeiling Resolve(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return Default.LoweredBy(
            ParseNonNegative(environment(LineageDepthEnvVar)),
            ParseNonNegative(environment(UnattendedCyclesEnvVar)),
            ParseNonNegative(environment(CyclesPerHourEnvVar)));
    }

    /// <summary>
    /// This ceiling lowered by an agent's own <c>Parameters</c>. An agent may ask for less
    /// authority than the deployment grants; a value above the current ceiling is ignored.
    /// </summary>
    public ExtensionCeiling NarrowedBy(BackgroundAgentConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return LoweredBy(
            ReadParameter(config, LineageDepthParameter),
            ReadParameter(config, UnattendedCyclesParameter),
            ReadParameter(config, CyclesPerHourParameter));
    }

    private ExtensionCeiling LoweredBy(int? lineageDepth, int? unattendedCycles, int? cyclesPerHour) => new(
        Lower(MaxLineageDepth, lineageDepth),
        Lower(MaxUnattendedCycles, unattendedCycles),
        Lower(MaxCyclesPerHour, cyclesPerHour));

    private static int Lower(int current, int? candidate) =>
        candidate is { } c && c >= 0 && c < current ? c : current;

    private static int? ReadParameter(BackgroundAgentConfig config, string key)
    {
        if (config.Parameters is null || !config.Parameters.TryGetValue(key, out var raw) || raw is null)
            return null;
        return raw switch
        {
            int i => i,
            long l when l <= int.MaxValue && l >= int.MinValue => (int)l,
            string s => ParseNonNegative(s),
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Number } je when je.TryGetInt32(out var n) => n,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } je => ParseNonNegative(je.GetString()),
            IConvertible conv => SafeConvert(conv),
            _ => null,
        };
    }

    private static int? SafeConvert(IConvertible value)
    {
        try
        {
            return value.ToInt32(CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    private static int? ParseNonNegative(string? raw) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? value
            : null;
}
