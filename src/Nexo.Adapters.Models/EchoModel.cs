using Nexo.Abstractions;

namespace Nexo.Adapters.Models;

public sealed class EchoModel : IModel
{
    public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
    {
        var text = input.Messages.LastOrDefault().content ?? "";
        return Task.FromResult(new ModelOutput(text));
    }
}
