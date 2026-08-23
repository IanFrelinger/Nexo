using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// DomainBrick registry that only exposes bricks admitted through the certification gate.
/// </summary>
public sealed class CertifiedBrickRegistry : Ashlar.Core.Domain.Execution.IBrickRegistry
{
    private readonly Dictionary<string, DomainBrick> _bricks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICertificationRecordStore _store;
    private readonly CertificationRecordSigner _signer;
    private readonly ILogger<CertifiedBrickRegistry>? _logger;

    /// <summary>Initializes a new certified brick registry.</summary>
    public CertifiedBrickRegistry(
        ICertificationRecordStore store,
        CertificationRecordSigner signer,
        ILogger<CertifiedBrickRegistry>? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _logger = logger;
    }

    /// <summary>Gets brick.</summary>
    public DomainBrick? GetBrick(string id)
    {
        if (!_store.IsAdmitted(id))
            return null;
        return _bricks.TryGetValue(id, out var brick) ? brick : null;
    }

    /// <summary>Gets all bricks.</summary>
    public IReadOnlyList<DomainBrick> GetAllBricks() =>
        _bricks.Values.Where(b => _store.IsAdmitted(b.Id)).ToList();

    internal bool TryAdmit(DomainBrick brick, CertificationRecord record)
    {
        if (!record.Admitted || !record.Signed || !_signer.Verify(record))
        {
            _logger?.LogWarning("Rejected ungated brick admission attempt for {BrickId}", brick.Id);
            return false;
        }

        _store.Save(record);
        _bricks[brick.Id] = brick;
        _logger?.LogInformation("Admitted certified brick {BrickId}", brick.Id);
        return true;
    }

    internal bool ContainsUngated(string brickId) =>
        _bricks.ContainsKey(brickId) && !_store.IsAdmitted(brickId);
}
