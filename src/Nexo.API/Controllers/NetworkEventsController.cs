using Microsoft.AspNetCore.Mvc;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.Networking.Ports;

namespace Nexo.API.Controllers;

/// <summary>
/// Receives network events and exposes peer/status for the networked event bus.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NetworkEventsController : ControllerBase
{
    private readonly INetworkBus _networkBus;

    public NetworkEventsController(INetworkBus networkBus)
    {
        _networkBus = networkBus ?? throw new ArgumentNullException(nameof(networkBus));
    }

    /// <summary>Receive an event from the wire (dispatch to subscribers, optionally relay).</summary>
    [HttpPost("events")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PostEvent([FromBody] NetworkEvent? evt, CancellationToken cancellationToken)
    {
        if (evt?.EventId == null || evt.SourceNodeId == null || evt.EventType == null)
            return BadRequest();
        await _networkBus.DeliverAsync(evt, cancellationToken).ConfigureAwait(false);
        return Accepted();
    }

    /// <summary>Return list of known peers and their status.</summary>
    [HttpGet("peers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PeersResponse> GetPeers()
    {
        var peers = _networkBus.ConnectedPeers;
        return Ok(new PeersResponse
        {
            NodeId = _networkBus.NodeId,
            Peers = peers.Select(p => new PeerInfo { NodeId = p, Status = "known" }).ToList()
        });
    }

    /// <summary>Return this node's ID, uptime, peer count.</summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<NetworkStatusResponse> GetStatus()
    {
        return Ok(new NetworkStatusResponse
        {
            NodeId = _networkBus.NodeId,
            PeerCount = _networkBus.ConnectedPeers.Count
        });
    }

    public sealed class PeersResponse
    {
        public string NodeId { get; set; } = "";
        public List<PeerInfo> Peers { get; set; } = new();
    }

    public sealed class PeerInfo
    {
        public string NodeId { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public sealed class NetworkStatusResponse
    {
        public string NodeId { get; set; } = "";
        public int PeerCount { get; set; }
    }
}
