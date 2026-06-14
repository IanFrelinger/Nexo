using Nexo.BackgroundAgents.HostRunners;

namespace Nexo.CLI.Commands;

internal sealed class AssetsHandler(Func<SelfExtendRunnerAdapter> runnerFactory)
{
    public async Task<int> ExecuteAsync(
        string projectRoot,
        string assetType,
        string description,
        bool json,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!UnityDevCommand.ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var validTypes = new[] { "material", "prefab", "scene", "audio", "animation", "soundbank", "animationset", "ui", "vfx", "physics", "input", "network", "ainavigation", "build" };
        var normalizedType = assetType.ToLowerInvariant();
        if (!validTypes.Contains(normalizedType))
        {
            UnityDevCommand.WriteError($"Invalid asset type '{assetType}'. Must be one of: {string.Join(", ", validTypes)}", json);
            return 1;
        }

        var assetDir = Path.Combine(fullProjectRoot, "Assets", "NexoAssets", normalizedType);
        Directory.CreateDirectory(assetDir);

        UnityDevCommand.SetPathAllowlist(fullProjectRoot, assetDir);

        var prompt = UnityDevCommand.BuildAssetPrompt(normalizedType, description);

        var runner = runnerFactory();
        var result = await runner.RunAsync(fullProjectRoot, prompt, "unity-dev-assets", ct).ConfigureAwait(false);

        if (!result.Success)
        {
            UnityDevCommand.WriteError($"Asset generation failed: {result.Summary}", json);
            return 1;
        }

        var files = UnityDevCommand.ParseFiles(result.Summary);
        var writtenFiles = new List<string>();

        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(fullProjectRoot, relativePath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(fullPath, content, ct).ConfigureAwait(false);
            writtenFiles.Add(relativePath);
        }

        UnityDevCommand.WriteManifest(assetDir, description, writtenFiles);
        UnityDevCommand.WriteAssetsResult(writtenFiles, normalizedType, description, json);
        return 0;
    }
}
