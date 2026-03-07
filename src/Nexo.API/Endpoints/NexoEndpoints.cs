using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexo.Core.Application.Agent.UseCases.RunAgent;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Infrastructure.Testing.ExecutionPlatform;
using Nexo.Orchestration.Coordination;
using System.Text.Json;

namespace Nexo.API.Endpoints;

/// <summary>
/// Extension methods for mapping Nexo API endpoints.
/// </summary>
public static class NexoEndpoints
{
    public static IEndpointRouteBuilder MapNexoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Nexo");

        group.MapPost("/agent", RunAgentAsync)
            .WithName("RunAgent")
            .WithSummary("Invoke an agent by name")
            .Produces<AgentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/validate", RunValidationAsync)
            .WithName("RunValidation")
            .WithSummary("Run validation tests")
            .Produces<ValidationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/orchestrate", OrchestrateAsync)
            .WithName("Orchestrate")
            .WithSummary("Run orchestration workflow")
            .Produces<OrchestrationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/status", GetStatusAsync)
            .WithName("GetStatus")
            .WithSummary("Get background agent status and mode")
            .Produces<StatusResponse>(StatusCodes.Status200OK);

        group.MapPost("/execution/build", BuildImageAsync)
            .WithName("BuildImage")
            .WithSummary("Build a container image from Dockerfile (for RemoteExecutionPlatform)")
            .Produces<ExecutionBuildResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/execution/run", RunContainerAsync)
            .WithName("RunContainer")
            .WithSummary("Run a container (for RemoteExecutionPlatform)")
            .Produces<ExecutionRunResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> RunAgentAsync(
        [FromBody] AgentRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.AgentName))
            return Results.BadRequest(new ProblemDetails { Title = "AgentName is required" });

        try
        {
            var command = new RunAgentCommand(request.AgentName, request.InputFilePath != null ? new FileInfo(request.InputFilePath) : null);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(new AgentResponse(result.Success, result.Message, result.Output));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> RunValidationAsync(
        [FromBody] ValidationRequest? request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RunValidationCommand(request?.Filter);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(new ValidationResponse(result.Passed, result.Message, result.TestsRun, result.TestsPassed, result.TestsFailed));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> OrchestrateAsync(
        [FromBody] OrchestrationRequest request,
        [FromServices] Orchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Request))
            return Results.BadRequest(new ProblemDetails { Title = "Request is required" });

        try
        {
            var result = await orchestrator.OrchestrateAsync(request.Request, cancellationToken);
            return Results.Ok(new OrchestrationResponse(
                result.Success,
                result.IntegratedOutput != null ? $"{result.IntegratedOutput.AgentOutputs.Count} agent(s) executed" : null,
                result.IntegratedOutput?.IntegratedResults));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetStatusAsync(
        [FromServices] IServiceProvider services,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var modeStore = services.GetService<Nexo.BackgroundAgents.Configuration.IAggressivenessModeStore>();
        var mode = modeStore?.GetMode() ?? Nexo.BackgroundAgents.Configuration.BackgroundAgentAggressivenessMode.Active;
        return Results.Ok(new StatusResponse(mode.ToString(), "Nexo API is running"));
    }

    private static async Task<IResult> BuildImageAsync(
        [FromBody] ExecutionBuildRequest request,
        [FromServices] IExecutionPlatform executionPlatform,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.DockerfilePath) || string.IsNullOrWhiteSpace(request?.ImageTag) || string.IsNullOrWhiteSpace(request?.ContextPath))
            return Results.BadRequest(new ProblemDetails { Title = "DockerfilePath, ImageTag, and ContextPath are required" });

        try
        {
            var result = await executionPlatform.BuildImageAsync(
                request.DockerfilePath,
                request.ImageTag,
                request.ContextPath,
                request.BuildArgs,
                null,
                cancellationToken);
            return Results.Ok(new ExecutionBuildResponse(result.Success, result.ErrorMessage, result.Duration.TotalMilliseconds));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> RunContainerAsync(
        [FromBody] ExecutionRunRequest request,
        [FromServices] IExecutionPlatform executionPlatform,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.ImageTag) || request?.Command == null)
            return Results.BadRequest(new ProblemDetails { Title = "ImageTag and Command are required" });

        try
        {
            var result = await executionPlatform.RunContainerAsync(
                request.ImageTag,
                request.Command,
                request.EnvironmentVariables,
                request.VolumeMounts,
                request.WorkingDirectory,
                cancellationToken);
            return Results.Ok(new ExecutionRunResponse(result.Success, result.ExitCode, result.StandardOutput, result.StandardError, result.ContainerId, result.Duration.TotalMilliseconds));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

// Request/Response DTOs
public sealed record AgentRequest(string AgentName, string? InputFilePath);
public sealed record AgentResponse(bool Success, string? Message, object? Output);

public sealed record ValidationRequest(string? Filter);
public sealed record ValidationResponse(bool Passed, string? Message, int TotalTests, int PassedTests, int FailedTests);

public sealed record OrchestrationRequest(string Request);
public sealed record OrchestrationResponse(bool Success, string? Summary, object? Output);

public sealed record StatusResponse(string Mode, string Message);

public sealed record ExecutionBuildRequest(string DockerfilePath, string ImageTag, string ContextPath, Dictionary<string, string>? BuildArgs = null);
public sealed record ExecutionBuildResponse(bool Success, string? ErrorMessage, double DurationMs);

public sealed record ExecutionRunRequest(string ImageTag, string[] Command, Dictionary<string, string>? EnvironmentVariables = null, Dictionary<string, string>? VolumeMounts = null, string? WorkingDirectory = null);
public sealed record ExecutionRunResponse(bool Success, int ExitCode, string StandardOutput, string StandardError, string? ContainerId, double DurationMs);
