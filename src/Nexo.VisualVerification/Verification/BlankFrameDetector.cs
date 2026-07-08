namespace Nexo.VisualVerification;

/// <summary>Detects hollow captures where every frame is identical.</summary>
public static class BlankFrameDetector
{
    public static bool AllFramesIdentical(IReadOnlyList<CapturedFrame> frames) =>
        frames.Count > 1 && frames.Select(f => f.Sha256).Distinct(StringComparer.Ordinal).Count() == 1;

    public static bool ShouldReject(ScenarioDefinition scenario, IReadOnlyList<CapturedFrame> frames)
    {
        if (frames.Count == 0)
            return true;

        if (!ScenarioExpectsVisualChange(scenario))
            return false;

        return AllFramesIdentical(frames);
    }

    private static bool ScenarioExpectsVisualChange(ScenarioDefinition scenario) =>
        scenario.InputScript.Any(e =>
            e.Channel is "spawn" or "despawn" or "pause"
            || !string.IsNullOrEmpty(e.Payload));
}
