using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ashlar.Orchestration.Agents.Assets;

/// <summary>
/// Result of constraint evaluation.
/// </summary>
public enum ConstraintResult
{
    Passed,
    Failed,
    Warning
}
