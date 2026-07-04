using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Assets.Models;
using Nexo.Orchestration.Assets.Ports;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.Orchestration.Agents.Assets;

/// <summary>
/// Result of constraint evaluation.
/// </summary>
public enum ConstraintResult
{
    Passed,
    Failed,
    Warning
}
