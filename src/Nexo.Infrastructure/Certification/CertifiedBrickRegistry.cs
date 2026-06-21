using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Brick registry that only exposes bricks admitted through the certification gate.
/// </summary>
public sealed class CertifiedBrickRegistry : Nexo.Core.Domain.Execution.IBrickRegistry
{
    private readonly Dictionary<string, Brick> _bricks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICertificationRecordStore _store;
    private readonly CertificationRecordSigner _signer;
    private readonly ILogger<CertifiedBrickRegistry>? _logger;

    public CertifiedBrickRegistry(
        ICertificationRecordStore store,
        CertificationRecordSigner signer,
        ILogger<CertifiedBrickRegistry>? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _logger = logger;
    }

    public Brick? GetBrick(string id)
    {
        if (!_store.IsAdmitted(id))
            return null;
        return _bricks.TryGetValue(id, out var brick) ? brick : null;
    }

    public IReadOnlyList<Brick> GetAllBricks() =>
        _bricks.Values.Where(b => _store.IsAdmitted(b.Id)).ToList();

    internal bool TryAdmit(Brick brick, CertificationRecord record)
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
