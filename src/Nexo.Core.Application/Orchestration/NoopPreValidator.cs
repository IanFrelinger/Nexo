using Nexo.Core.Application.Interfaces;

namespace Nexo.Core.Application.Orchestration;

/// <summary>Pre-execution validator that always succeeds. Default for minimal hosts.</summary>
public sealed class NoopPreValidator : IPreValidator
{
    /// <inheritdoc />
    public ValueTask<(bool ok, string? reason)> ValidateAsync(object input, CancellationToken ct)
        => new ValueTask<(bool ok, string? reason)>((true, (string?)null));
}
