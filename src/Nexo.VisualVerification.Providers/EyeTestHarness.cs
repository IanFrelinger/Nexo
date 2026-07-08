namespace Nexo.VisualVerification.Providers;

/// <summary>Harness options for capture and golden comparison.</summary>
public sealed class EyeTestHarnessOptions
{
    public int PerceptualEpsilon { get; init; } = PerceptualHash.DefaultEpsilon;

    public FrameExpectationSet? FrameExpectations { get; init; }

    public string FrameStorageRoot { get; init; } = Path.Combine(Path.GetTempPath(), "nexo-eye-frames");

    public bool RequireDeterminismGate { get; init; } = true;
}

/// <summary>Single harness execution output.</summary>
public sealed record EyeTestRunResult(DisplayConfig Display, IReadOnlyList<CapturedFrame> Frames);

/// <summary>
/// Composes display + target providers into a certified eye-test session.
/// Runs every session twice when determinism gate is enabled.
/// </summary>
public sealed class EyeTestHarness
{
    private readonly IDisplayProvider _display;
    private readonly ITargetAppProvider _target;
    private readonly FrameContentStore _frameStore;

    public EyeTestHarness(
        IDisplayProvider display,
        ITargetAppProvider target,
        EyeTestHarnessOptions? options = null)
    {
        _display = display;
        _target = target;
        Options = options ?? new EyeTestHarnessOptions();
        _frameStore = new FrameContentStore(Options.FrameStorageRoot);
    }

    public EyeTestHarnessOptions Options { get; }

    public async Task<EyeTestSessionRecord> RunAsync(
        ScenarioDefinition scenario,
        BuildArtifactRef build,
        int width,
        int height,
        CancellationToken ct = default)
    {
        var firstRun = await RunOnceAsync(scenario, build, width, height, ct).ConfigureAwait(false);

        VerificationVerdict verdict;
        if (BlankFrameDetector.ShouldReject(scenario, firstRun.Frames))
        {
            verdict = VerdictComputer.CaptureFailure();
        }
        else if (Options.RequireDeterminismGate)
        {
            var secondRun = await RunOnceAsync(scenario, build, width, height, ct).ConfigureAwait(false);
            var nondeterministicSteps = FindMismatchedSteps(firstRun.Frames, secondRun.Frames);
            if (nondeterministicSteps.Count > 0)
                verdict = VerdictComputer.NonDeterministic(nondeterministicSteps);
            else if (Options.FrameExpectations is not null)
                verdict = VerdictComputer.CompareToExpectations(
                    ScenarioHasher.ComputeHash(scenario),
                    build.Sha256,
                    firstRun.Frames,
                    Options.FrameExpectations,
                    Options.PerceptualEpsilon);
            else
                verdict = new VerificationVerdict(VerdictTier.ExactMatch, Array.Empty<int>(), 0);
        }
        else if (Options.FrameExpectations is not null)
        {
            verdict = VerdictComputer.CompareToExpectations(
                ScenarioHasher.ComputeHash(scenario),
                build.Sha256,
                firstRun.Frames,
                Options.FrameExpectations,
                Options.PerceptualEpsilon);
        }
        else
        {
            verdict = new VerificationVerdict(VerdictTier.ExactMatch, Array.Empty<int>(), 0);
        }

        return EyeTestSessionRecordBuilder.BuildUnsigned(
            scenario,
            build,
            firstRun.Display,
            firstRun.Frames,
            verdict);
    }

    private async Task<EyeTestRunResult> RunOnceAsync(
        ScenarioDefinition scenario,
        BuildArtifactRef build,
        int width,
        int height,
        CancellationToken ct)
    {
        var display = await _display.StartAsync(width, height, ct).ConfigureAwait(false);
        await _target.LaunchAsync(build, display, scenario, ct).ConfigureAwait(false);

        var frames = new List<CapturedFrame>(scenario.TotalSteps);
        for (var step = 0; step < scenario.TotalSteps; step++)
        {
            if (await _target.HasExitedAsync().ConfigureAwait(false))
                break;

            await _target.StepAsync(step, ct).ConfigureAwait(false);
            var pixels = await _display.CaptureFrameAsync(ct).ConfigureAwait(false);
            var sha256 = ContentSha256.ComputeHex(pixels);
            var perceptual = PerceptualHash.ComputeDHashHex(pixels, display.Width, display.Height, display.PixelFormat);
            var storagePath = _frameStore.Store(pixels);
            frames.Add(new CapturedFrame(step, sha256, perceptual, storagePath));
        }

        return new EyeTestRunResult(display, frames);
    }

    private static IReadOnlyList<int> FindMismatchedSteps(
        IReadOnlyList<CapturedFrame> runA,
        IReadOnlyList<CapturedFrame> runB)
    {
        var mismatched = new List<int>();
        var count = Math.Min(runA.Count, runB.Count);
        for (var i = 0; i < count; i++)
        {
            if (!string.Equals(runA[i].Sha256, runB[i].Sha256, StringComparison.Ordinal))
                mismatched.Add(runA[i].Step);
        }

        if (runA.Count != runB.Count)
        {
            for (var step = count; step < Math.Max(runA.Count, runB.Count); step++)
                mismatched.Add(step);
        }

        return mismatched;
    }
}
