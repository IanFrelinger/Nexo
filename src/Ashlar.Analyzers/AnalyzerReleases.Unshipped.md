; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ASHLAR0001 | Ashlar.BrickContract | Warning | BrickInterfaceDriftAnalyzer: brick reads an input key its BrickInterface does not declare.
ASHLAR0002 | Ashlar.BrickContract | Warning | BrickInterfaceDriftAnalyzer: brick writes an output key its BrickInterface does not declare.
ASHLAR0003 | Ashlar.TrustLoop | Warning | BrickConstructorPurityAnalyzer: brick constructor or initializer performs file-system I/O.
ASHLAR0004 | Ashlar.TrustLoop | Warning | BrickConstructorPurityAnalyzer: brick constructor or initializer performs network access.
ASHLAR0005 | Ashlar.TrustLoop | Warning | SelfRecursiveRegistrationAnalyzer: service factory resolves its own service type (ValidateOnBuild-passing resolution-time hang).
ASHLAR0006 | Ashlar.TrustLoop | Warning | BrickDeterminismAnalyzer: brick reads DateTime.Now/DateTimeOffset.Now (require Utc).
ASHLAR0007 | Ashlar.TrustLoop | Warning | BrickDeterminismAnalyzer: brick uses unseeded randomness (new Random() or Random.Shared).
ASHLAR0008 | Ashlar.TrustLoop | Warning | BrickDeterminismAnalyzer: brick declares mutable static state.
ASHLAR0009 | Ashlar.TrustLoop | Warning | BrickEmptyCatchAnalyzer: empty catch block in brick execution paths.
ASHLAR0010 | Ashlar.TrustLoop | Warning | BrickConstraintManifestAnalyzer: using directive outside the manifest allowlist.
ASHLAR0011 | Ashlar.TrustLoop | Warning | BrickConstraintManifestAnalyzer: resolved reference matches a forbidden API token.
ASHLAR0012 | Ashlar.TrustLoop | Warning | BrickConstraintManifestAnalyzer: resolved reference lives inside a forbidden namespace.
ASHLAR0013 | Ashlar.TrustLoop | Warning | TouchSetReferenceAnalyzer: resolved reference outside the objective's declared touch-set.
ASHLAR0014 | Ashlar.TrustLoop | Warning | TouchSetReferenceAnalyzer: undeclared resolved reference into the trust kernel.
