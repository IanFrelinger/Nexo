using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Ashlar.Orchestration.Configuration;

/// <summary>
/// A configuration error.
/// 
/// Contains:
/// - Setting name that has the error
/// - Error message describing the issue
/// 
/// Used by ConfigurationValidator to report validation errors.
/// </summary>
public sealed record ConfigurationError
{
    public required string Setting { get; init; }
    public required string Message { get; init; }
}
