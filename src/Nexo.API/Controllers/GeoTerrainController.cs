using Microsoft.AspNetCore.Mvc;
using Nexo.API.Models;
using Nexo.API.Services;
using Nexo.Adapters.GeoTerrain.Validation;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Validation;

namespace Nexo.API.Controllers;

/// <summary>
/// Controller for terrain generation operations.
/// </summary>
[ApiController]
[Route("api/v1/geoterrain")]
[Produces("application/json")]
public class GeoTerrainController : BaseGeospatialController<IGeoTerrainService>
{
    public GeoTerrainController(IGeoTerrainService service, ILogger<GeoTerrainController> logger)
        : base(service, logger)
    {
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
    /// Download generated terrain mesh.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="format">Output format (obj, gltf, glb, fbx, usd)</param>
    /// <returns>Mesh file</returns>
    /// <summary>
    /// Download generated terrain mesh.
    /// </summary>
    [HttpGet("jobs/{jobId}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadTerrain(string jobId, [FromQuery] string format = "obj")
    {
        return await DownloadFile(jobId, format, "application/octet-stream", GetContentType);
    }

    /// <summary>
    /// Validate mesh quality and integrity.
    /// </summary>
    /// <param name="request">Validation request with mesh file path</param>
    /// <returns>Validation report</returns>
    [HttpPost("validate-mesh")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ValidationResponse>> ValidateMesh([FromBody] MeshValidationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<ActionResult<ValidationResponse>>(BadRequest(ModelState));
        }

        try
        {
            if (string.IsNullOrWhiteSpace(request.MeshPath))
            {
                return Task.FromResult<ActionResult<ValidationResponse>>(
                    BadRequest(new ErrorResponse { Message = "MeshPath is required" }));
            }

            if (!File.Exists(request.MeshPath))
            {
                return Task.FromResult<ActionResult<ValidationResponse>>(
                    NotFound(new ErrorResponse { Message = $"Mesh file not found: {request.MeshPath}" }));
            }

            // Mesh file validation is complex and requires format-specific parsers (OBJ, glTF, etc.)
            // For now, provide a helpful error message with alternatives
            var issues = new List<string>
            {
                "Mesh validation from file path is not yet fully implemented in the API.",
                "To validate mesh quality, use one of these options:",
                "1. Use the CLI: nexo geoterrain bounds-to-obj --bounds ... --mesh-quality-report --output mesh.obj",
                "2. Generate terrain via API with validation flags (coming soon)",
                "3. Use the programmatic SDK: MeshQualityMetrics.ComputeTriangleQuality(mesh)"
            };

            return Task.FromResult<ActionResult<ValidationResponse>>(
                StatusCode(501, new ValidationResponse
                {
                    IsValid = false,
                    Issues = issues
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating mesh");
            return Task.FromResult<ActionResult<ValidationResponse>>(
                StatusCode(500, new ErrorResponse { Message = $"Validation error: {ex.Message}" }));
        }
    }

    /// <summary>
    /// Validate data integrity of elevation grid.
    /// </summary>
    /// <param name="request">Validation request with grid bounds</param>
    /// <returns>Integrity validation report</returns>
    [HttpPost("validate-integrity")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ValidationResponse>> ValidateIntegrity([FromBody] IntegrityValidationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<ActionResult<ValidationResponse>>(BadRequest(ModelState));
        }

        try
        {
            // Parse bounds using GeoBounds.Parse for proper validation
            GeoBounds bounds;
            try
            {
                bounds = GeoBounds.Parse(request.Bounds);
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult<ActionResult<ValidationResponse>>(
                    BadRequest(new ErrorResponse { Message = $"Invalid bounds format: {ex.Message}. Expected format: minLat,minLon,maxLat,maxLon" }));
            }

            // Validate projection parameters
            var isValid = DataIntegrityChecker.ValidateProjectionParameters(bounds, _logger);
            var issues = new List<string>();

            if (!isValid)
            {
                issues.Add("Projection parameters validation failed - bounds may be invalid or out of range");
            }

            // Note: Full corruption detection requires loading elevation grid data.
            // For bounds-only validation, we only check projection parameters.
            // To perform full corruption detection, use the terrain generation endpoint with --validate-integrity flag.

            return Task.FromResult<ActionResult<ValidationResponse>>(
                Ok(new ValidationResponse
                {
                    IsValid = isValid,
                    Issues = issues
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating integrity");
            return Task.FromResult<ActionResult<ValidationResponse>>(
                StatusCode(500, new ErrorResponse { Message = $"Validation error: {ex.Message}" }));
        }
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
