using Microsoft.AspNetCore.Mvc;
using Nexo.API.Models;
using Nexo.API.Services;
using Nexo.GeoTerrain;

namespace Nexo.API.Controllers;

/// <summary>
/// Controller for terrain generation operations.
/// </summary>
[ApiController]
[Route("api/v1/geoterrain")]
[Produces("application/json")]
public class GeoTerrainController : ControllerBase
{
    private readonly IGeoTerrainService _service;
    private readonly ILogger<GeoTerrainController> _logger;

    public GeoTerrainController(IGeoTerrainService service, ILogger<GeoTerrainController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Generate a terrain mesh from elevation data.
    /// </summary>
    /// <param name="request">Terrain generation request</param>
    /// <returns>Job ID for async processing</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(JobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobResponse>> GenerateTerrain([FromBody] TerrainGenerationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var jobId = await _service.GenerateTerrainAsync(request);
            return Accepted(new JobResponse { JobId = jobId, Status = "accepted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating terrain");
            return StatusCode(500, new ErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get terrain generation job status.
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
    /// Download generated terrain mesh.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="format">Output format (obj, gltf, glb, fbx, usd)</param>
    /// <returns>Mesh file</returns>
    [HttpGet("jobs/{jobId}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadTerrain(string jobId, [FromQuery] string format = "obj")
    {
        var filePath = await _service.GetJobOutputPathAsync(jobId, format);
        if (filePath == null || !System.IO.File.Exists(filePath))
        {
            return NotFound(new ErrorResponse { Message = $"Output file not found for job {jobId}" });
        }

        var contentType = GetContentType(format);
        var fileName = Path.GetFileName(filePath);
        return PhysicalFile(filePath, contentType, fileName);
    }

    private static string GetContentType(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "obj" => "model/obj",
            "gltf" => "model/gltf+json",
            "glb" => "model/gltf-binary",
            "fbx" => "application/octet-stream",
            "usd" => "model/vnd.usd",
            _ => "application/octet-stream"
        };
    }
}
