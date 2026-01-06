using Microsoft.AspNetCore.Mvc;
using Nexo.Demo.Api.DTOs;
using Nexo.Infrastructure.Execution;

namespace Nexo.Demo.Api.Controllers;

/// <summary>
/// REST API controller for accessing the catalog of bricks, behaviors, and agents.
/// </summary>
[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly IBrickRegistry _brickRegistry;
    private readonly IBehaviorRegistry _behaviorRegistry;
    private readonly IAgentRegistry _agentRegistry;
    
    public CatalogController(
        IBrickRegistry brickRegistry,
        IBehaviorRegistry behaviorRegistry,
        IAgentRegistry agentRegistry)
    {
        _brickRegistry = brickRegistry;
        _behaviorRegistry = behaviorRegistry;
        _agentRegistry = agentRegistry;
    }
    
    /// <summary>
    /// Get all available bricks with their domain knowledge and implementations.
    /// </summary>
    [HttpGet("bricks")]
    public ActionResult<IEnumerable<BrickDto>> GetBricks()
    {
        var bricks = _brickRegistry.GetAllBricks()
            .Select(BrickDto.FromDomain)
            .ToList();
        return Ok(bricks);
    }
    
    /// <summary>
    /// Get a specific brick by ID.
    /// </summary>
    [HttpGet("bricks/{id}")]
    public ActionResult<BrickDto> GetBrick(string id)
    {
        var brick = _brickRegistry.GetBrick(id);
        if (brick == null)
            return NotFound();
        return Ok(BrickDto.FromDomain(brick));
    }
    
    /// <summary>
    /// Get all available behaviors.
    /// </summary>
    [HttpGet("behaviors")]
    public ActionResult<IEnumerable<BehaviorDto>> GetBehaviors()
    {
        var behaviors = _behaviorRegistry.GetAllBehaviors()
            .Select(BehaviorDto.FromDomain)
            .ToList();
        return Ok(behaviors);
    }
    
    /// <summary>
    /// Get a specific behavior by ID.
    /// </summary>
    [HttpGet("behaviors/{id}")]
    public ActionResult<BehaviorDto> GetBehavior(string id)
    {
        var behavior = _behaviorRegistry.GetBehavior(id);
        if (behavior == null)
            return NotFound();
        return Ok(BehaviorDto.FromDomain(behavior));
    }
    
    /// <summary>
    /// Get all available agents.
    /// </summary>
    [HttpGet("agents")]
    public ActionResult<IEnumerable<AgentDto>> GetAgents()
    {
        var agents = _agentRegistry.GetAllAgents()
            .Select(AgentDto.FromDomain)
            .ToList();
        return Ok(agents);
    }
    
    /// <summary>
    /// Get a specific agent by ID.
    /// </summary>
    [HttpGet("agents/{id}")]
    public ActionResult<AgentDto> GetAgent(string id)
    {
        var agent = _agentRegistry.GetAgent(id);
        if (agent == null)
            return NotFound();
        return Ok(AgentDto.FromDomain(agent));
    }
}

