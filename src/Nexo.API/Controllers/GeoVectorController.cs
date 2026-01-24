using Microsoft.AspNetCore.Mvc;
using Nexo.API.Models;
using Nexo.API.Services;

namespace Nexo.API.Controllers;

/// <summary>
/// Controller for vector feature extraction operations.
/// </summary>
[ApiController]
[Route("api/v1/geovector")]
[Produces("application/json")]
public class GeoVectorController : ControllerBase
{
    private readonly IGeoVectorService _service;
    private readonly ILogger<GeoVectorController> _logger;

    public GeoVectorController(IGeoVectorService service, ILogger<GeoVectorController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Extract vector features from geographic bounds.
    /// </summary>
    /// <param name="request">Vector extraction request</param>
    /// <returns>Job ID for async processing</returns>
    [HttpPost("extract")]
    [ProducesResponseType(typeof(JobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobResponse>> ExtractFeatures([FromBody] VectorExtractionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var jobId = await _service.ExtractFeaturesAsync(request);
            return Accepted(new JobResponse { JobId = jobId, Status = "accepted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting vector features");
            return StatusCode(500, new ErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get vector extraction job status.
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
    /// Download extracted vector features.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="format">Output format (json, geojson)</param>
    /// <returns>Feature file</returns>
    [HttpGet("jobs/{jobId}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadFeatures(string jobId, [FromQuery] string format = "json")
    {
        var filePath = await _service.GetJobOutputPathAsync(jobId, format);
        if (filePath == null || !System.IO.File.Exists(filePath))
        {
            return NotFound(new ErrorResponse { Message = $"Output file not found for job {jobId}" });
        }

        var contentType = format.ToLowerInvariant() == "geojson" ? "application/geo+json" : "application/json";
        var fileName = Path.GetFileName(filePath);
        return PhysicalFile(filePath, contentType, fileName);
    }
}
