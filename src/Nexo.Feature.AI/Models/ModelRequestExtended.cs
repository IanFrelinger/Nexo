using System.Collections.Generic;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Extended request model for AI model execution with metadata support
/// </summary>
public record ModelRequestExtended : ModelRequest
{
    /// <summary>
    /// Additional metadata for the request
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
