using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Orchestration.Configuration;

/// <summary>
/// A configuration warning.
/// 
/// Contains:
/// - Setting name that has the warning
/// - Warning message describing the issue
/// 
/// Used by ConfigurationValidator to report validation warnings.
/// </summary>
public sealed record ConfigurationWarning
{
    public required string Setting { get; init; }
    public required string Message { get; init; }
}
