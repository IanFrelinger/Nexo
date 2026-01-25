using Microsoft.AspNetCore.Mvc;
using Nexo.API.Models;
using Nexo.API.Services;

namespace Nexo.API.Controllers;

/// <summary>
/// Base controller for geospatial operations with common endpoints.
/// </summary>
public abstract class BaseGeospatialController<TService> : ControllerBase
    where TService : IJobStatusService
{
    protected readonly TService _service;
    protected readonly ILogger _logger;

    protected BaseGeospatialController(TService service, ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets job status from the service.
    /// </summary>
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
    /// Streams job progress updates via Server-Sent Events (SSE).
    /// </summary>
    [HttpGet("jobs/{jobId}/progress")]
    [Produces("text/event-stream")]
    public async Task GetJobProgress(string jobId, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        var lastProgress = -1;
        var lastStatus = "";

        while (!cancellationToken.IsCancellationRequested)
        {
            var status = await _service.GetJobStatusAsync(jobId);

            if (status == null)
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = "Job not found" })}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                break;
            }

            // Only send update if status or progress changed
            if (status.Progress != lastProgress || status.Status != lastStatus)
            {
                var update = new
                {
                    jobId = status.JobId,
                    status = status.Status,
                    progress = status.Progress,
                    errorMessage = status.ErrorMessage,
                    outputPath = status.OutputPath,
                    completedAt = status.CompletedAt
                };

                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(update)}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                lastProgress = status.Progress;
                lastStatus = status.Status;

                // Stop streaming if job is completed or failed
                if (status.Status == "completed" || status.Status == "failed")
                {
                    break;
                }
            }

            await Task.Delay(1000, cancellationToken); // Poll every second
        }
    }

    /// <summary>
    /// Downloads job output file.
    /// </summary>
    protected async Task<ActionResult> DownloadFile(string jobId, string format, string defaultContentType, Func<string, string>? contentTypeMapper = null)
    {
        var filePath = await _service.GetJobOutputPathAsync(jobId, format);

        if (filePath == null || !System.IO.File.Exists(filePath))
        {
            return NotFound(new ErrorResponse { Message = $"Output file not found for job {jobId}" });
        }

        var contentType = contentTypeMapper != null ? contentTypeMapper(format) : defaultContentType;
        var fileName = Path.GetFileName(filePath);
        return PhysicalFile(filePath, contentType, fileName);
    }
}
