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
public class GeoVectorController : BaseGeospatialController<IGeoVectorService>
{
    public GeoVectorController(IGeoVectorService service, ILogger<GeoVectorController> logger)
        : base(service, logger)
    {
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
    /// Download extracted vector features.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="format">Output format (json, geojson)</param>
    /// <returns>Feature file</returns>
    /// <summary>
    /// Download extracted vector features.
    /// </summary>
    [HttpGet("jobs/{jobId}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadFeatures(string jobId, [FromQuery] string format = "json")
    {
        return await DownloadFile(
            jobId, 
            format, 
            "application/json", 
            f => f.ToLowerInvariant() == "geojson" ? "application/geo+json" : "application/json");
    }
}
