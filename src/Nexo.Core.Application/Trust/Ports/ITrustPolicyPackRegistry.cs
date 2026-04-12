using Nexo.Core.Application.Trust.Models;

namespace Nexo.Core.Application.Trust.Ports;

/// <summary>
/// Registry for versioned trust policy packs and activation status.
/// </summary>
public interface ITrustPolicyPackRegistry
{
    IReadOnlyList<TrustPolicyPackInfo> ListPacks();
    TrustPolicyPack? GetById(string id);
    ActiveTrustPolicyPack? GetActivePack();
    Task<ActiveTrustPolicyPack> ActivateAsync(string id, CancellationToken cancellationToken = default);
}
