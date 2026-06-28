namespace Nexo.Certification.State;

/// <summary>
/// Verifies attested state logs: certified behavior provenance, schema validity, and hash-chain integrity.
/// Tier-1 is structural; optional <see cref="ITransitionReplayer"/> enables Tier-2 replay checks;
/// optional <see cref="ILiveStateHashReader"/> enables Tier-3 live head binding.
/// </summary>
public static class StateLogVerifier
{
    private static readonly CertifiedTransitionBuilder TransitionBuilder = new();

    public static StateLogTrustResult Verify(
        AttestedStateLog log,
        StateSchema schema,
        ICertificateResolver resolver,
        string? hmacKey = null,
        ITransitionReplayer? replayer = null,
        ILiveStateHashReader? liveStateReader = null)
    {
        if (log is null)
            return Refused("log-missing", "Attested state log is required.", 0);

        if (schema is null)
            return Refused("schema-missing", "State schema is required.", 0);

        if (resolver is null)
            return Refused("resolver-missing", "Certificate resolver is required.", 0);

        var verified = 0;
        string? priorResultingStateHash = null;
        string? priorEntryHash = null;

        for (var index = 0; index < log.Transitions.Count; index++)
        {
            var transition = log.Transitions[index];
            var transitionLabel = $"transition[{index}]";

            if (!schema.IsValidStateHash(transition.ResultingStateHash))
            {
                return Refused(
                    "schema-violation",
                    $"{transitionLabel} has a resulting state hash that violates the schema.",
                    verified);
            }

            if (index == 0)
            {
                if (!string.Equals(transition.PrevEntryHash, CertifiedTransition.GenesisPrevEntryHash, StringComparison.Ordinal))
                {
                    return Refused(
                        "chain-prev-entry-break",
                        $"{transitionLabel} must start the hash chain with an empty prev-entry hash.",
                        verified);
                }
            }
            else
            {
                if (!string.Equals(transition.PriorStateHash, priorResultingStateHash, StringComparison.Ordinal))
                {
                    return Refused(
                        "chain-reorder",
                        $"{transitionLabel} prior-state hash does not match the prior resulting state hash.",
                        verified);
                }

                if (!string.Equals(transition.PrevEntryHash, priorEntryHash, StringComparison.Ordinal))
                {
                    var code = IsDroppedTransitionGap(log, index, transition.PrevEntryHash, priorEntryHash)
                        ? "chain-gap"
                        : "chain-prev-entry-break";

                    return Refused(
                        code,
                        $"{transitionLabel} prev-entry hash does not link to the prior entry.",
                        verified);
                }
            }

            var expectedEntryHash = TransitionBuilder.ComputeEntryHash(
                transition.PriorStateHash,
                transition.Action,
                transition.BehaviorCertContentHash,
                transition.ResultingStateHash,
                transition.PrevEntryHash);

            if (!string.Equals(transition.EntryHash, expectedEntryHash, StringComparison.Ordinal))
            {
                return Refused(
                    "chain-prev-entry-break",
                    $"{transitionLabel} entry hash does not match its fields.",
                    verified);
            }

            var resolve = resolver.Resolve(transition.BehaviorCertContentHash);
            if (!resolve.Found || resolve.Record is null || resolve.BrickSource is null)
            {
                var code = index == log.Transitions.Count - 1 && log.Transitions.Count > 1
                    ? "injected-behavior-cert-unresolved"
                    : "behavior-cert-unresolved";

                return Refused(
                    code,
                    $"{transitionLabel} behavior certification could not be resolved.",
                    verified);
            }

            var trust = Contracts.CertificationTrustVerifier.Verify(
                resolve.Record,
                resolve.BrickSource,
                hmacKey);

            if (!trust.Trusted)
            {
                return Refused(
                    "behavior-cert-untrusted",
                    $"{transitionLabel} behavior certification is not trusted ({trust.FailureCode}: {trust.Reason}).",
                    verified);
            }

            if (replayer is not null)
            {
                var replay = replayer.Replay(
                    transition.PriorStateHash,
                    transition.Action,
                    resolve.Record,
                    resolve.BrickSource);

                if (!replay.Matches
                    || !string.Equals(replay.ComputedStateHash, transition.ResultingStateHash, StringComparison.Ordinal))
                {
                    return Refused(
                        "replay-mismatch",
                        replay.Reason ?? $"{transitionLabel} replay did not reproduce the resulting state hash.",
                        verified);
                }
            }

            verified++;
            priorResultingStateHash = transition.ResultingStateHash;
            priorEntryHash = transition.EntryHash;
        }

        if (liveStateReader is not null)
        {
            var headBinding = VerifyLiveHeadBinding(log, schema, liveStateReader, verified);
            if (!headBinding.Trusted)
                return headBinding;
        }

        return new StateLogTrustResult(true, null, null, verified);
    }

    private static StateLogTrustResult VerifyLiveHeadBinding(
        AttestedStateLog log,
        StateSchema schema,
        ILiveStateHashReader liveStateReader,
        int verified)
    {
        var head = log.CurrentStateHash;
        if (string.IsNullOrWhiteSpace(head))
        {
            return Refused(
                "live-state-head-missing",
                "Attested state log has no current state hash to bind against live storage.",
                verified);
        }

        var live = liveStateReader.ReadCurrentStateHash();
        if (!live.Found || string.IsNullOrWhiteSpace(live.StateHash))
        {
            return Refused(
                "live-state-unavailable",
                "Live state storage did not yield a current state hash.",
                verified);
        }

        if (!schema.IsValidStateHash(live.StateHash))
        {
            return Refused(
                "live-state-schema-violation",
                "Live state hash violates the schema.",
                verified);
        }

        if (!string.Equals(head, live.StateHash, StringComparison.Ordinal))
        {
            return Refused(
                "live-state-mismatch",
                "Attested log head does not match the live state store hash.",
                verified);
        }

        return new StateLogTrustResult(true, null, null, verified);
    }

    private static bool IsDroppedTransitionGap(
        AttestedStateLog log,
        int index,
        string observedPrevEntryHash,
        string? expectedPrevEntryHash)
    {
        if (string.IsNullOrEmpty(observedPrevEntryHash)
            || string.Equals(observedPrevEntryHash, expectedPrevEntryHash, StringComparison.Ordinal))
        {
            return false;
        }

        for (var priorIndex = 0; priorIndex < index - 1; priorIndex++)
        {
            if (string.Equals(log.Transitions[priorIndex].EntryHash, observedPrevEntryHash, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static StateLogTrustResult Refused(string code, string reason, int verified) =>
        new(false, code, reason, verified);
}
