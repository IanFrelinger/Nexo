using System.Text.Json;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification.HotSwap;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Judges witness observations for the certification gate. Nothing here EXECUTES a brick: every
/// execution of candidate or mutant code happens in an execution backend (by default the
/// child-process replay in <see cref="LocalProcessExecutionBackend"/>) and comes back as raw
/// observations, which these judges score with the gate's own comparers. The in-process
/// <c>RunAsync</c> that used to live here ran author code on the certifier's threads, where an
/// infinite loop hung the gate and a stack overflow killed it.
/// </summary>
internal static class WitnessRunner
{
    /// <summary>
    /// Judges one unit's raw backend observations — the gate decides, the backend only ran.
    /// Only repeat 0 is judged (later repeats exist for determinism comparison). A case the wall
    /// clock stopped or that killed its process is reported as exactly that, not as a throw: the
    /// candidate produced nothing the repair loop could be shown.
    /// </summary>
    public static WitnessRunResult JudgeObservations(
        WitnessSpec witness,
        IReadOnlyList<CandidateCaseObservation> observations)
    {
        var failures = new List<string>();
        var findings = new List<WitnessFinding>();

        foreach (var (caseIndex, witnessCase) in witness.Cases.Select((c, i) => (i, c)))
        {
            var observation = observations.FirstOrDefault(o => o.CaseIndex == caseIndex && o.Repeat == 0);
            if (observation is null)
            {
                failures.Add($"case {caseIndex}: the execution backend returned no observation");
                findings.Add(new WitnessFinding(caseIndex, WitnessFindingKind.NoObservation));
                continue;
            }

            if (observation.Threw)
            {
                var error = observation.Error ?? string.Empty;
                if (error.StartsWith(ExecutionRunnerMarkers.RunnerCrashPrefix, StringComparison.Ordinal))
                {
                    failures.Add($"case {caseIndex}: {error}");
                    findings.Add(new WitnessFinding(caseIndex, WitnessFindingKind.Crashed, Detail: error));
                    continue;
                }

                if (error.StartsWith(ExecutionRunnerMarkers.ExecutionTimeoutPrefix, StringComparison.Ordinal))
                {
                    failures.Add($"case {caseIndex}: {error}");
                    findings.Add(new WitnessFinding(caseIndex, WitnessFindingKind.TimedOut, Detail: error));
                    continue;
                }

                failures.Add($"case {caseIndex}: execution threw {observation.Error}");
                findings.Add(new WitnessFinding(caseIndex, WitnessFindingKind.Threw, Detail: observation.Error));
                continue;
            }

            var outputs = ProjectObservation(observation);
            foreach (var (key, expected) in witnessCase.ExpectedOutput)
            {
                if (!outputs.TryGetValue(key, out var actual) || actual is null)
                {
                    failures.Add($"case {caseIndex}: missing output key '{key}'");
                    findings.Add(new WitnessFinding(caseIndex, WitnessFindingKind.MissingKey, key, FormatValue(expected)));
                    continue;
                }

                if (!ValuesEqual(expected, actual))
                {
                    failures.Add(
                        $"case {caseIndex}: output['{key}'] expected {FormatValue(expected)} got {FormatValue(actual)}");
                    findings.Add(new WitnessFinding(
                        caseIndex, WitnessFindingKind.Mismatch, key, FormatValue(expected), FormatValue(actual)));
                }
            }
        }

        return new WitnessRunResult(failures.Count == 0, failures, findings);
    }

    /// <summary>
    /// The witness-observable view of a session observation: keyed outputs plus the summary
    /// under <see cref="WitnessObservableOutput.SummaryKey"/> — the same projection the
    /// in-process judges apply, so a witness sees one shape regardless of where the
    /// candidate executed.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> ProjectObservation(CandidateCaseObservation observation)
    {
        var view = new Dictionary<string, object?>(
            observation.Outputs ?? new Dictionary<string, object?>(), StringComparer.Ordinal);
        if (observation.Summary is not null)
            view[WitnessObservableOutput.SummaryKey] = observation.Summary;
        return view;
    }

