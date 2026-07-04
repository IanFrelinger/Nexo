using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;
internal delegate Task<RuntimeExecuteResult> RuntimeExecuteCore(
    string goal,
    string repoRoot,
    string? provider,
    bool allowMock,
    bool runTests,
    string testFilter,
    string bootstrapProfile,
    string qaPolicy,
    string? runtimeManifestPath,
    string? runtimeManifestJson,
    int? maxIterationsOverride,
    bool bootstrapApply,
    bool bootstrapYes,
    bool bootstrapDryRun,
    bool runPreflight,
    bool useHistory,
    int historyWindow,
    bool persistHistory,
    string benchmarkSet,
    bool allowVisualCapabilityDegrade,
    CancellationToken ct);
