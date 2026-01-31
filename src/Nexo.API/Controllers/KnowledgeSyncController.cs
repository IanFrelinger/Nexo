using Microsoft.AspNetCore.Mvc;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.Networking.Ports;

namespace Nexo.API.Controllers;

/// <summary>
/// Receives knowledge chunks from peers and exposes local chunks and sync status.
/// </summary>
[ApiController]
[Route("api/knowledge")]
public class KnowledgeSyncController : ControllerBase
{
    private readonly IKnowledgeChunkStore _store;
    private readonly IKnowledgeSyncService _syncService;

    public KnowledgeSyncController(IKnowledgeChunkStore store, IKnowledgeSyncService syncService)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
    }

    /// <summary>Receive a chunk from a peer (store locally).</summary>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PostChunk([FromBody] KnowledgeChunk? chunk, CancellationToken cancellationToken)
    {
        if (chunk?.Id == null || chunk.SourceNodeId == null)
            return BadRequest();
        await _store.AddAsync(chunk, cancellationToken).ConfigureAwait(false);
        return Accepted();
    }

    /// <summary>List stored chunks (for pull from peers). Optional contentType and maxCount.</summary>
    [HttpGet("sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KnowledgeChunk>>> GetChunks(
        [FromQuery] string? contentType,
        [FromQuery] int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        var chunks = await _store.GetAsync(contentType, maxCount, cancellationToken).ConfigureAwait(false);
        return Ok(chunks);
    }

    /// <summary>Sync status: this node id, peer ids, last sync times.</summary>
    [HttpGet("sync/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<KnowledgeSyncStatus>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _syncService.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return Ok(status);
    }
}
