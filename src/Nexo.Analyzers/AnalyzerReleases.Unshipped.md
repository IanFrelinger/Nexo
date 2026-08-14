; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NEXO0001 | Nexo.BrickContract | Warning | BrickInterfaceDriftAnalyzer: brick reads an input key its BrickInterface does not declare.
NEXO0002 | Nexo.BrickContract | Warning | BrickInterfaceDriftAnalyzer: brick writes an output key its BrickInterface does not declare.
NEXO0003 | Nexo.TrustLoop | Warning | BrickConstructorPurityAnalyzer: brick constructor or initializer performs file-system I/O.
NEXO0004 | Nexo.TrustLoop | Warning | BrickConstructorPurityAnalyzer: brick constructor or initializer performs network access.
NEXO0005 | Nexo.TrustLoop | Warning | SelfRecursiveRegistrationAnalyzer: service factory resolves its own service type (ValidateOnBuild-passing resolution-time hang).
NEXO0006 | Nexo.TrustLoop | Warning | BrickDeterminismAnalyzer: brick reads DateTime.Now/DateTimeOffset.Now (require Utc).
NEXO0007 | Nexo.TrustLoop | Warning | BrickDeterminismAnalyzer: brick uses unseeded randomness (new Random() or Random.Shared).
NEXO0008 | Nexo.TrustLoop | Warning | BrickDeterminismAnalyzer: brick declares mutable static state.
NEXO0009 | Nexo.TrustLoop | Warning | BrickEmptyCatchAnalyzer: empty catch block in brick execution paths.
NEXO0010 | Nexo.TrustLoop | Warning | BrickConstraintManifestAnalyzer: using directive outside the manifest allowlist.
NEXO0011 | Nexo.TrustLoop | Warning | BrickConstraintManifestAnalyzer: resolved reference matches a forbidden API token.
NEXO0012 | Nexo.TrustLoop | Warning | BrickConstraintManifestAnalyzer: resolved reference lives inside a forbidden namespace.
NEXO0013 | Nexo.TrustLoop | Warning | TouchSetReferenceAnalyzer: resolved reference outside the objective's declared touch-set.
NEXO0014 | Nexo.TrustLoop | Warning | TouchSetReferenceAnalyzer: undeclared resolved reference into the trust kernel.
