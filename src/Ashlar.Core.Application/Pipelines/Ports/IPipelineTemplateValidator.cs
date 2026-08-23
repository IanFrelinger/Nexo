using Ashlar.Core.Application.Pipelines.Models;

namespace Ashlar.Core.Application.Pipelines.Ports;

/// <summary>
/// Validates pipeline templates and returns diagnostics.
/// </summary>
public interface IPipelineTemplateValidator
{
    /// <summary>
    /// Validates template structure, connectivity, and constraint consistency.
    /// </summary>
    /// <param name="template">Template to validate.</param>
    /// <returns>Validation outcome with error messages when invalid.</returns>
    PipelineTemplateValidationResult Validate(PipelineTemplate template);
}
