using Nexo.Core.Application.Orchestration;

namespace Nexo.Core.Application.Orchestration;
public sealed class NoopPreValidator : IPreValidator
{
    public ValueTask<(bool ok, string? reason)> ValidateAsync(object input, CancellationToken ct)
        => ValueTask.FromResult((true, (string?)null));
}
