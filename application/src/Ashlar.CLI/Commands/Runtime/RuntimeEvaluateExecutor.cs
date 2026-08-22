namespace Ashlar.CLI.Commands.Runtime;
internal delegate Task<int> RuntimeEvaluateExecutor(
    string? goalsJson, string? goalsFile, string policiesCsv, string repoRoot, string? provider, bool allowMock, bool runTests, string testFilter, string bootstrapProfile, string? runtimeManifestPath, string? runtimeManifestJson, int? maxIterationsOverride, bool bootstrapApply, bool runPreflight, bool useHistory, int historyWindow, bool persistHistory, string benchmarkSet, bool allowVisualCapabilityDegrade, bool json, CancellationToken ct);
