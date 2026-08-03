using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexo.Core.Application.Scaling.Models;
using Nexo.Core.Application.Scaling.Ports;

namespace Nexo.API.Endpoints;

/// <summary>HTTP surface for swappable container workload scaling.</summary>
public static class WorkloadScalingEndpoints
{
    /// <summary>Maps <c>/api/workloads/*</c> scale endpoints onto the Nexo API group.</summary>
    public static RouteGroupBuilder MapWorkloadScalingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/workloads", ListWorkloadsAsync)
            .WithName("ListWorkloads")
            .WithSummary("List scalable workloads for the active provider (kubernetes|compose|null)")
            .Produces<WorkloadListResponse>(StatusCodes.Status200OK);

        group.MapGet("/workloads/{workloadId}/replicas", GetReplicasAsync)
            .WithName("GetWorkloadReplicas")
            .WithSummary("Get desired/current/ready replica counts for a workload")
            .Produces<WorkloadReplicaSnapshot>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPut("/workloads/{workloadId}/replicas", ScaleWorkloadAsync)
            .WithName("ScaleWorkload")
            .WithSummary("Set desired replica count (provider: kubernetes Deployment or compose service)")
            .Produces<WorkloadScaleResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/workloads/provider", GetProviderAsync)
            .WithName("GetWorkloadScalerProvider")
            .WithSummary("Active workload scaler provider and availability")
            .Produces<WorkloadProviderResponse>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> ListWorkloadsAsync(
        [FromServices] IWorkloadScaler scaler,
        CancellationToken cancellationToken)
    {
        var workloads = await scaler.ListWorkloadsAsync(cancellationToken);
        return Results.Ok(new WorkloadListResponse(scaler.ProviderName, workloads));
    }

    private static async Task<IResult> GetReplicasAsync(
        string workloadId,
        [FromServices] IWorkloadScaler scaler,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await scaler.GetReplicasAsync(workloadId, cancellationToken);
            return Results.Ok(snapshot);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new ProblemDetails { Title = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ScaleWorkloadAsync(
        string workloadId,
        [FromBody] ScaleWorkloadRequest? body,
        [FromServices] IWorkloadScaler scaler,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return Results.BadRequest(new ProblemDetails { Title = "Body is required" });
        if (body.DesiredReplicas < 0)
            return Results.BadRequest(new ProblemDetails { Title = "DesiredReplicas must be >= 0" });

        try
        {
            var result = await scaler.ScaleAsync(
                new WorkloadScaleRequest(workloadId, body.DesiredReplicas, body.Reason),
                cancellationToken);
            return result.Success ? Results.Ok(result) : Results.Problem(
                detail: result.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Scale failed");
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new ProblemDetails { Title = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetProviderAsync(
        [FromServices] IWorkloadScaler scaler,
        CancellationToken cancellationToken)
    {
        var available = await scaler.IsAvailableAsync(cancellationToken);
        return Results.Ok(new WorkloadProviderResponse(scaler.ProviderName, available));
    }
}

/// <summary>PUT body for replica scaling.</summary>
public sealed record ScaleWorkloadRequest(int DesiredReplicas, string? Reason = null);

/// <summary>List response for configured workloads.</summary>
public sealed record WorkloadListResponse(string Provider, IReadOnlyList<WorkloadDescriptor> Workloads);

/// <summary>Active scaler provider probe.</summary>
public sealed record WorkloadProviderResponse(string Provider, bool Available);
