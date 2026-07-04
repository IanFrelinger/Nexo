using Nexo.Core.Application.Interfaces;

namespace Nexo.Core.Application.Orchestration;

/// <summary>Validates command output after execution.</summary>
/// <typeparam name="TOut">Command output type.</typeparam>
public interface IPostValidator<TOut>
{
    /// <summary>Returns (ok, reason) where reason explains failures.</summary>
    ValueTask<(bool ok, string? reason)> ValidateAsync(TOut output, CancellationToken ct);
}
