using System.Text.Json;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification;

internal static class WitnessRunner
{
    public static async Task<WitnessRunResult> RunAsync(
        Brick brick,
        WitnessSpec witness,
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        var failures = new List<string>();

        foreach (var (caseIndex, witnessCase) in witness.Cases.Select((c, i) => (i, c)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var input = new BrickInput(witnessCase.Input);
            BrickOutput output;
            try
            {
                output = await brick.ExecuteAsync(
                    input,
                    ImplementationType.Deterministic,
                    context,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                failures.Add($"case {caseIndex}: execution threw {ex.Message}");
                continue;
            }

            foreach (var (key, expected) in witnessCase.ExpectedOutput)
            {
                if (!output.ToDictionary().TryGetValue(key, out var actual))
                {
                    failures.Add($"case {caseIndex}: missing output key '{key}'");
                    continue;
                }

                if (!ValuesEqual(expected, actual))
                {
                    failures.Add(
                        $"case {caseIndex}: output['{key}'] expected {FormatValue(expected)} got {FormatValue(actual)}");
                }
            }
        }

        return new WitnessRunResult(failures.Count == 0, failures);
    }

    public static async Task<(bool Identical, string? First, string? Second)> CheckDeterminismAsync(
        Brick brick,
        WitnessSpec witness,
        CancellationToken cancellationToken)
    {
        if (witness.Cases.Count == 0)
            return (true, null, null);

        var auditContext = new AuditExecutionContext();
        var probe = witness.Cases[0];
        var input = new BrickInput(probe.Input);

        var first = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            auditContext,
            cancellationToken).ConfigureAwait(false);
        var second = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            auditContext,
            cancellationToken).ConfigureAwait(false);

        var firstJson = BrickOutputSerializer.ToCanonicalJson(first);
        var secondJson = BrickOutputSerializer.ToCanonicalJson(second);
        return (firstJson == secondJson, firstJson, secondJson);
    }

    private static bool ValuesEqual(object expected, object actual)
    {
        if (expected is JsonElement expectedEl && actual is not JsonElement)
            return ValuesEqual(FromJsonElement(expectedEl), actual);
        if (actual is JsonElement actualEl && expected is not JsonElement)
            return ValuesEqual(expected, FromJsonElement(actualEl));

        if (expected is int or long or short or byte)
        {
            try
            {
                return Convert.ToInt64(expected) == Convert.ToInt64(actual);
            }
            catch
            {
                return false;
            }
        }

        return string.Equals(
            Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    private static object FromJsonElement(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString()!,
        JsonValueKind.Number when el.TryGetInt64(out var l) => l,
        JsonValueKind.Number => el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => el.GetRawText()
    };

    private static string FormatValue(object value) =>
        Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "<null>";
}

internal sealed record WitnessRunResult(bool Passed, IReadOnlyList<string> Failures);

internal sealed class AuditExecutionContext : IExecutionContext
{
    public string AgentId => "cert-gate";
    public string BehaviorId => "cert-gate";
    public bool IsAirGapped => true;
    public bool AuditMode => true;
    public string Provider => "deterministic";
    public IReadOnlyDictionary<string, object> Variables { get; } =
        new Dictionary<string, object>();
}