    /// <summary>
    /// Judges a MUTANT unit's observations: any throw, missing key, or mismatch kills. Actual
    /// values are unwrapped from their JSON transport shape before the comparer so a
    /// transport artifact can never masquerade as a kill — a vacuous kill is the exact
    /// failure mode the mutation gate exists to prevent. WHY a mutant died (witness, wall
    /// clock, process death) is the engine's to classify from the observation markers; this
    /// only says whether it lived.
    /// </summary>
    public static bool JudgeMutantObservations(
        WitnessSpec witness,
        IReadOnlyList<CandidateCaseObservation> observations)
    {
        foreach (var (caseIndex, witnessCase) in witness.Cases.Select((c, i) => (i, c)))
        {
            var observation = observations.FirstOrDefault(o => o.CaseIndex == caseIndex && o.Repeat == 0);
            if (observation is null || observation.Threw || observation.Outputs is null)
                return false;

            foreach (var (key, expected) in witnessCase.ExpectedOutput)
            {
                if (!ProjectObservation(observation).TryGetValue(key, out var actual) || actual is null)
                    return false;

                if (!WitnessValueComparer.AreEqual(expected, UnwrapJson(actual)))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Canonicalizes one observation for determinism comparison: summary plus sorted
    /// outputs. Self-consistent by construction — both sides of the comparison come from
    /// the same backend and pass through here.
    /// </summary>
    public static string CanonicalizeObservation(CandidateCaseObservation observation)
    {
        // Positional envelope, not a merged dictionary: an output key literally named
        // "summary" must not be able to shadow the summary itself.
        var outputs = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in observation.Outputs ?? new Dictionary<string, object?>())
        {
            outputs[key] = value is JsonElement el
                ? el.GetRawText()
                : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        return JsonSerializer.Serialize(new object?[] { observation.Summary, observation.Threw, outputs });
    }

    /// <summary>Unwraps a JSON transport value to its CLR shape; non-JSON values pass through.</summary>
    public static object UnwrapJson(object value) =>
        value is JsonElement el ? FromJsonElement(el) : value;

    private static bool ValuesEqual(object expected, object actual)
    {
        if (expected is JsonElement expectedEl && actual is not JsonElement)
            return ValuesEqual(FromJsonElement(expectedEl), actual);
        if (actual is JsonElement actualEl && expected is not JsonElement)
            return ValuesEqual(expected, FromJsonElement(actualEl));

        // A witness pins EXACT output — no coercion across kinds. Integer and boolean are
        // matched type-first (if either side is integral/boolean, both must be), so a double
        // never rounds into an int (2.4 == 2) and an int never equals its decimal string or a
        // bool. Mirror of WitnessValueComparer.AreEqual.
        var expectedIsInt = expected is int or long or short or byte;
        var actualIsInt = actual is int or long or short or byte;
        if (expectedIsInt || actualIsInt)
        {
            if (!(expectedIsInt && actualIsInt))
            {
                return false;
            }
            return Convert.ToInt64(expected) == Convert.ToInt64(actual);
        }

        if (expected is bool || actual is bool)
        {
            return expected is bool eb && actual is bool ab && eb == ab;
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

    /// <summary>
    /// Renders a witness value for a failure message. Distinguishing null from the empty
    /// string matters more than it looks: this text is the repair feedback a proposer sees,
    /// and "expected  got " (which is what Convert.ToString produces for both, since it
    /// returns "" for null and JsonElement renders JsonValueKind.Null as empty) tells a
    /// proposer nothing it can act on. Strings are quoted so an empty one is visible.
    /// </summary>
    private static string FormatValue(object? value)
    {
        if (value is null)
            return "<null>";

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => "<null>",
                JsonValueKind.String => Quote(element.GetString()),
                _ => element.GetRawText(),
            };
        }

        return value is string text
            ? Quote(text)
            : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "<null>";
    }

    private static string Quote(string? value) => value is null ? "<null>" : $"\"{value}\"";
}
