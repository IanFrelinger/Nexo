using Microsoft.AspNetCore.Mvc;
using Nexo.BrickContracts;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure.Execution;
using BrickEntity = Nexo.Core.Domain.Bricks.Brick;

namespace Nexo.API.Controllers;

/// <summary>
/// Brick catalog and execute API for networked brick sharing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BricksController : ControllerBase
{
    private readonly Nexo.Infrastructure.Execution.IBrickRegistry _brickRegistry;

    public BricksController(Nexo.Infrastructure.Execution.IBrickRegistry brickRegistry)
    {
        _brickRegistry = brickRegistry ?? throw new ArgumentNullException(nameof(brickRegistry));
    }

    /// <summary>List all bricks in the catalog.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(BrickCatalogResponseDto), StatusCodes.Status200OK)]
    public ActionResult<BrickCatalogResponseDto> GetBricks()
    {
        var bricks = _brickRegistry.GetAllBricks();
        var dtos = bricks.Select(MapToCatalogEntry).ToList();
        return Ok(new BrickCatalogResponseDto
        {
            WireFormatVersion = WireFormatVersion.Current,
            Bricks = dtos
        });
    }

    /// <summary>Get a single brick by id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BrickCatalogEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BrickCatalogEntryDto> GetBrick(string id)
    {
        var brick = _brickRegistry.GetBrick(id);
        if (brick == null)
            return NotFound();
        return Ok(MapToCatalogEntry(brick));
    }

    /// <summary>Execute a brick by id with the given input and context.</summary>
    [HttpPost("{id}/execute")]
    [ProducesResponseType(typeof(BrickExecuteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrickExecuteResponseDto>> ExecuteBrick(string id, [FromBody] BrickExecuteRequestDto? request, CancellationToken cancellationToken)
    {
        var brick = _brickRegistry.GetBrick(id);
        if (brick == null)
            return NotFound();

        if (request == null)
            return BadRequest(new BrickExecuteResponseDto { Success = false, Error = "Request body is required." });

        if (!Enum.TryParse<ImplementationType>(request.Implementation, ignoreCase: true, out var implementationType)
            || (implementationType != ImplementationType.Deterministic && implementationType != ImplementationType.Agentic))
        {
            return BadRequest(new BrickExecuteResponseDto { Success = false, Error = "Implementation must be Deterministic or Agentic." });
        }

        try
        {
            var input = BrickValueSerializer.FromWireToBrickInput(request.Input);
            var context = MapToExecutionContext(request.ExecutionContext);

            var output = await brick.ExecuteAsync(input, implementationType, context, cancellationToken);

            var wireOutput = BrickValueSerializer.ToWireDictionary(output);
            return Ok(new BrickExecuteResponseDto
            {
                WireFormatVersion = WireFormatVersion.Current,
                Success = true,
                Summary = output.Summary,
                Output = wireOutput
            });
        }
        catch (Exception ex)
        {
            return Ok(new BrickExecuteResponseDto
            {
                WireFormatVersion = WireFormatVersion.Current,
                Success = false,
                Error = ex.Message
            });
        }
    }

    private static Nexo.Core.Domain.Execution.IExecutionContext MapToExecutionContext(ExecutionContextDto? dto)
    {
        if (dto == null)
            return new Nexo.Infrastructure.Execution.ExecutionContext { AgentId = "", BehaviorId = "", Provider = "openai" };

        return new Nexo.Infrastructure.Execution.ExecutionContext
        {
            AgentId = dto.AgentId ?? "",
            BehaviorId = dto.BehaviorId ?? "",
            IsAirGapped = dto.IsAirGapped,
            AuditMode = dto.AuditMode,
            Provider = dto.Provider ?? "openai",
            Variables = dto.Variables != null ? new Dictionary<string, object>(dto.Variables) : new Dictionary<string, object>()
        };
    }

    private static BrickCatalogEntryDto MapToCatalogEntry(BrickEntity brick)
    {
        return new BrickCatalogEntryDto
        {
            WireFormatVersion = WireFormatVersion.Current,
            Id = brick.Id,
            Name = brick.Name,
            Version = brick.Version,
            Icon = brick.Icon,
            Category = brick.Category.ToString(),
            Description = brick.Description,
            HostBaseUrl = null,
            Interface = new BrickInterfaceDto
            {
                Inputs = brick.Interface.Inputs.Select(i => new BrickInputDefinitionDto
                {
                    Name = i.Name,
                    Type = i.Type,
                    Description = i.Description,
                    Required = i.Required,
                    Default = i.Default
                }).ToList(),
                Outputs = brick.Interface.Outputs.Select(o => new BrickOutputDefinitionDto
                {
                    Name = o.Name,
                    Type = o.Type,
                    Description = o.Description
                }).ToList()
            },
            HasDeterministic = brick.Implementations.HasDeterministic,
            HasAgentic = brick.Implementations.HasAgentic,
            Metadata = new BrickMetadataDto
            {
                Author = brick.Metadata.Author,
                License = brick.Metadata.License,
                Repository = brick.Metadata.Repository,
                UsageCount = brick.Metadata.UsageCount,
                LastUpdated = brick.Metadata.LastUpdated
            }
        };
    }
}
