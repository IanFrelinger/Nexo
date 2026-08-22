using Ashlar.Core.Application.Interfaces;

namespace Ashlar.Core.Application.Orchestration;

/// <summary>Validates command input before execution.</summary>
public interface IPreValidator
{
    /// <summary>Returns (ok, reason) where reason explains failures.</summary>
    ValueTask<(bool ok, string? reason)> ValidateAsync(object input, CancellationToken ct);
}
