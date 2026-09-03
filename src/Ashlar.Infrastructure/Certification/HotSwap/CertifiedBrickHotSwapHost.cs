using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Autonomy;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Testing.CodeAnalysis;

namespace Ashlar.Infrastructure.Certification.HotSwap;

/// <summary>
/// Hot-reloads certified bricks into a running host, one collectible load context per
/// <em>generation</em> of the brick set — never per brick.
/// </summary>
/// <remarks>
/// Trust properties (trust-loop integration plan §3):
/// <list type="number">
/// <item><description><b>Verify-at-load.</b> Every brick's certification record is re-verified
/// against the exact source bytes being loaded (<see cref="CertificationTrustVerifier"/>),
/// even though admission already verified once — the artifact may have changed since.
/// Hash mismatch refuses the load.</description></item>
/// <item><description><b>Fail-closed swap.</b> Any verification, compile, load, or
/// instantiation failure refuses the <em>entire</em> swap and leaves the previous
/// generation serving. There is no partial swap.</description></item>
/// <item><description><b>Serialized transitions.</b> Generation load/unload/collection runs
/// under <see cref="CollectibleLoadContextGate"/>, so collectible <c>LoaderAllocator</c>s
/// are never finalized concurrently (<c>0x80131506</c>).</description></item>
/// <item><description><b>Provenance.</b> Every swap — committed or refused — and every
/// generation lifecycle transition emits a <see cref="BrickSwapProvenanceEvent"/>.</description></item>
/// </list>
/// Swap sequence: verify all → load generation N+1 → route new invocations to it →
/// drain generation N → unload → drive collection and report leak suspicion by context
/// name (the named-context attribution trick).
/// </remarks>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed class CertifiedBrickHotSwapHost : IDisposable
{
    private static readonly TimeSpan DefaultDrainTimeout = TimeSpan.FromSeconds(30);

    private readonly ICertifiedBrickSwapProvenanceSink? _provenanceSink;
    private readonly ILogger<CertifiedBrickHotSwapHost>? _logger;
    private readonly string? _hmacKey;
    private readonly TimeSpan _drainTimeout;
    private readonly Ashlar.Core.Application.Autonomy.ICertificateRevocationList? _revocations;
    private readonly int _retentionWindow;
    private readonly List<RetainedGeneration> _retained = new();
    private readonly WatchThresholds? _watchThresholds;
    private readonly Ashlar.Core.Application.Autonomy.ILineageAuthority? _lineageAuthority;
    private readonly Ashlar.Core.Application.Autonomy.LoopPauseControl? _pauseControl;
    private readonly TimeSpan? _cadenceFloor;
    private readonly TimeProvider _clock;

    private BrickGeneration? _current;
    private int _generationCounter;
    private GenerationWatchStats? _watchBaseline;
    private GenerationWatchStats? _watchCurrent;
    private int _watchGenerationId;
    private int _watchBreachLatch;
    private DateTimeOffset? _lastAutonomousSwapUtc;
    private IReadOnlyList<string> _currentLineageKeys = Array.Empty<string>();

    /// <summary>Initializes the host.</summary>
    /// <param name="provenanceSink">Receives swap/generation provenance events; null records nothing.</param>
    /// <param name="logger">Optional diagnostics logger.</param>
    /// <param name="hmacKey">Explicit record-verification key; falls back to environment/dev key.</param>
    /// <param name="drainTimeout">How long a retired generation may drain before it is unloaded anyway.</param>
    /// <param name="revocations">Quarantine list; any request whose certificate content hash is revoked is refused permanently (R5.3).</param>
    /// <param name="retentionWindow">How many committed generations stay reactivatable via <see cref="RollbackToAsync"/> (R5.1). 0 disables retention.</param>
    /// <param name="watchThresholds">Post-swap watch thresholds (R5.2); breach quarantines the generation and rolls back automatically. Null = no watch (human-driven flow).</param>
    /// <param name="lineageAuthority">Rollback ledger per objective lineage (R5.5); demoted lineages lose auto-swap.</param>
    /// <param name="pauseControl">Global pause (R6.2): while paused, autonomous swaps are refused; human-driven swaps proceed.</param>
    /// <param name="cadenceFloor">Minimum interval between autonomous swaps (R6.1) so the runtime never absorbs changes faster than watch windows clear.</param>
    /// <param name="clock">Clock for cadence decisions; system time when null.</param>
    public CertifiedBrickHotSwapHost(
        ICertifiedBrickSwapProvenanceSink? provenanceSink = null,
        ILogger<CertifiedBrickHotSwapHost>? logger = null,
        string? hmacKey = null,
        TimeSpan? drainTimeout = null,
        Ashlar.Core.Application.Autonomy.ICertificateRevocationList? revocations = null,
        int retentionWindow = 2,
        WatchThresholds? watchThresholds = null,
        Ashlar.Core.Application.Autonomy.ILineageAuthority? lineageAuthority = null,
        Ashlar.Core.Application.Autonomy.LoopPauseControl? pauseControl = null,
        TimeSpan? cadenceFloor = null,
        TimeProvider? clock = null)
    {
        _provenanceSink = provenanceSink;
        _logger = logger;
        _hmacKey = hmacKey;
        _drainTimeout = drainTimeout ?? DefaultDrainTimeout;
        _revocations = revocations;
        _retentionWindow = Math.Max(0, retentionWindow);
        _watchThresholds = watchThresholds;
        _lineageAuthority = lineageAuthority;
        _pauseControl = pauseControl;
        _cadenceFloor = cadenceFloor;
        _clock = clock ?? TimeProvider.System;
    }

    private sealed record RetainedGeneration(int GenerationId, IReadOnlyList<CertifiedBrickLoadRequest> Requests);

    /// <summary>Per-generation runtime signals for the watch window (R5.2). Interlocked counters only.</summary>
    private sealed class GenerationWatchStats
    {
        private long _invocations;
        private long _faults;
        private long _latencyTicks;
        private long _undeclaredWrites;
        private long _maxLatencyTicks;

        public void Record(long elapsedTicks, bool faulted, int undeclaredWrites)
        {
            Interlocked.Increment(ref _invocations);
            Interlocked.Add(ref _latencyTicks, elapsedTicks);
            if (faulted)
                Interlocked.Increment(ref _faults);
            if (undeclaredWrites > 0)
                Interlocked.Add(ref _undeclaredWrites, undeclaredWrites);

            // Lock-free running maximum for the absolute-duration leg.
            for (var seen = Interlocked.Read(ref _maxLatencyTicks);
                 elapsedTicks > seen;
                 seen = Interlocked.Read(ref _maxLatencyTicks))
            {
                if (Interlocked.CompareExchange(ref _maxLatencyTicks, elapsedTicks, seen) == seen)
                    break;
            }
        }

        public (long Invocations, long Faults, long LatencyTicks, long UndeclaredWrites, long MaxLatencyTicks) Snapshot() => (
            Interlocked.Read(ref _invocations),
            Interlocked.Read(ref _faults),
            Interlocked.Read(ref _latencyTicks),
            Interlocked.Read(ref _undeclaredWrites),
            Interlocked.Read(ref _maxLatencyTicks));
    }

    /// <summary>Generation currently serving, or null before the first successful swap.</summary>
    public int? CurrentGenerationId => Volatile.Read(ref _current)?.Id;

    /// <summary>Brick ids serving in the current generation.</summary>
    public IReadOnlyCollection<string> CurrentBrickIds =>
        Volatile.Read(ref _current)?.BrickIds ?? Array.Empty<string>();

    /// <summary>
    /// Verifies, loads, and atomically publishes a new generation of certified bricks.
    /// On any refusal the previous generation keeps serving untouched.
    /// </summary>
    public async Task<CertifiedBrickSwapResult> SwapAsync(
        IReadOnlyList<CertifiedBrickLoadRequest> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests is null)
            throw new ArgumentNullException(nameof(requests));
        if (requests.Count == 0)
            throw new ArgumentException("A swap needs at least one brick.", nameof(requests));

        // Verify-at-load happens before any load context exists: pure computation over
        // in-memory source, so it needs no gate and a refusal costs no ALC churn.
        var refusals = VerifyAll(requests);
        if (refusals.Count > 0)
            return Refuse(Volatile.Read(ref _generationCounter) + 1, requests, refusals);

        await CollectibleLoadContextGate.Instance.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var generationId = _generationCounter + 1;
            var materialized = await MaterializeAsync(generationId, requests, cancellationToken).ConfigureAwait(false);

            if (materialized.Generation is null)
            {
                // Half-built context: unload was already requested inside the frame that
                // owned it; drive collection so at most one allocator awaits finalization.
                await WaitForContextReleaseAsync(materialized.AbortedContextRef).ConfigureAwait(false);
                TryDeleteDirectory(materialized.TempDirectory);
                cancellationToken.ThrowIfCancellationRequested();
                return Refuse(generationId, requests, materialized.Refusals);
            }

            _generationCounter = generationId;
            var previous = Interlocked.Exchange(ref _current, materialized.Generation);
            RetainCommitted(generationId, requests, materialized.EmittedImages);
            // Watch rotation (R5.2): the outgoing generation's runtime signals become the
            // baseline the incoming generation is judged against; the breach latch resets.
            _watchBaseline = _watchCurrent;
            _watchCurrent = new GenerationWatchStats();
            Volatile.Write(ref _watchGenerationId, generationId);
            Volatile.Write(ref _watchBreachLatch, 0);

            // Cadence + in-flight bookkeeping (R6.1) for autonomous swaps.
            var autonomousKeys = requests
                .Where(r => r.Autonomous is not null)
                .Select(r => r.Autonomous!.LineageKey)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (requests.Any(r => r.Autonomous is not null))
                _lastAutonomousSwapUtc = _clock.GetUtcNow();
            _currentLineageKeys = autonomousKeys;

            foreach (var request in requests)
            {
                Emit(new BrickSwapProvenanceEvent
                {
                    Generation = generationId,
                    Outcome = BrickSwapProvenanceOutcomes.BrickLoaded,
                    Timestamp = DateTimeOffset.UtcNow,
                    BrickId = request.BrickId,
                    ContentHash = request.Record.ContentHash,
                    CertificateSignature = request.Record.Signature,
                    ContextName = materialized.Generation.ContextName
                });
            }

            Emit(new BrickSwapProvenanceEvent
            {
                Generation = generationId,
                Outcome = BrickSwapProvenanceOutcomes.SwapCommitted,
                Timestamp = DateTimeOffset.UtcNow,
                ContextName = materialized.Generation.ContextName
            });
            _logger?.LogInformation(
                "hot-swap committed generation {Generation} ({Context}) with {Count} brick(s)",
                generationId, materialized.Generation.ContextName, requests.Count);

            var previousCollected = false;
            string? previousName = null;
            if (previous is not null)
            {
                previousName = previous.ContextName;
                previousCollected = await RetireAsync(previous).ConfigureAwait(false);
            }

            return new CertifiedBrickSwapResult
            {
                Swapped = true,
                GenerationId = generationId,
                GenerationContextName = materialized.Generation.ContextName,
                LoadedBrickIds = materialized.Generation.BrickIds.ToArray(),
                PreviousGenerationCollected = previousCollected,
                PreviousGenerationContextName = previousName
            };
        }
        finally
        {
            CollectibleLoadContextGate.Instance.Release();
        }
    }

    /// <summary>
    /// Executes a certified brick from the currently serving generation. Invocations are
    /// leased so a retiring generation drains before its context is unloaded.
    /// </summary>
    public async Task<BrickOutput> ExecuteAsync(
        string brickId,
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(brickId))
            throw new ArgumentException("Brick id is required.", nameof(brickId));

        // A generation read can race its own retirement; when the lease is refused the
        // freshly published generation is already routable, so re-read and retry.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var generation = Volatile.Read(ref _current)
                ?? throw new InvalidOperationException("No certified brick generation is loaded.");

            if (!generation.TryEnter())
                continue;

            BrickOutput? output = null;
            Exception? brickFault = null;
            DomainBrick? brick = null;
            var startTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
                brick = generation.GetBrick(brickId)
                    ?? throw new KeyNotFoundException(
                        $"No certified brick '{brickId}' in generation {generation.Id}.");
                try
                {
                    output = await brick.ExecuteAsync(input, implementation, context, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Host-side cancellation is not brick misbehavior; no watch signal.
                    throw;
                }
                catch (Exception ex)
                {
                    brickFault = ex;
                }
            }
            finally
            {
                generation.Exit();
            }

            // Watch observation runs OUTSIDE the lease so a breach-triggered rollback can
            // drain this generation without deadlocking on its own invocation (R5.2).
            if (_watchThresholds is not null && brick is not null
                && generation.Id == Volatile.Read(ref _watchGenerationId))
            {
                var elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - startTicks;
                var undeclared = output is null ? 0 : CountUndeclaredWrites(brick, output);
                _watchCurrent?.Record(elapsed, brickFault is not null, undeclared);
                var breachReasons = EvaluateWatch();
                if (breachReasons is not null)
                    await QuarantineCurrentAsync(breachReasons, cancellationToken).ConfigureAwait(false);
            }

            if (brickFault is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(brickFault).Throw();
            return output!;
        }

        throw new InvalidOperationException(
            "Could not lease a serving generation; swaps kept retiring it mid-read.");
    }

    private static int CountUndeclaredWrites(DomainBrick brick, BrickOutput output)
    {
        var declared = brick.Interface?.Outputs;
        if (declared is null || declared.Count == 0)
            return 0; // No declared contract to check — honesty over guessing.

        var declaredNames = new HashSet<string>(declared.Select(o => o.Name), StringComparer.Ordinal);
        return output.ToDictionary().Keys.Count(k => !declaredNames.Contains(k));
    }

    /// <summary>
    /// Whether the current generation's watch window is still in flight for a lineage:
    /// thresholds active, the lineage is among the serving generation's autonomous keys,
    /// no breach yet, and fewer than MinInvocations observed (R6.1).
    /// </summary>
    private bool WatchWindowInFlight(string lineageKey)
    {
        if (_watchThresholds is null || _watchCurrent is null)
            return false;
        if (!_currentLineageKeys.Contains(lineageKey, StringComparer.OrdinalIgnoreCase))
            return false;
        if (Volatile.Read(ref _watchBreachLatch) != 0)
            return false; // Breached windows resolve via quarantine, not by blocking.

        var (invocations, _, _, _, _) = _watchCurrent.Snapshot();
        return invocations < _watchThresholds.MinInvocations;
    }

    /// <summary>Breach reasons when the watch thresholds are crossed; null otherwise (R5.2).</summary>
    private IReadOnlyList<string>? EvaluateWatch()
    {
        var thresholds = _watchThresholds;
        var current = _watchCurrent;
        if (thresholds is null || current is null || Volatile.Read(ref _watchBreachLatch) != 0)
            return null;

        var (invocations, faults, latencyTicks, undeclared, maxLatencyTicks) = current.Snapshot();
        var reasons = new List<string>();

        // Contract conformance is absolute — no baseline needed (R5.2).
        if (undeclared > thresholds.MaxUndeclaredWrites)
            reasons.Add($"{undeclared} undeclared output write(s) exceed the tolerated {thresholds.MaxUndeclaredWrites}");

        // The duration ceiling is absolute too: a first-generation deploy has no baseline
        // for the relative legs, and a pathological single invocation must not hide in a
        // healthy mean.
        if (thresholds.MaxInvocationDuration is { } durationCap && maxLatencyTicks > durationCap.Ticks)
        {
            reasons.Add($"an invocation took {TimeSpan.FromTicks(maxLatencyTicks).TotalMilliseconds:F0}ms, "
                + $"exceeding the absolute ceiling of {durationCap.TotalMilliseconds:F0}ms");
        }

        if (invocations >= thresholds.MinInvocations && _watchBaseline is { } baseline)
        {
            var (bInv, bFaults, bLatency, _, _) = baseline.Snapshot();
            if (bInv > 0)
            {
                var errorRate = (double)faults / invocations;
                var baselineRate = (double)bFaults / bInv;
                if (errorRate > baselineRate + thresholds.MaxErrorRateDelta)
                    reasons.Add($"error rate {errorRate:F2} exceeds baseline {baselineRate:F2} by more than {thresholds.MaxErrorRateDelta:F2}");

                var meanLatency = (double)latencyTicks / invocations;
                var baselineMean = (double)bLatency / bInv;
                if (baselineMean > 0 && meanLatency > baselineMean * thresholds.MaxLatencyFactor)
                    reasons.Add($"mean latency is {meanLatency / baselineMean:F1}x the baseline (max {thresholds.MaxLatencyFactor:F1}x)");
            }
        }

        if (reasons.Count == 0)
            return null;

        // One quarantine per generation: first observer wins the latch.
        return Interlocked.CompareExchange(ref _watchBreachLatch, 1, 0) == 0 ? reasons : null;
    }

    /// <summary>
    /// Quarantines the current generation after a watch breach (R5.2/R5.3): revokes its
    /// certificate hashes, records the rollback against its lineage (R5.5), emits
    /// provenance, and reactivates the previous retained generation. All fail-closed:
    /// with nothing retained to roll back to, the quarantine is still recorded loudly.
    /// </summary>
    private async Task QuarantineCurrentAsync(IReadOnlyList<string> reasons, CancellationToken cancellationToken)
    {
        RetainedGeneration? currentRetained = null;
        RetainedGeneration? previousRetained = null;
        lock (_retained)
        {
            if (_retained.Count > 0)
                currentRetained = _retained[^1];
            if (_retained.Count > 1)
                previousRetained = _retained[^2];
        }

        var reason = "Watch breach: " + string.Join(" | ", reasons);
        _logger?.LogWarning("hot-swap watch breach on generation {Generation}: {Reason}",
            currentRetained?.GenerationId, reason);

        if (currentRetained is not null)
        {
            foreach (var request in currentRetained.Requests)
            {
                if (_revocations is not null && !string.IsNullOrWhiteSpace(request.Record.ContentHash))
                    _revocations.Revoke(request.Record.ContentHash!, reason);
                if (_lineageAuthority is not null && request.Autonomous?.LineageKey is { } lineageKey)
                    _lineageAuthority.RecordRollback(lineageKey);
            }
        }

        Emit(new BrickSwapProvenanceEvent
        {
            Generation = currentRetained?.GenerationId ?? Volatile.Read(ref _watchGenerationId),
            Outcome = BrickSwapProvenanceOutcomes.WatchBreachQuarantined,
            Timestamp = DateTimeOffset.UtcNow,
            Reason = reason
        });

        if (previousRetained is not null)
        {
            await RollbackToAsync(previousRetained.GenerationId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger?.LogError(
                "watch breach quarantined generation {Generation} but no earlier generation is retained to roll back to",
                currentRetained?.GenerationId);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Best-effort teardown at end of host lifetime: no drain, no forced collection —
        // in-flight invocations keep the context alive until they return, and the GC
        // reclaims it once the last reference drops.
        var current = Interlocked.Exchange(ref _current, null);
        if (current is null)
            return;
        _ = current.RetireAndSignal();
        _ = current.DetachAndUnload();
        TryDeleteDirectory(current.TempDirectory);
    }

    /// <summary>
    /// Reactivates a retained generation (autonomy spec R5.1): the retained certificates
    /// and source are re-verified, the retained EMITTED IMAGES load into a fresh context —
    /// no build, no network, no model — and the standard fail-closed swap semantics apply
    /// (a revoked hash in the retained set refuses the rollback; R5.3 outranks R5.1).
    /// </summary>
    public async Task<CertifiedBrickSwapResult> RollbackToAsync(
        int generationId,
        CancellationToken cancellationToken = default)
    {
        RetainedGeneration? retained;
        lock (_retained)
        {
            retained = _retained.FirstOrDefault(r => r.GenerationId == generationId);
        }

        if (retained is null)
        {
            return new CertifiedBrickSwapResult
            {
                Swapped = false,
                Refusals = new[]
                {
                    new BrickSwapRefusal
                    {
                        BrickId = "*",
                        Stage = BrickSwapRefusalStage.Request,
                        FailureCode = "no-retained-generation",
                        Reason = $"Generation {generationId} is not in the retention window "
                            + $"({_retentionWindow} generation(s) retained)."
                    }
                }
            };
        }

        var result = await SwapAsync(retained.Requests, cancellationToken).ConfigureAwait(false);
        if (result.Swapped)
        {
            Emit(new BrickSwapProvenanceEvent
            {
                Generation = result.GenerationId ?? 0,
                Outcome = BrickSwapProvenanceOutcomes.RollbackCommitted,
                Timestamp = DateTimeOffset.UtcNow,
                ContextName = result.GenerationContextName,
                Reason = $"Reactivated retained generation {generationId} from its emitted images."
            });
        }

        return result;
    }

    private void RetainCommitted(
        int generationId,
        IReadOnlyList<CertifiedBrickLoadRequest> requests,
        IReadOnlyDictionary<string, byte[]>? emittedImages)
    {
        if (_retentionWindow == 0 || emittedImages is null)
            return;

        var snapshot = requests
            .Select(r => r with
            {
                PrecompiledAssembly = emittedImages.TryGetValue(r.BrickId, out var image) ? image : null,
            })
            .ToArray();
        if (snapshot.Any(r => r.PrecompiledAssembly is null))
            return; // A generation we cannot fully reactivate without a build is not retained.

        lock (_retained)
        {
            _retained.Add(new RetainedGeneration(generationId, snapshot));
            while (_retained.Count > _retentionWindow)
                _retained.RemoveAt(0);
        }
    }

    private List<BrickSwapRefusal> VerifyAll(IReadOnlyList<CertifiedBrickLoadRequest> requests)
    {
        var refusals = new List<BrickSwapRefusal>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var request in requests)
        {
            if (!seen.Add(request.BrickId))
            {
                refusals.Add(new BrickSwapRefusal
                {
                    BrickId = request.BrickId,
                    Stage = BrickSwapRefusalStage.Request,
                    FailureCode = "duplicate-brick-id",
                    Reason = $"Brick '{request.BrickId}' appears more than once in the swap."
                });
                continue;
            }

            if (!string.Equals(request.Record.BrickId, request.BrickId, StringComparison.OrdinalIgnoreCase))
            {
                refusals.Add(new BrickSwapRefusal
                {
                    BrickId = request.BrickId,
                    Stage = BrickSwapRefusalStage.Request,
                    FailureCode = "record-brick-mismatch",
                    Reason = $"Certification record was minted for '{request.Record.BrickId}', not '{request.BrickId}'."
                });
                continue;
            }

            var trust = CertificationTrustVerifier.Verify(request.Record, request.SourceCode, _hmacKey);
            if (!trust.Trusted)
            {
                refusals.Add(new BrickSwapRefusal
                {
                    BrickId = request.BrickId,
                    Stage = BrickSwapRefusalStage.Verification,
                    FailureCode = trust.FailureCode,
                    Reason = trust.Reason
                });
                continue;
            }

            // R5.3: quarantine is permanent. A revoked content hash never loads again —
            // rollbacks and bit-identical resubmissions included. Re-earning admission
            // means re-certifying a new candidate, never resurrecting the old hash.
            if (_revocations?.IsRevoked(request.Record.ContentHash ?? "") == true)
            {
                refusals.Add(new BrickSwapRefusal
                {
                    BrickId = request.BrickId,
                    Stage = BrickSwapRefusalStage.Verification,
                    FailureCode = "revoked-hash",
                    Reason = "Certificate content hash is revoked (quarantined): "
                        + (_revocations.TryGetReason(request.Record.ContentHash ?? "") ?? "no reason recorded")
                });
                continue;
            }

            // Autonomy spec R3.2 (swap-host leg) + R4.2 (independent ceiling): when the
            // LOOP drives the swap, only Tier-0 classifications may auto-swap, and the
            // recursion rules are re-checked here regardless of what the certifier did.
            if (request.Autonomous is { } autonomous)
            {
                // R6.2: the global pause halts autonomous swaps immediately. Human-driven
                // swaps (null Autonomous) proceed — pause bounds the LOOP, not the operator.
                if (_pauseControl?.IsPaused == true)
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Request,
                        FailureCode = "loop-paused",
                        Reason = $"The autonomy loop is paused: {_pauseControl.PausedReason}"
                    });
                    continue;
                }

                // R6.1: the cadence floor keeps the runtime from absorbing autonomous
                // changes faster than watch windows can clear them.
                if (_cadenceFloor is { } floor && _lastAutonomousSwapUtc is { } last
                    && _clock.GetUtcNow() - last < floor)
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Request,
                        FailureCode = "cadence-floor",
                        Reason = $"Autonomous swap cadence floor of {floor.TotalSeconds:F0}s has not elapsed "
                            + "since the previous autonomous swap (R6.1)."
                    });
                    continue;
                }

                // R6.1: an in-flight watch window blocks the next autonomous swap of the
                // SAME lineage until the window clears (MinInvocations without breach).
                if (autonomous.LineageKey is { } inFlightKey && WatchWindowInFlight(inFlightKey))
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Request,
                        FailureCode = "watch-window-in-flight",
                        Reason = $"Lineage '{inFlightKey}' has an in-flight watch window; the next "
                            + "autonomous swap of this lineage waits for it to clear (R6.1)."
                    });
                    continue;
                }

                // R5.5: a lineage demoted on rollback evidence has lost Tier-0 autonomy —
                // its swaps wait for the human gate no matter what tier the objective
                // classified at. Autonomy is lost on evidence, never gained on it.
                if (_lineageAuthority is not null
                    && autonomous.LineageKey is { } lineageKey
                    && _lineageAuthority.IsDemoted(lineageKey))
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Request,
                        FailureCode = "lineage-demoted",
                        Reason = $"Objective lineage '{lineageKey}' lost Tier-0 autonomy after repeated "
                            + "rollback (R5.5); admission now waits for the human gate."
                    });
                    continue;
                }

                if (autonomous.Tier != Ashlar.Core.Application.Autonomy.ObjectiveTier.Tier0Autonomous)
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Request,
                        FailureCode = "tier-requires-human-admission",
                        Reason = $"Objective tier {autonomous.Tier} cannot auto-swap; admission waits for "
                            + "the human gate (autonomy spec R3.1)."
                    });
                    continue;
                }

                var lineage = autonomous.Lineage ?? Ashlar.Core.Application.Autonomy.GenerationLineage.HumanAuthored;
                var recursion = Ashlar.Core.Application.Autonomy.RecursionDiscipline.FindViolations(lineage);
                if (recursion.Count > 0)
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Request,
                        FailureCode = "recursion-refused",
                        Reason = "Recursion discipline failed at the swap host: " + string.Join(" | ", recursion)
                    });
                }
            }
        }

        return refusals;
    }

    private CertifiedBrickSwapResult Refuse(
        int generationId,
        IReadOnlyList<CertifiedBrickLoadRequest> requests,
        IReadOnlyList<BrickSwapRefusal> refusals)
    {
        // TryAdd: duplicate brick ids are themselves a refusal cause, so they must not
        // blow up refusal reporting.
        var byBrick = new Dictionary<string, CertifiedBrickLoadRequest>(StringComparer.OrdinalIgnoreCase);
        foreach (var request in requests)
            byBrick.TryAdd(request.BrickId, request);
        foreach (var refusal in refusals)
        {
            byBrick.TryGetValue(refusal.BrickId, out var request);
            Emit(new BrickSwapProvenanceEvent
            {
                Generation = generationId,
                Outcome = BrickSwapProvenanceOutcomes.BrickRefused,
                Timestamp = DateTimeOffset.UtcNow,
                BrickId = refusal.BrickId,
                ContentHash = request?.Record.ContentHash,
                CertificateSignature = request?.Record.Signature,
                FailureCode = refusal.FailureCode,
                Reason = refusal.Reason
            });
        }

        Emit(new BrickSwapProvenanceEvent
        {
            Generation = generationId,
            Outcome = BrickSwapProvenanceOutcomes.SwapRefused,
            Timestamp = DateTimeOffset.UtcNow,
            Reason = $"{refusals.Count} of {requests.Count} brick(s) refused; previous generation keeps serving."
        });
        _logger?.LogWarning(
            "hot-swap refused for would-be generation {Generation}: {Refusals}",
            generationId,
            string.Join("; ", refusals.Select(r => $"{r.BrickId}:{r.FailureCode}")));

        return new CertifiedBrickSwapResult { Swapped = false, Refusals = refusals };
    }

    private sealed record MaterializeOutcome(
        BrickGeneration? Generation,
        IReadOnlyList<BrickSwapRefusal> Refusals,
        WeakReference AbortedContextRef,
        string TempDirectory,
        IReadOnlyDictionary<string, byte[]>? EmittedImages = null);

    /// <summary>
    /// Compiles and loads every brick into one new collectible context. On any failure the
    /// half-built context is unloaded inside this frame — nothing context-owned escapes a
    /// refusal — and only a weak handle is returned for collection. NoInlining keeps
    /// context-owned locals out of the caller's frame (the mutation engine's discipline).
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<MaterializeOutcome> MaterializeAsync(
        int generationId,
        IReadOnlyList<CertifiedBrickLoadRequest> requests,
        CancellationToken cancellationToken)
    {
        var contextName = $"BrickGeneration_{generationId:D4}_{Guid.NewGuid():N}";
        var tempDirectory = Path.Combine(Path.GetTempPath(), "ashlar-hot-swap", contextName);
        Directory.CreateDirectory(tempDirectory);

        var context = new BrickGenerationLoadContext(contextName);
        var bricks = new Dictionary<string, DomainBrick>(StringComparer.OrdinalIgnoreCase);
        var emittedImages = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var refusals = new List<BrickSwapRefusal>();
        var compiler = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance);

        try
        {
            foreach (var request in requests)
            {
                var assemblyName = $"CertifiedBrick_{generationId:D4}_{Guid.NewGuid():N}";
                var outputPath = Path.Combine(tempDirectory, $"{assemblyName}.dll");
                var references = new List<string>
                {
                    typeof(DomainBrick).Assembly.Location,
                    typeof(BrickInput).Assembly.Location
                };
                references.AddRange(request.AdditionalCompilationReferences);

                // Rollback path (R5.1): a retained generation reactivates from its exact
                // emitted image — no compiler runs. Verify-at-load already re-checked the
                // source hash against the certificate before this frame.
                if (request.PrecompiledAssembly is { } image)
                {
                    File.WriteAllBytes(outputPath, image);
                }
                else
                {
                    var compile = await compiler.CompileAsync(
                        WrapForRoslynCompile(request.SourceCode),
                        assemblyName,
                        outputPath,
                        references,
                        cancellationToken).ConfigureAwait(false);

                    if (!compile.Success || string.IsNullOrWhiteSpace(compile.AssemblyPath) || !File.Exists(compile.AssemblyPath))
                    {
                        refusals.Add(new BrickSwapRefusal
                        {
                            BrickId = request.BrickId,
                            Stage = BrickSwapRefusalStage.Compilation,
                            FailureCode = "compile-failed",
                            Reason = string.Join("; ", compile.Errors.DefaultIfEmpty("no assembly produced"))
                        });
                        continue;
                    }
                }

                Assembly assembly;
                try
                {
                    assembly = context.LoadFromAssemblyPath(outputPath);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Load,
                        FailureCode = "assembly-load-failed",
                        Reason = ex.Message
                    });
                    continue;
                }

                var type = request.BrickTypeName is not null
                    ? assembly.GetType(request.BrickTypeName)
                    : assembly.GetTypes().FirstOrDefault(t =>
                        t.IsClass && !t.IsAbstract && typeof(DomainBrick).IsAssignableFrom(t));
                if (type is null)
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Load,
                        FailureCode = "brick-type-not-found",
                        Reason = request.BrickTypeName is not null
                            ? $"Type '{request.BrickTypeName}' not found in the compiled assembly."
                            : "No concrete Brick-derived type found in the compiled assembly."
                    });
                    continue;
                }

                DomainBrick brick;
                try
                {
                    brick = (DomainBrick)Activator.CreateInstance(type)!;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Instantiation,
                        FailureCode = "brick-instantiation-failed",
                        Reason = ex.Message
                    });
                    continue;
                }

                if (!string.Equals(brick.Id, request.BrickId, StringComparison.OrdinalIgnoreCase))
                {
                    refusals.Add(new BrickSwapRefusal
                    {
                        BrickId = request.BrickId,
                        Stage = BrickSwapRefusalStage.Instantiation,
                        FailureCode = "brick-id-mismatch",
                        Reason = $"Instantiated brick declares Id '{brick.Id}', but the certified record is for '{request.BrickId}'."
                    });
                    continue;
                }

                bricks[request.BrickId] = brick;
                // Captured for generation retention (R5.1): rollback reactivates from
                // these exact bytes with no compiler involved.
                emittedImages[request.BrickId] = File.ReadAllBytes(outputPath);
            }
        }
        catch (OperationCanceledException)
        {
            bricks.Clear();
            context.Unload();
            return new MaterializeOutcome(
                null,
                new[]
                {
                    new BrickSwapRefusal
                    {
                        BrickId = "*",
                        Stage = BrickSwapRefusalStage.Load,
                        FailureCode = "swap-cancelled",
                        Reason = "The swap was cancelled while materializing the generation."
                    }
                },
                new WeakReference(context),
                tempDirectory);
        }

        if (refusals.Count > 0)
        {
            bricks.Clear();
            context.Unload();
            return new MaterializeOutcome(null, refusals, new WeakReference(context), tempDirectory);
        }

        return new MaterializeOutcome(
            new BrickGeneration(generationId, contextName, tempDirectory, context, bricks),
            Array.Empty<BrickSwapRefusal>(),
            new WeakReference(null),
            tempDirectory,
            emittedImages);
    }

    private async Task<bool> RetireAsync(BrickGeneration previous)
    {
        var drained = previous.RetireAndSignal();
        // Deliberately not tied to the caller's token: once the new generation is
        // published, the old one must be torn down regardless of who cancelled.
        var drainedInTime = await Task.WhenAny(drained, Task.Delay(_drainTimeout, CancellationToken.None))
            .ConfigureAwait(false) == drained;

        var inFlight = previous.InFlightCount;
        var contextRef = previous.DetachAndUnload();
        Emit(new BrickSwapProvenanceEvent
        {
            Generation = previous.Id,
            Outcome = BrickSwapProvenanceOutcomes.GenerationRetired,
            Timestamp = DateTimeOffset.UtcNow,
            ContextName = previous.ContextName,
            Reason = drainedInTime
                ? "Drained; unload requested."
                : $"Drain timeout after {_drainTimeout.TotalSeconds:F0}s with {inFlight} invocation(s) in flight; unload requested anyway."
        });

        await WaitForContextReleaseAsync(contextRef).ConfigureAwait(false);
        var collected = !contextRef.IsAlive;
        Emit(new BrickSwapProvenanceEvent
        {
            Generation = previous.Id,
            Outcome = collected
                ? BrickSwapProvenanceOutcomes.GenerationCollected
                : BrickSwapProvenanceOutcomes.GenerationLeakSuspected,
            Timestamp = DateTimeOffset.UtcNow,
            ContextName = previous.ContextName,
            Reason = collected
                ? null
                : "Load context still reachable after forced collection; find it by name in AssemblyLoadContext.All."
        });
        if (!collected)
        {
            _logger?.LogWarning(
                "hot-swap generation {Generation} load context '{Context}' survived forced collection",
                previous.Id, previous.ContextName);
        }

        TryDeleteDirectory(previous.TempDirectory);
        return collected;
    }

    private void Emit(BrickSwapProvenanceEvent provenanceEvent)
    {
        try
        {
            _provenanceSink?.Record(provenanceEvent);
        }
        catch (Exception ex)
        {
            // A provenance sink failure must never fail a swap.
            _logger?.LogError(ex, "hot-swap provenance sink threw for outcome {Outcome}", provenanceEvent.Outcome);
        }
    }

    /// <summary>Drives collection after Unload, which only requests it: the allocator is
    /// freed once the last reference drops, and that needs a collection. (The mutation
    /// engine still uses a synchronous back-to-back loop; bringing it to parity is a follow-up.)
    /// Bounded retry with a real yield between passes rather than back-to-back passes: the
    /// swap is often reached inline on the thread that just completed the last invocation
    /// of the retiring generation, and that thread's frames still root the finished
    /// invocation's state machine (and with it a brick instance from the old context) until
    /// they unwind. No number of collections frees a stack-rooted object; giving the stack
    /// back does, so the pass after the first yield is the one that normally succeeds.</summary>
    private static async Task WaitForContextReleaseAsync(WeakReference contextRef)
    {
        if (!contextRef.IsAlive)
            return;

        const int maxAttempts = 20;
        for (var attempt = 0; attempt < maxAttempts && contextRef.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (contextRef.IsAlive)
                await Task.Delay(TimeSpan.FromMilliseconds(10), CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static void TryDeleteDirectory(string directory)
    {
        // While an assembly in the directory is still mapped the delete silently fails;
        // leftovers are attributable via the context-named directory.
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // best effort
        }
    }

    private static string WrapForRoslynCompile(string sourceCode) =>
        """
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainBrick = Ashlar.Core.Domain.Bricks.Brick;

""" + sourceCode;

    /// <summary>
    /// One immutable generation: its collectible context, its brick instances, and an
    /// invocation lease count so retirement can drain before unload.
    /// </summary>
    private sealed class BrickGeneration
    {
        private readonly Dictionary<string, DomainBrick> _bricks;
        private readonly TaskCompletionSource _drained =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private BrickGenerationLoadContext? _context;
        private int _inFlight;
        private volatile bool _retired;

        public BrickGeneration(
            int id,
            string contextName,
            string tempDirectory,
            BrickGenerationLoadContext context,
            Dictionary<string, DomainBrick> bricks)
        {
            Id = id;
            ContextName = contextName;
            TempDirectory = tempDirectory;
            _context = context;
            _bricks = bricks;
        }

        public int Id { get; }

        public string ContextName { get; }

        public string TempDirectory { get; }

        public int InFlightCount => Volatile.Read(ref _inFlight);

        public IReadOnlyCollection<string> BrickIds => _bricks.Keys.ToArray();

        public DomainBrick? GetBrick(string brickId) =>
            _bricks.TryGetValue(brickId, out var brick) ? brick : null;

        /// <summary>Leases the generation for one invocation; false once retired.</summary>
        public bool TryEnter()
        {
            Interlocked.Increment(ref _inFlight);
            if (_retired)
            {
                Exit();
                return false;
            }

            return true;
        }

        /// <summary>Releases one invocation lease; completes the drain when retired and idle.</summary>
        public void Exit()
        {
            if (Interlocked.Decrement(ref _inFlight) == 0 && _retired)
                _drained.TrySetResult();
        }

        /// <summary>Marks the generation retired and returns a task completing when drained.</summary>
        public Task RetireAndSignal()
        {
            _retired = true;
            if (Volatile.Read(ref _inFlight) == 0)
                _drained.TrySetResult();
            return _drained.Task;
        }

        /// <summary>
        /// Drops every strong reference into the load context (brick instances, the
        /// context itself), requests unload, and returns only a weak handle. After this
        /// returns, nothing in the generation keeps the allocator alive.
        /// </summary>
        public WeakReference DetachAndUnload()
        {
            _bricks.Clear();
            var context = _context;
            _context = null;
            var reference = new WeakReference(context);
            context?.Unload();
            return reference;
        }
    }
}
