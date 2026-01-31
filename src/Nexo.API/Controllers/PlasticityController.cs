using Microsoft.AspNetCore.Mvc;
using Nexo.Core.Application.Networking.Ports;

namespace Nexo.API.Controllers;

/// <summary>
/// Exposes plasticity metrics (usage, cache, network, knowledge sync, agent directory).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PlasticityController : ControllerBase
{
    private readonly IPlasticityService _plasticityService;

    public PlasticityController(IPlasticityService plasticityService)
    {
        _plasticityService = plasticityService ?? throw new ArgumentNullException(nameof(plasticityService));
    }

    /// <summary>Get aggregated plasticity metrics.</summary>
    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        var metrics = await _plasticityService.GetMetricsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(metrics);
    }
}
