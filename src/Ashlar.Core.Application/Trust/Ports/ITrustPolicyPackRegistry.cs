using Ashlar.Core.Application.Trust.Models;

namespace Ashlar.Core.Application.Trust.Ports;

/// <summary>
/// Registry for versioned trust policy packs and activation status.
/// </summary>
public interface ITrustPolicyPackRegistry
{
    /// <summary>Lists all available policy packs.</summary>
    IReadOnlyList<TrustPolicyPackInfo> ListPacks();

    /// <summary>Loads a policy pack by identifier.</summary>
    /// <param name="id">Pack identifier.</param>
    TrustPolicyPack? GetById(string id);

    /// <summary>Returns the currently active policy pack, if any.</summary>
    ActiveTrustPolicyPack? GetActivePack();

    /// <summary>Activates a policy pack by identifier.</summary>
    /// <param name="id">Pack identifier to activate.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<ActiveTrustPolicyPack> ActivateAsync(string id, CancellationToken cancellationToken = default);
}
