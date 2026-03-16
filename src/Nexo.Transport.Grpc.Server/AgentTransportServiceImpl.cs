using System.Text.Json;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Transport;

namespace Nexo.Transport.Grpc.Server;

/// <summary>
/// Thin gRPC server facade over <see cref="IAgentTransport"/>.
/// </summary>
public sealed class AgentTransportServiceImpl : AgentTransportService.AgentTransportServiceBase
{
    private const string BarrierHeader = "x-nexo-barrier";
    private const string BarrierSourceHeader = "x-nexo-barrier-source";
    private const string CorrelationHeader = "x-nexo-correlation-id";

    private readonly IAgentTransport _transport;
    private readonly ILogger<AgentTransportServiceImpl> _logger;
    private readonly BarrierHierarchy _barrierHierarchy;
    private readonly BarrierOptions _barrierOptions;
    private readonly IBarrierContextAccessor _barrierContextAccessor;
    private readonly IBarrierAuditLog _barrierAuditLog;

    public AgentTransportServiceImpl(
        IAgentTransport transport,
        ILogger<AgentTransportServiceImpl> logger,
        BarrierHierarchy barrierHierarchy,
        IOptions<BarrierOptions> barrierOptions,
        IBarrierContextAccessor barrierContextAccessor,
        IBarrierAuditLog barrierAuditLog)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _barrierHierarchy = barrierHierarchy ?? throw new ArgumentNullException(nameof(barrierHierarchy));
        _barrierOptions = barrierOptions?.Value ?? throw new ArgumentNullException(nameof(barrierOptions));
        _barrierContextAccessor = barrierContextAccessor ?? throw new ArgumentNullException(nameof(barrierContextAccessor));
        _barrierAuditLog = barrierAuditLog ?? throw new ArgumentNullException(nameof(barrierAuditLog));
    }

    public override async Task<InvokeResponse> Invoke(InvokeRequest request, ServerCallContext context)
    {
        _logger.LogDebug("gRPC Invoke request for agent {AgentName}", request.AgentName);
        var correlationId = GetHeader(context, CorrelationHeader) ?? request.CorrelationId;
        var spanId = string.IsNullOrWhiteSpace(request.SpanId) ? string.Empty : request.SpanId;

        var barrierLevel = GetHeader(context, BarrierHeader);
        var barrierSource = GetHeader(context, BarrierSourceHeader) ?? BarrierAuthoritySource.Header;
        if (string.IsNullOrWhiteSpace(barrierLevel))
        {
            if (_barrierOptions.RequireExplicitBarrier)
            {
                await _barrierAuditLog.RecordAsync(new BarrierAuditEvent(
                    BarrierAuditEventType.ValidationFailed,
                    string.Empty,
                    barrierSource,
                    request.AgentName,
                    correlationId ?? string.Empty,
                    spanId,
                    DateTimeOffset.UtcNow,
                    "Missing barrier header."));
                return InvalidBarrierResponse(request, correlationId, spanId, "Missing barrier header.");
            }

            barrierLevel = _barrierHierarchy.Floor.Name;
            barrierSource = BarrierAuthoritySource.Default;
        }

        if (!_barrierHierarchy.IsKnown(barrierLevel))
        {
            await _barrierAuditLog.RecordAsync(new BarrierAuditEvent(
                BarrierAuditEventType.ValidationFailed,
                barrierLevel,
                barrierSource,
                request.AgentName,
                correlationId ?? string.Empty,
                spanId,
                DateTimeOffset.UtcNow,
                "Unknown barrier level."));
            return InvalidBarrierResponse(request, correlationId, spanId, $"Unknown barrier level '{barrierLevel}'.");
        }

        var barrierContext = BarrierContext.Create(
            barrierLevel,
            barrierSource,
            request.AgentName,
            correlationId ?? string.Empty,
            _barrierHierarchy);
        _barrierContextAccessor.Initialize(barrierContext);
        await _barrierAuditLog.RecordAsync(new BarrierAuditEvent(
            BarrierAuditEventType.ContextInitialized,
            barrierContext.Level,
            barrierContext.AuthoritySource,
            barrierContext.IssuedTo,
            barrierContext.CorrelationId,
            spanId,
            DateTimeOffset.UtcNow));

        var payload = DeserializePayload(request.Payload);
        var invocation = new AgentInvocationRequest(
            AgentName: request.AgentName,
            CorrelationId: correlationId ?? request.CorrelationId,
            SpanId: string.IsNullOrWhiteSpace(request.SpanId) ? null : request.SpanId,
            Payload: payload,
            Options: new AgentInvocationOptions(
                Timeout: TimeSpan.FromMilliseconds(Math.Max(1, request.TimeoutMs)),
                MaxRetries: Math.Max(0, request.MaxRetries),
                TargetEndpoint: string.IsNullOrWhiteSpace(request.TargetEndpoint) ? null : request.TargetEndpoint));

        var result = await _transport.SendAsync(invocation, context.CancellationToken);
        var output = SerializeOutput(result.Output);
        var errorCode = !string.IsNullOrWhiteSpace(result.ErrorCode)
            ? result.ErrorCode
            : (result.Metadata != null && result.Metadata.TryGetValue("errorCode", out var code) ? code : string.Empty);

        return new InvokeResponse
        {
            Success = result.Success,
            CorrelationId = result.CorrelationId ?? correlationId ?? request.CorrelationId ?? string.Empty,
            SpanId = result.SpanId ?? request.SpanId ?? string.Empty,
            ErrorMessage = result.ErrorMessage ?? string.Empty,
            ErrorCode = errorCode ?? string.Empty,
            Output = { output }
        };
    }

    public override async Task<HealthResponse> CheckHealth(HealthRequest request, ServerCallContext context)
    {
        _logger.LogDebug("gRPC health check request");
        var health = await _transport.CheckHealthAsync(context.CancellationToken);
        return new HealthResponse
        {
            IsHealthy = health.IsHealthy,
            TransportType = health.TransportType ?? health.TransportName,
            DiagnosticMessage = health.DiagnosticMessage ?? health.Message ?? string.Empty
        };
    }

    private static Dictionary<string, object?> DeserializePayload(IDictionary<string, string> payload)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in payload)
        {
            result[key] = DeserializeJson(value);
        }
        return result;
    }

    private static Dictionary<string, string> SerializeOutput(object? output)
    {
        if (output is IReadOnlyDictionary<string, object?> roMap)
        {
            return roMap.ToDictionary(kvp => kvp.Key, kvp => JsonSerializer.Serialize(kvp.Value), StringComparer.OrdinalIgnoreCase);
        }

        if (output is IReadOnlyDictionary<string, object> map)
        {
            return map.ToDictionary(kvp => kvp.Key, kvp => JsonSerializer.Serialize(kvp.Value), StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["result"] = JsonSerializer.Serialize(output)
        };
    }

    private static object? DeserializeJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<object?>(json);
        }
        catch
        {
            return json;
        }
    }

    private static string? GetHeader(ServerCallContext context, string key)
    {
        return context.RequestHeaders
            .FirstOrDefault(header => string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static InvokeResponse InvalidBarrierResponse(
        InvokeRequest request,
        string? correlationId,
        string spanId,
        string message)
    {
        return new InvokeResponse
        {
            Success = false,
            CorrelationId = correlationId ?? request.CorrelationId ?? string.Empty,
            SpanId = spanId,
            ErrorMessage = message,
            ErrorCode = "BARRIER_VALIDATION_FAILED"
        };
    }
}
