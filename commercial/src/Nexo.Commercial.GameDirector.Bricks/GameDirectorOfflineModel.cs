using Nexo.Abstractions;

namespace GameDirector.Bricks;

/// <summary>
/// Fast local model stub for air-gapped / no-Ollama startup.
/// </summary>
public sealed class GameDirectorOfflineModel : IModel
{
    public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
    {
        var last = input.Messages.LastOrDefault().content ?? "";
        return Task.FromResult(new ModelOutput($"[offline] {last}"));
    }
}
