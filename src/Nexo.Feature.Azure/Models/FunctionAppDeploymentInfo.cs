using System;
using Nexo.Feature.Azure.Enums;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Function app deployment information
/// </summary>
public record FunctionAppDeploymentInfo
{
    public string ResourceGroupName { get; init; } = string.Empty;
    public string FunctionAppName { get; init; } = string.Empty;
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime DeployedAt { get; init; }
    public string Runtime { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public AppServicePlanType PlanType { get; init; }
    public string Url { get; init; } = string.Empty;
} 