using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Bricks;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Execution plan for a cluster.
/// </summary>
public record ExecutionPlan(IReadOnlyList<ExecutionStep> Steps);
