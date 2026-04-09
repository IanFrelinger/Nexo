using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Nexo.BrickContracts;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Execution.Routing;

/// <summary>
/// Executes generation jobs on peer Nexo systems in the local network mesh.
/// </summary>
public sealed class NexoPeerBrickExecutor : IPeerExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NexoPeerBrickExecutor> _logger;
    private readonly IPeerCapabilitySnapshot _snapshot;
    private readonly string _peerBrickId;
    private readonly int _queueDepthThreshold;

    public NexoPeerBrickExecutor(
        IHttpClientFactory httpClientFactory,
        ILogger<NexoPeerBrickExecutor> logger,
        IPeerCapabilitySnapshot snapshot,
        Microsoft.Extensions.Options.IOptions<RunPodBrickConfig> config)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _queueDepthThreshold = Math.Max(0, config?.Value?.QueueDepthThreshold ?? 0);
        _peerBrickId = string.IsNullOrWhiteSpace(config?.Value?.PeerRoutingBrickId)
            ? "generation.capability-routing"
            : config!.Value.PeerRoutingBrickId;
    }

    public async Task<Result<GenerationExecutionResult>> ExecuteAsync(
        RunPodJobPayload payload,
        JobRequirements requirements,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var endpoint = _snapshot.Candidates
            .Where(candidate => IsEligible(candidate, requirements))
            .Select(candidate => candidate.Endpoint?.Trim())
            .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return Result<GenerationExecutionResult>.Failure(
                "peer-routing.no_eligible_peers",
                "No eligible peer endpoint is currently available.");
        }

        var client = _httpClientFactory.CreateClient();
        endpoint = endpoint.TrimEnd('/');
        var uri = $"{endpoint}/api/bricks/{Uri.EscapeDataString(_peerBrickId)}/execute";
        var input = new BrickInput();
        input.Set("payload", payload);
        input.Set("requirements", requirements);
        var wireInput = BrickValueSerializer.ToWireDictionary(input);
        var request = new BrickExecuteRequestDto
        {
            BrickId = _peerBrickId,
            Implementation = ImplementationType.Deterministic.ToString(),
            Input = wireInput,
            ExecutionContext = new ExecutionContextDto
            {
                AgentId = context.AgentId,
                BehaviorId = context.BehaviorId,
                AuditMode = context.AuditMode,
                IsAirGapped = context.IsAirGapped,
                Provider = context.Provider,
                Variables = new Dictionary<string, object>(context.Variables)
            }
        };

        try
        {
            _logger.LogInformation("peer-routing stage=dispatch endpoint={Endpoint} model={ModelId}", endpoint, payload.ModelId);
            using var response = await client.PostAsJsonAsync(uri, request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result<GenerationExecutionResult>.Failure(
                    "peer.http_error",
                    $"Peer execution call failed with status {(int)response.StatusCode}.",
                    response.ReasonPhrase);
            }

            var dto = await response.Content.ReadFromJsonAsync<BrickExecuteResponseDto>(cancellationToken).ConfigureAwait(false);
            if (dto is null)
            {
                return Result<GenerationExecutionResult>.Failure(
                    "peer.null_response",
                    "Peer execution returned null response.");
            }

            if (!dto.Success)
            {
                return Result<GenerationExecutionResult>.Failure(
                    "peer.execution_failed",
                    dto.Error ?? "Peer execution failed.");
            }

            var output = BrickValueSerializer.FromWireToBrickOutput(dto.Output, dto.Summary);
            var bytes = output.Get<byte[]>("payload");
            var path = output.ToDictionary().TryGetValue("outputPath", out var outputPathValue)
                ? outputPathValue?.ToString() ?? string.Empty
                : string.Empty;
            var summary = dto.Summary ?? string.Empty;

            return Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
            {
                Payload = bytes,
                OutputPath = path ?? string.Empty,
                Provider = "nexo-peer",
                ModelId = payload.ModelId,
                IsRemote = true,
                Summary = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "peer-routing stage=failed endpoint={Endpoint}", endpoint);
            return Result<GenerationExecutionResult>.Failure(
                "peer.dispatch_exception",
                "Peer execution dispatch failed.",
                ex.Message);
        }
    }

    private bool IsEligible(PeerExecutionCandidate candidate, JobRequirements requirements)
    {
        if (string.IsNullOrWhiteSpace(candidate.Endpoint))
        {
            return false;
        }

        if (candidate.AvailableVramBytes > 0 && candidate.AvailableVramBytes < requirements.MinimumVramBytes)
        {
            return false;
        }

        if (candidate.ComputeClass < requirements.ComputeClass)
        {
            return false;
        }

        if (candidate.QueueDepth > _queueDepthThreshold)
        {
            return false;
        }

        return true;
    }
}
