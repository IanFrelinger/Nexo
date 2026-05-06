using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.GameDomain.Mapping;

namespace Nexo.API.Forge;

/// <summary>
/// Executes map adaptation stages with bounded HTTP fetches, SSRF validation, payload heuristics,
/// and optional vector intelligence.
/// </summary>
public sealed class MapPipelineRunner
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IVectorMapIntelligenceService _intelligence;
    private readonly IOptions<ForgeSessionOptions> _forgeOptions;
    private readonly ILogger<MapPipelineRunner> _log;

    public MapPipelineRunner(
        IHttpClientFactory httpFactory,
        IVectorMapIntelligenceService intelligence,
        IOptions<ForgeSessionOptions> forgeOptions,
        ILogger<MapPipelineRunner> log)
    {
        _httpFactory = httpFactory;
        _intelligence = intelligence;
        _forgeOptions = forgeOptions;
        _log = log;
    }

    public async Task<MapPipelineRunResult> RunAsync(
        MapAdaptationPlan plan,
        MapPipelineRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(request);

        if (request.TimeoutMs <= 0)
            return new MapPipelineRunResult(false, [], "TimeoutMs must be positive.");

        var opts = _forgeOptions.Value;
        var maxBytes = opts.MaxFetchResponseBytes > 0
            ? opts.MaxFetchResponseBytes
            : 2_097_152;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(TimeSpan.FromMilliseconds(request.TimeoutMs));

        var stages = new List<MapPipelineStageResult>();
        var client = _httpFactory.CreateClient("forge-map");

        foreach (var stage in plan.PipelineStages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (stage)
            {
                case "resolve_aesthetic":
                case "resolve_map_profile":
                case "emit_host_manifest":
                case "voxel_rasterize":
                case "mesh_displace":
                case "vector_tessellate":
                case "rasterize_overlay":
                    stages.Add(new MapPipelineStageResult(stage, "ok", "Host-side geometry deferred."));
                    break;

                case "fetch_terrain":
                    if (string.IsNullOrWhiteSpace(request.TerrainDataUrl))
                    {
                        stages.Add(new MapPipelineStageResult(stage, "skipped", "No TerrainDataUrl on request."));
                    }
                    else if (!ForgeMapFetchUrlValidator.TryValidate(request.TerrainDataUrl, opts, out var terrErr))
                    {
                        stages.Add(new MapPipelineStageResult(stage, "error", terrErr));
                    }
                    else
                    {
                        var r = await FetchBytesAsync(client, request.TerrainDataUrl!, maxBytes, linked.Token)
                            .ConfigureAwait(false);
                        stages.Add(new MapPipelineStageResult(stage, r.Success ? "ok" : "error",
                            r.Success ? $"Fetched {r.ByteLength} bytes" : r.Error));
                    }
                    break;

                case "fetch_vector":
                    if (string.IsNullOrWhiteSpace(request.VectorDataUrl))
                    {
                        stages.Add(new MapPipelineStageResult(stage, "skipped", "No VectorDataUrl on request."));
                    }
                    else if (!ForgeMapFetchUrlValidator.TryValidate(request.VectorDataUrl, opts, out var vecErr))
                    {
                        stages.Add(new MapPipelineStageResult(stage, "error", vecErr));
                    }
                    else
                    {
                        var r = await FetchBytesAsync(client, request.VectorDataUrl!, maxBytes, linked.Token)
                            .ConfigureAwait(false);
                        if (!r.Success)
                        {
                            stages.Add(new MapPipelineStageResult(stage, "error", r.Error));
                            break;
                        }

                        var insp = VectorMapPayloadInspector.Inspect(new ReadOnlyMemory<byte>(r.Body), r.ContentType);
                        var detail = $"Fetched {r.ByteLength} bytes; format={insp.FormatGuess}";
                        if (opts.EnableVectorIntelligence && r.ByteLength > 0)
                        {
                            try
                            {
                                var intel = await _intelligence.AnalyzeAsync(
                                    new ReadOnlyMemory<byte>(r.Body),
                                    r.ContentType,
                                    linked.Token).ConfigureAwait(false);
                                detail += $"; intelligence: {intel.Summary}";
                            }
                            catch (Exception ex)
                            {
                                _log.LogWarning(ex, "Vector map intelligence failed; continuing.");
                                detail += "; intelligence failed (see logs)";
                            }
                        }

                        stages.Add(new MapPipelineStageResult(stage, "ok", detail));
                    }
                    break;

                default:
                    stages.Add(new MapPipelineStageResult(stage, "skipped", "Unknown stage in reference runner."));
                    break;
            }
        }

        var success = stages.All(s => s.Status is "ok" or "skipped");
        return new MapPipelineRunResult(success, stages, success ? null : "One or more stages failed.");
    }

    private sealed record FetchOutcome(bool Success, string? ContentType, int ByteLength, string? Error, byte[] Body);

    private static async Task<FetchOutcome> FetchBytesAsync(
        HttpClient client,
        string url,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (resp.StatusCode != HttpStatusCode.OK)
                return new FetchOutcome(false, null, 0, $"HTTP {(int)resp.StatusCode}", Array.Empty<byte>());

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var ms = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                       .ConfigureAwait(false)) > 0)
            {
                if (ms.Length + read > maxBytes)
                    return new FetchOutcome(false, resp.Content.Headers.ContentType?.MediaType, (int)ms.Length,
                        $"Response exceeded MaxFetchResponseBytes ({maxBytes}).", ms.ToArray());

                ms.Write(buffer, 0, read);
            }

            var body = ms.ToArray();
            return new FetchOutcome(true, resp.Content.Headers.ContentType?.MediaType, body.Length, null, body);
        }
        catch (Exception ex)
        {
            return new FetchOutcome(false, null, 0, ex.Message, Array.Empty<byte>());
        }
    }
}
