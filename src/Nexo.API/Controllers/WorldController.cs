using Microsoft.AspNetCore.Mvc;
using Nexo.API.Models;
using Nexo.API.Services;

namespace Nexo.API.Controllers;

/// <summary>
/// Controller for world bundle generation operations.
/// </summary>
[ApiController]
[Route("api/v1/world")]
[Produces("application/json")]
public class WorldController : BaseGeospatialController<IWorldService>
{
    public WorldController(IWorldService service, ILogger<WorldController> logger)
        : base(service, logger)
    {
    }

    /// <summary>
    /// Generate a complete world bundle with terrain, buildings, roads, water, and vegetation.
    /// </summary>
    /// <param name="request">World generation request</param>
    /// <returns>Job ID for async processing</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(JobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobResponse>> GenerateWorld([FromBody] WorldGenerationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var jobId = await _service.GenerateWorldAsync(request);
            return Accepted(new JobResponse { JobId = jobId, Status = "accepted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating world");
            return StatusCode(500, new ErrorResponse { Message = ex.Message });
        }
    }


    /// <summary>
    /// Download generated world bundle as ZIP archive.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <returns>World bundle ZIP file</returns>
    /// <summary>
    /// Download generated world bundle as ZIP archive.
    /// </summary>
    [HttpGet("jobs/{jobId}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadWorld(string jobId)
    {
        return await DownloadFile(jobId, "zip", "application/zip");
    }

    /// <summary>
    /// Validate a world bundle manifest.
    /// </summary>
    /// <param name="request">Validation request</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ValidationResponse>> ValidateWorld([FromBody] WorldValidationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _service.ValidateWorldAsync(request.BundlePath);
            return Ok(new ValidationResponse { IsValid = result.IsValid, Issues = result.Issues });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating world");
            return StatusCode(500, new ErrorResponse { Message = ex.Message });
        }
    }
}
