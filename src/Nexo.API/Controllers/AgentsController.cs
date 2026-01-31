using Microsoft.AspNetCore.Mvc;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.Networking.Ports;

namespace Nexo.API.Controllers;

/// <summary>
/// Registers and lists agents on this node; exposes network directory.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly INetworkAgentDirectory _directory;
    private readonly INetworkNegotiationService _negotiationService;

    public AgentsController(INetworkAgentDirectory directory, INetworkNegotiationService negotiationService)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _negotiationService = negotiationService ?? throw new ArgumentNullException(nameof(negotiationService));
    }

    /// <summary>List agents on this node (used by peers for discovery).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NetworkAgentEntry>>> List(
        [FromQuery] bool includeNetwork = false,
        [FromQuery] bool refreshFromPeers = false,
        CancellationToken cancellationToken = default)
    {
        if (includeNetwork)
        {
            var all = await _directory.GetAllAsync(refreshFromPeers, cancellationToken).ConfigureAwait(false);
            return Ok(all);
        }
        var local = await _directory.GetByNodeAsync(_directory.NodeId, cancellationToken).ConfigureAwait(false);
        return Ok(local);
    }

    /// <summary>Register or update a local agent.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Register([FromBody] NetworkAgentEntry? entry, CancellationToken cancellationToken)
    {
        if (entry?.AgentId == null)
            return BadRequest();
        await _directory.RegisterAsync(entry, cancellationToken).ConfigureAwait(false);
        return Accepted();
    }

    /// <summary>Unregister a local agent.</summary>
    [HttpDelete("{agentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Unregister(string agentId, CancellationToken cancellationToken)
    {
        await _directory.UnregisterAsync(agentId, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Get agent by id (local or from directory).</summary>
    [HttpGet("{agentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NetworkAgentEntry>> GetById(string agentId, CancellationToken cancellationToken)
    {
        var entry = await _directory.GetByAgentIdAsync(agentId, cancellationToken).ConfigureAwait(false);
        if (entry == null) return NotFound();
        return Ok(entry);
    }

    /// <summary>Find agents by capability (e.g. schema-negotiation, resource).</summary>
    [HttpGet("network/by-capability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NetworkAgentEntry>>> FindByCapability(
        [FromQuery] string[]? capabilities,
        [FromQuery] bool refreshFromPeers = true,
        CancellationToken cancellationToken = default)
    {
        var caps = capabilities ?? Array.Empty<string>();
        var list = await _negotiationService.FindByCapabilityAsync(caps, refreshFromPeers, cancellationToken).ConfigureAwait(false);
        return Ok(list);
    }

    /// <summary>Find agents by domain.</summary>
    [HttpGet("network/by-domain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NetworkAgentEntry>>> FindByDomain(
        [FromQuery] string domain,
        [FromQuery] bool refreshFromPeers = true,
        CancellationToken cancellationToken = default)
    {
        var list = await _negotiationService.FindByDomainAsync(domain ?? "", refreshFromPeers, cancellationToken).ConfigureAwait(false);
        return Ok(list);
    }
}
