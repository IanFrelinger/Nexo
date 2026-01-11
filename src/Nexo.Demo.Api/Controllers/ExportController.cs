using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;
using Nexo.Demo.Api.DTOs;
using Nexo.Infrastructure.Export;

namespace Nexo.Demo.Api.Controllers;

/// <summary>
/// REST API controller for workflow export.
/// </summary>
[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly IWorkflowExporter _exporter;
    
    public ExportController(IWorkflowExporter exporter)
    {
        _exporter = exporter;
    }
    
    /// <summary>
    /// Export a workflow.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExportResultDto>> Export(
        [FromBody] ExportRequest request,
        CancellationToken ct)
    {
        var workflow = BuildWorkflowFromRequest(request.Workflow);
        var config = new ExportConfig
        {
            Mode = Enum.Parse<ExportMode>(request.Mode, ignoreCase: true),
            Target = Enum.Parse<ExportTarget>(request.Target, ignoreCase: true),
            GenerationConfig = request.GenerationConfig is not null
                ? new GenerationConfig
                {
                    VariationsPerItem = request.GenerationConfig.VariationsPerItem,
                    Provider = request.GenerationConfig.Provider,
                    Model = request.GenerationConfig.Model,
                    RequireReview = request.GenerationConfig.RequireReview
                }
                : null,
            IncludeFallbacks = request.IncludeFallbacks,
            Output = new OutputConfig
            {
                Format = Enum.Parse<Nexo.Core.Domain.Export.OutputFormat>(request.OutputFormat, ignoreCase: true),
                Namespace = request.Namespace ?? "Generated"
            }
        };
        
        var result = await _exporter.ExportAsync(workflow, config, ct);
        
        return Ok(ExportResultDto.FromDomain(result));
    }
    
    /// <summary>
    /// Export and download as ZIP.
    /// </summary>
    [HttpPost("download")]
    public async Task<IActionResult> Download(
        [FromBody] ExportRequest request,
        CancellationToken ct)
    {
        var workflow = BuildWorkflowFromRequest(request.Workflow);
        var config = new ExportConfig
        {
            Mode = Enum.Parse<ExportMode>(request.Mode, ignoreCase: true),
            Target = Enum.Parse<ExportTarget>(request.Target, ignoreCase: true),
            Output = new OutputConfig { Format = Nexo.Core.Domain.Export.OutputFormat.Zip }
        };
        
        var result = await _exporter.ExportAsync(workflow, config, ct);
        
        if (!result.Success)
        {
            return BadRequest(result.Messages);
        }
        
        // Create zip archive
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var file in result.Files)
            {
                var entry = archive.CreateEntry(file.Path);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(file.Content);
            }
        }
        
        memoryStream.Position = 0;
        return File(memoryStream.ToArray(), "application/zip", "nexo-export.zip");
    }
    
    private static Workflow BuildWorkflowFromRequest(ExportWorkflowRequest request)
    {
        return new Workflow
        {
            Id = request.Id ?? Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            Instances = request.Instances.Select(i => new ClusterInstance
            {
                ClusterId = i.ClusterId,
                InstanceId = i.InstanceId,
                Name = i.Name,
                Parameters = i.Parameters,
                ImplementationOverrides = i.ImplementationOverrides
                    .ToDictionary(kv => kv.Key, kv => Enum.Parse<Nexo.Core.Domain.Bricks.ImplementationType>(kv.Value, ignoreCase: true))
            }).ToList(),
            Connections = request.Connections.Select(c => new WorkflowConnection
            {
                Id = c.Id,
                FromInstanceId = c.FromInstanceId,
                FromOutput = c.FromOutput,
                ToInstanceId = c.ToInstanceId,
                ToInput = c.ToInput
            }).ToList()
        };
    }
}

/// <summary>
/// Request to export a workflow.
/// </summary>
public record ExportRequest(
    ExportWorkflowRequest Workflow,
    string Mode,
    string Target,
    ExportGenerationConfig? GenerationConfig,
    bool IncludeFallbacks,
    string OutputFormat,
    string? Namespace
);

public record ExportWorkflowRequest(
    string? Id,
    string Name,
    string Description,
    IReadOnlyList<ExportInstanceRequest> Instances,
    IReadOnlyList<ExportConnectionRequest> Connections
);

public record ExportInstanceRequest(
    string ClusterId,
    string InstanceId,
    string? Name,
    Dictionary<string, object> Parameters,
    Dictionary<string, string> ImplementationOverrides
);

public record ExportConnectionRequest(
    string Id,
    string FromInstanceId,
    string FromOutput,
    string ToInstanceId,
    string ToInput
);

public record ExportGenerationConfig(
    int VariationsPerItem,
    string Provider,
    string Model,
    bool RequireReview
);

