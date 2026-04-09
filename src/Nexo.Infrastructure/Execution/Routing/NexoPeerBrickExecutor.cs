using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
    private readonly TimeSpan _peerRequestTimeout;

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
        _peerRequestTimeout = config?.Value?.PeerRequestTimeout > TimeSpan.Zero
            ? config.Value.PeerRequestTimeout
            : TimeSpan.FromSeconds(30);
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
        var candidates = GetEligibleCandidates(requirements);
        if (candidates.Count == 0)
        {
            return Result<GenerationExecutionResult>.Failure(
                "peer-routing.no_eligible_peers",
                "No eligible peer endpoint is currently available.");
        }

        var requestJson = CreateRequestJson(payload, requirements, context);

        var failures = new List<string>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var result = await ExecuteAgainstPeerAsync(candidate, payload, requestJson, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return result;
            }

            var code = result.Error?.Code ?? "peer.unknown_error";
            var detail = result.Error?.Detail ?? result.Error?.Message ?? "No detail";
            failures.Add($"{candidate.PeerId}:{code}:{detail}");
            _logger.LogWarning(
                "peer-routing stage=fallback peer={PeerId} endpoint={Endpoint} code={Code} detail={Detail}",
                candidate.PeerId,
                candidate.Endpoint,
                code,
                detail);
        }

        return Result<GenerationExecutionResult>.Failure(
            "peer-routing.all_peers_failed",
            $"Peer execution failed across {candidates.Count} eligible peers.",
            string.Join(" | ", failures));
    }

    private async Task<Result<GenerationExecutionResult>> ExecuteAgainstPeerAsync(
        PeerExecutionCandidate candidate,
        RunPodJobPayload payload,
        string requestJson,
        CancellationToken cancellationToken)
    {
        if (!TryCreateEndpointUri(candidate.Endpoint, out var endpointUri) || endpointUri is null)
        {
            return Result<GenerationExecutionResult>.Failure(
                "peer.invalid_endpoint",
                "Peer endpoint is not a valid HTTP URI.",
                candidate.Endpoint);
        }

        var client = _httpClientFactory.CreateClient();
        var requestUri = new Uri(endpointUri, $"api/bricks/{Uri.EscapeDataString(_peerBrickId)}/execute");
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_peerRequestTimeout);
        try
        {
            _logger.LogInformation(
                "peer-routing stage=dispatch peer={PeerId} endpoint={Endpoint} model={ModelId}",
                candidate.PeerId,
                endpointUri,
                payload.ModelId);
            using var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUri, requestContent, timeoutCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result<GenerationExecutionResult>.Failure(
                    "peer.http_error",
                    $"Peer execution call failed with status {(int)response.StatusCode}.",
                    $"{candidate.PeerId}:{response.ReasonPhrase}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            if (!TryParsePeerExecutionResponse(responseJson, out var parsed, out var parseError))
            {
                return Result<GenerationExecutionResult>.Failure(
                    parseError?.Code ?? "peer.invalid_response",
                    parseError?.Message ?? "Peer execution returned an invalid response.",
                    candidate.PeerId);
            }

            if (!parsed.IsSuccess)
            {
                return Result<GenerationExecutionResult>.Failure(
                    "peer.execution_failed",
                    parsed.Error ?? "Peer execution failed.",
                    candidate.PeerId);
            }

            if (parsed.Payload is null || parsed.Payload.Length == 0)
            {
                return Result<GenerationExecutionResult>.Failure(
                    "peer.invalid_output",
                    "Peer execution response did not include payload bytes.",
                    candidate.PeerId);
            }

            return Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
            {
                Payload = parsed.Payload,
                OutputPath = parsed.OutputPath,
                Provider = "nexo-peer",
                ModelId = payload.ModelId,
                IsRemote = true,
                Summary = parsed.Summary
            });
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "peer-routing stage=timeout peer={PeerId} endpoint={Endpoint}",
                candidate.PeerId,
                endpointUri);
            return Result<GenerationExecutionResult>.Failure(
                "peer.timeout",
                $"Peer execution timed out after {_peerRequestTimeout.TotalSeconds:0.#} seconds.",
                candidate.PeerId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "peer-routing stage=http-failed peer={PeerId} endpoint={Endpoint}",
                candidate.PeerId,
                endpointUri);
            return Result<GenerationExecutionResult>.Failure(
                "peer.http_request_failed",
                "Peer HTTP request failed.",
                $"{candidate.PeerId}:{ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "peer-routing stage=failed peer={PeerId} endpoint={Endpoint}",
                candidate.PeerId,
                endpointUri);
            return Result<GenerationExecutionResult>.Failure(
                "peer.dispatch_exception",
                "Peer execution dispatch failed.",
                $"{candidate.PeerId}:{ex.Message}");
        }
    }

    private IReadOnlyList<PeerExecutionCandidate> GetEligibleCandidates(JobRequirements requirements)
    {
        return _snapshot.Candidates
            .Where(candidate => IsEligible(candidate, requirements))
            .OrderBy(candidate => candidate.QueueDepth)
            .ThenByDescending(candidate => candidate.ComputeClass)
            .ThenByDescending(candidate => candidate.AvailableVramBytes)
            .ThenByDescending(candidate => candidate.CapturedAt)
            .GroupBy(candidate => NormalizeEndpoint(candidate.Endpoint), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private bool IsEligible(PeerExecutionCandidate candidate, JobRequirements requirements)
    {
        if (!TryCreateEndpointUri(candidate.Endpoint, out _))
        {
            return false;
        }

        if (candidate.AvailableVramBytes > 0 && candidate.AvailableVramBytes < requirements.MinimumVramBytes)
        {
            return false;
        }

        if (candidate.ComputeClass != GpuComputeClass.None && candidate.ComputeClass < requirements.ComputeClass)
        {
            return false;
        }

        if (candidate.QueueDepth > _queueDepthThreshold)
        {
            return false;
        }

        return true;
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (!TryCreateEndpointUri(endpoint, out var endpointUri) || endpointUri is null)
        {
            return string.Empty;
        }

        var authority = endpointUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return authority;
    }

    private static bool TryCreateEndpointUri(string endpoint, out Uri? endpointUri)
    {
        endpointUri = null;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return false;
        }

        var trimmed = endpoint.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        endpointUri = new Uri(uri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/", UriKind.Absolute);
        return true;
    }

    private static bool TryParsePeerExecutionResponse(
        string responseJson,
        out PeerExecutionParseResult result,
        out Error? error)
    {
        result = default;
        error = null;

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            error = new Error
            {
                Code = "peer.null_response",
                Message = "Peer execution returned an empty response."
            };
            return false;
        }

        if (!TryReadBooleanProperty(responseJson, "success", out var success))
        {
            error = new Error
            {
                Code = "peer.invalid_response",
                Message = "Peer execution response did not include a success flag."
            };
            return false;
        }

        var summary = TryReadStringProperty(responseJson, "summary") ?? string.Empty;
        var remoteError = TryReadStringProperty(responseJson, "error") ?? string.Empty;
        if (!success)
        {
            result = new PeerExecutionParseResult(
                false,
                Array.Empty<byte>(),
                string.Empty,
                summary,
                string.IsNullOrWhiteSpace(remoteError) ? "Peer execution failed." : remoteError);
            return true;
        }

        if (!TryReadPayloadBytes(responseJson, out var payload))
        {
            error = new Error
            {
                Code = "peer.invalid_output",
                Message = "Peer execution response did not include a valid payload."
            };
            return false;
        }

        var outputPath = TryReadStringProperty(responseJson, "outputPath") ?? string.Empty;
        result = new PeerExecutionParseResult(true, payload, outputPath, summary, string.Empty);
        return true;
    }

    private static bool TryReadPayloadBytes(string json, out byte[] payload)
    {
        payload = Array.Empty<byte>();

        var wrapperMatch = Regex.Match(
            json,
            "\"payload\"\\s*:\\s*\\{[^\\}]*\"base64\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (wrapperMatch.Success)
        {
            return TryDecodeBase64Value(wrapperMatch.Groups["value"].Value, out payload);
        }

        var stringMatch = Regex.Match(
            json,
            "\"payload\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (stringMatch.Success)
        {
            return TryDecodeBase64Value(stringMatch.Groups["value"].Value, out payload);
        }

        return false;
    }

    private static bool TryDecodeBase64Value(string escapedValue, out byte[] payload)
    {
        payload = Array.Empty<byte>();
        var base64 = UnescapeJsonString(escapedValue);
        if (string.IsNullOrWhiteSpace(base64))
        {
            return false;
        }

        try
        {
            payload = Convert.FromBase64String(base64);
            return payload.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private readonly record struct PeerExecutionParseResult(
        bool IsSuccess,
        byte[] Payload,
        string OutputPath,
        string Summary,
        string Error);

    private string CreateRequestJson(
        RunPodJobPayload payload,
        JobRequirements requirements,
        IExecutionContext context)
    {
        var modelId = string.IsNullOrWhiteSpace(payload.ModelId) ? requirements.ModelId : payload.ModelId;
        var estimatedSeconds = Math.Max(1, (int)Math.Ceiling(requirements.EstimatedDuration.TotalSeconds));
        var builder = new StringBuilder(512);
        builder.Append('{');
        AppendStringProperty(builder, "wireFormatVersion", WireFormatVersion.Current, true);
        AppendStringProperty(builder, "brickId", _peerBrickId, true);
        AppendStringProperty(builder, "implementation", "Deterministic", true);
        builder.Append("\"input\":{");
        AppendStringProperty(builder, "modelId", modelId, true);
        AppendStringProperty(builder, "prompt", payload.Prompt ?? string.Empty, true);
        builder.Append("\"parameters\":");
        AppendJsonValue(builder, payload.GenerationParameters);
        builder.Append(',');
        AppendStringProperty(builder, "outputFormat", payload.OutputFormat ?? "text/plain", true);
        AppendBooleanProperty(builder, "priority", payload.IsPriority, true);
        AppendNumberProperty(builder, "minimumVramBytes", requirements.MinimumVramBytes, true);
        AppendStringProperty(builder, "computeClass", requirements.ComputeClass.ToString(), true);
        AppendNumberProperty(builder, "estimatedDurationSeconds", estimatedSeconds, true);
        AppendBooleanProperty(builder, "overnight", requirements.IsOvernightOrBackground, false);
        builder.Append("},");
        builder.Append("\"executionContext\":{");
        AppendStringProperty(builder, "agentId", context.AgentId ?? string.Empty, true);
        AppendStringProperty(builder, "behaviorId", context.BehaviorId ?? string.Empty, true);
        AppendBooleanProperty(builder, "auditMode", context.AuditMode, true);
        AppendBooleanProperty(builder, "isAirGapped", context.IsAirGapped, true);
        AppendStringProperty(builder, "provider", context.Provider ?? string.Empty, true);
        builder.Append("\"variables\":");
        AppendJsonValue(builder, context.Variables);
        builder.Append('}');
        builder.Append('}');
        return builder.ToString();
    }

    private static void AppendStringProperty(StringBuilder builder, string propertyName, string value, bool trailingComma)
    {
        builder.Append('"').Append(EscapeJsonString(propertyName)).Append('"').Append(':');
        builder.Append('"').Append(EscapeJsonString(value)).Append('"');
        if (trailingComma)
        {
            builder.Append(',');
        }
    }

    private static void AppendBooleanProperty(StringBuilder builder, string propertyName, bool value, bool trailingComma)
    {
        builder.Append('"').Append(EscapeJsonString(propertyName)).Append('"').Append(':');
        builder.Append(value ? "true" : "false");
        if (trailingComma)
        {
            builder.Append(',');
        }
    }

    private static void AppendNumberProperty(StringBuilder builder, string propertyName, long value, bool trailingComma)
    {
        builder.Append('"').Append(EscapeJsonString(propertyName)).Append('"').Append(':');
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
        if (trailingComma)
        {
            builder.Append(',');
        }
    }

    private static void AppendJsonValue(StringBuilder builder, object? value)
    {
        if (value is null)
        {
            builder.Append("null");
            return;
        }

        switch (value)
        {
            case string text:
                builder.Append('"').Append(EscapeJsonString(text)).Append('"');
                return;
            case bool b:
                builder.Append(b ? "true" : "false");
                return;
            case byte by:
                builder.Append(by.ToString(CultureInfo.InvariantCulture));
                return;
            case sbyte sb:
                builder.Append(sb.ToString(CultureInfo.InvariantCulture));
                return;
            case short s16:
                builder.Append(s16.ToString(CultureInfo.InvariantCulture));
                return;
            case ushort us16:
                builder.Append(us16.ToString(CultureInfo.InvariantCulture));
                return;
            case int i32:
                builder.Append(i32.ToString(CultureInfo.InvariantCulture));
                return;
            case uint ui32:
                builder.Append(ui32.ToString(CultureInfo.InvariantCulture));
                return;
            case long i64:
                builder.Append(i64.ToString(CultureInfo.InvariantCulture));
                return;
            case ulong ui64:
                builder.Append(ui64.ToString(CultureInfo.InvariantCulture));
                return;
            case float f:
                builder.Append(f.ToString(CultureInfo.InvariantCulture));
                return;
            case double d:
                builder.Append(d.ToString(CultureInfo.InvariantCulture));
                return;
            case decimal dec:
                builder.Append(dec.ToString(CultureInfo.InvariantCulture));
                return;
            case DateTimeOffset dto:
                builder.Append('"').Append(dto.ToString("O", CultureInfo.InvariantCulture)).Append('"');
                return;
            case DateTime dt:
                builder.Append('"').Append(dt.ToString("O", CultureInfo.InvariantCulture)).Append('"');
                return;
            case Guid guid:
                builder.Append('"').Append(guid.ToString()).Append('"');
                return;
            case Enum enumValue:
                builder.Append('"').Append(EscapeJsonString(enumValue.ToString())).Append('"');
                return;
            case byte[] bytes:
                builder.Append("{\"__type\":\"bytes\",\"base64\":\"")
                    .Append(Convert.ToBase64String(bytes))
                    .Append("\"}");
                return;
            case IReadOnlyDictionary<string, object> readOnlyDict:
                AppendDictionary(builder, readOnlyDict.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)));
                return;
            case IDictionary<string, object> dict:
                AppendDictionary(builder, dict.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)));
                return;
            case System.Collections.IDictionary rawDict:
                AppendDictionary(builder, EnumerateDictionary(rawDict));
                return;
            case System.Collections.IEnumerable sequence:
                builder.Append('[');
                var hasItem = false;
                foreach (var item in sequence)
                {
                    if (hasItem)
                    {
                        builder.Append(',');
                    }
                    AppendJsonValue(builder, item);
                    hasItem = true;
                }
                builder.Append(']');
                return;
            default:
                builder.Append('"')
                    .Append(EscapeJsonString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty))
                    .Append('"');
                return;
        }
    }

    private static void AppendDictionary(StringBuilder builder, IEnumerable<KeyValuePair<string, object?>> entries)
    {
        builder.Append('{');
        var first = true;
        foreach (var entry in entries)
        {
            if (!first)
            {
                builder.Append(',');
            }
            builder.Append('"').Append(EscapeJsonString(entry.Key)).Append('"').Append(':');
            AppendJsonValue(builder, entry.Value);
            first = false;
        }
        builder.Append('}');
    }

    private static IEnumerable<KeyValuePair<string, object?>> EnumerateDictionary(System.Collections.IDictionary dictionary)
    {
        foreach (System.Collections.DictionaryEntry entry in dictionary)
        {
            var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty;
            yield return new KeyValuePair<string, object?>(key, entry.Value);
        }
    }

    private static bool TryReadBooleanProperty(string json, string propertyName, out bool value)
    {
        value = false;
        var pattern = $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*(?<value>true|false)";
        var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return false;
        }

        var text = match.Groups["value"].Value;
        value = string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private static string? TryReadStringProperty(string json, string propertyName)
    {
        var pattern = $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"";
        var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return null;
        }

        return UnescapeJsonString(match.Groups["value"].Value);
    }

    private static string UnescapeJsonString(string escaped)
    {
        if (string.IsNullOrEmpty(escaped))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(escaped.Length);
        for (var i = 0; i < escaped.Length; i++)
        {
            var c = escaped[i];
            if (c != '\\')
            {
                builder.Append(c);
                continue;
            }

            if (i + 1 >= escaped.Length)
            {
                break;
            }

            var next = escaped[++i];
            switch (next)
            {
                case '"':
                    builder.Append('"');
                    break;
                case '\\':
                    builder.Append('\\');
                    break;
                case '/':
                    builder.Append('/');
                    break;
                case 'b':
                    builder.Append('\b');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'u':
                    if (i + 4 < escaped.Length)
                    {
                        var hex = escaped.Substring(i + 1, 4);
                        if (ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                        {
                            builder.Append((char)code);
                            i += 4;
                        }
                    }
                    break;
                default:
                    builder.Append(next);
                    break;
            }
        }

        return builder.ToString();
    }

    private static string EscapeJsonString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length + 16);
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (c < 32)
                    {
                        builder.Append("\\u");
                        builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }

        return builder.ToString();
    }
}
