#if NET8_0_OR_GREATER
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;

namespace Nexo.Runtime.Barriers.Identity;

/// <summary>
/// Populates <see cref="BarrierResolutionContext"/> from the incoming HTTP request
/// and runs <see cref="IBarrierIdentityResolverPipeline"/> to resolve the barrier level.
/// When a level is resolved, initializes the scoped <see cref="IBarrierContextAccessor"/>
/// and records an audit event. Gated by NEXO_BARRIER_MIDDLEWARE_ENABLED.
/// </summary>
public sealed class HttpBarrierContextMiddleware : IMiddleware
{
    private readonly IBarrierIdentityResolverPipeline _pipeline;
    private readonly IBarrierAuditLog _auditLog;
    private readonly BarrierHierarchy _hierarchy;
    private readonly ILogger<HttpBarrierContextMiddleware> _logger;

    public HttpBarrierContextMiddleware(
        IBarrierIdentityResolverPipeline pipeline,
        IBarrierAuditLog auditLog,
        BarrierHierarchy hierarchy,
        ILogger<HttpBarrierContextMiddleware> logger)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Resolves barrier context from the HTTP request and initializes scoped accessor state.</summary>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var ambient = context.RequestServices?.GetService(typeof(IBarrierContextAmbient)) as IBarrierContextAmbient;
        try
        {
            var enabled = Environment.GetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED");
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var correlationId = context.TraceIdentifier;
            var resolutionContext = BuildResolutionContext(context, correlationId);

            BarrierResolutionResult? result;
            try
            {
                result = await _pipeline.ResolveAsync(resolutionContext, context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Barrier identity resolution failed for {CorrelationId}", correlationId);
                await next(context);
                return;
            }

            if (result is not null)
            {
                var accessor = context.RequestServices?.GetService(typeof(IBarrierContextAccessor)) as IBarrierContextAccessor;
                if (accessor is not null)
                {
                    try
                    {
                        var barrierContext = BarrierContext.Create(
                            result.ResolvedLevel,
                            result.AuthoritySource,
                            issuedTo: string.Empty,
                            correlationId,
                            _hierarchy,
                            result.Detail);
                        accessor.Initialize(barrierContext);
                    }
                    catch (BarrierCeilingExceededException ceilingEx)
                    {
                        _logger.LogWarning("Barrier ceiling exceeded: {Message}", ceilingEx.Message);
                        await _auditLog.RecordAsync(new BarrierAuditEvent(
                            BarrierAuditEventType.CeilingExceeded,
                            result.ResolvedLevel,
                            result.AuthoritySource,
                            AgentName: string.Empty,
                            correlationId,
                            SpanId: string.Empty,
                            DateTimeOffset.UtcNow,
                            ceilingEx.Message), context.RequestAborted);
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
                }

                await _auditLog.RecordAsync(new BarrierAuditEvent(
                    BarrierAuditEventType.ContextInitialized,
                    result.ResolvedLevel,
                    result.AuthoritySource,
                    AgentName: string.Empty,
                    correlationId,
                    SpanId: string.Empty,
                    DateTimeOffset.UtcNow,
                    result.Detail), context.RequestAborted);
            }

            await next(context);
        }
        finally
        {
            ambient?.SetCurrent(null);
        }
    }

    private static BarrierResolutionContext BuildResolutionContext(HttpContext context, string correlationId)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in context.Request.Headers)
        {
            headers[header.Key] = header.Value.ToString();
        }

        headers.TryGetValue("x-nexo-barrier", out var explicitLevel);
        headers.TryGetValue("x-nexo-api-key", out var apiKey);

        var certSubjects = new List<string>();
        var certSans = new List<string>();
        var clientCert = context.Connection.ClientCertificate;
        if (clientCert is not null)
        {
            certSubjects.Add(clientCert.Subject);
            ClientCertificateSanExtractor.ExtractDnsSans(clientCert, certSans);
        }

        string? rawJwt = null;
        var jwtClaims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (headers.TryGetValue("Authorization", out var authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            rawJwt = authHeader.Substring("Bearer ".Length).Trim();
        }

        if (context.User?.Identity?.IsAuthenticated == true && context.User.Claims.Any())
        {
            foreach (var claim in context.User.Claims)
            {
                jwtClaims.TryAdd(claim.Type, claim.Value);
            }
        }

        return new BarrierResolutionContext(
            CorrelationId: correlationId,
            ExplicitLevel: explicitLevel,
            Headers: headers,
            CertSubjects: certSubjects,
            CertSans: certSans,
            RawJwt: rawJwt,
            JwtClaims: jwtClaims,
            ApiKey: apiKey);
    }
}
#endif
