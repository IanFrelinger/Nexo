using System.Text;
using System.Text.Json;
using Ashlar.CLI.Unity.Pipeline;

namespace Ashlar.CLI.Commands.Unity;
internal delegate Task<int> UnityGenerateExecutor(
    string projectRoot,
    string systemDescription,
    string outputDir,
    string testDir,
    bool dryRun,
    bool json,
    CancellationToken ct,
    string? templatePath = null,
    string? compositionContext = null);
