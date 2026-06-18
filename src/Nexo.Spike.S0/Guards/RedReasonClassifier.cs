using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S0.Guards;

/// <summary>
/// Distinguishes unimplemented failures from assertion failures in RED output.
/// </summary>
public static class RedReasonClassifier
{
    private static readonly string[] UnimplementedMarkers =
    [
        "CS0246", // type or namespace not found
        "CS0103", // name does not exist
        "CS0117", // does not contain a definition
        "CS1061", // does not contain a definition for
        "NotImplementedException",
        "does not exist in the current context",
        "could not be found",
        "No test is available",
        "error CS"
    ];

    private static readonly string[] AssertionMarkers =
    [
        "Assert.",
        "Should().",
        "Expected ",
        "Expected:",
        "but found",
        "FluentAssertions",
        "Xunit.Sdk",
        "Equal() Failure",
        "True() Failure",
        "False() Failure",
        "Contains() Failure"
    ];

    public static RedFailureReason Classify(int exitCode, string stdout, string stderr, bool timedOut)
    {
        if (timedOut)
            return RedFailureReason.Unknown;

        var combined = $"{stdout}\n{stderr}";
        if (exitCode == 0)
            return RedFailureReason.PassedAgainstStub;

        if (ContainsAny(combined, UnimplementedMarkers))
            return RedFailureReason.Unimplemented;

        if (ContainsAny(combined, AssertionMarkers))
            return RedFailureReason.AssertionFailure;

        return RedFailureReason.Unknown;
    }

    private static bool ContainsAny(string haystack, IEnumerable<string> needles)
    {
        foreach (var needle in needles)
        {
            if (haystack.Contains(needle, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
