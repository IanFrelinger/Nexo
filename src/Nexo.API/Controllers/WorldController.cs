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
public class WorldController : ControllerBase
{
    private readonly IWorldService _service;
    private readonly ILogger<WorldController> _logger;

    public WorldController(IWorldService service, ILogger<WorldController> logger)
    {
        _service = service;
        _logger = logger;
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
    /// Get world generation job status.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <returns>Job status and result if complete</returns>
    [HttpGet("jobs/{jobId}")]
    [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobStatusResponse>> GetJobStatus(string jobId)
    {
        var status = await _service.GetJobStatusAsync(jobId);
        if (status == null)
        {
            return NotFound(new ErrorResponse { Message = $"Job {jobId} not found" });
        }

        return Ok(status);
    }

    /// <summary>
    /// Download generated world bundle as ZIP archive.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <returns>World bundle ZIP file</returns>
    [HttpGet("jobs/{jobId}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadWorld(string jobId)
    {
        var filePath = await _service.GetJobOutputPathAsync(jobId, "zip");
        if (filePath == null || !System.IO.File.Exists(filePath))
        {
            return NotFound(new ErrorResponse { Message = $"Output file not found for job {jobId}" });
        }

        var fileName = Path.GetFileName(filePath);
        return PhysicalFile(filePath, "application/zip", fileName);
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
