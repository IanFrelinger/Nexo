using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Middleware.Ports;

namespace Nexo.Core.Application.Behaviors;

/// <summary>
/// Wraps MediatR handlers in a logging scope derived from <see cref="INexoIngressAccessor"/> when correlation is present.
/// </summary>
public sealed class IngressLoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly INexoIngressAccessor _ingress;
    private readonly ILogger<IngressLoggingPipelineBehavior<TRequest, TResponse>> _logger;

    public IngressLoggingPipelineBehavior(
        INexoIngressAccessor ingress,
        ILogger<IngressLoggingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _ingress = ingress;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(_ingress.CorrelationId))
            return await next().ConfigureAwait(false);

        var state = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Nexo.CorrelationId"] = _ingress.CorrelationId,
            ["Nexo.IngressTransport"] = _ingress.Transport,
            ["Nexo.TenantId"] = _ingress.TenantId,
            ["Nexo.AppId"] = _ingress.AppId,
            ["Nexo.IdempotencyKey"] = _ingress.IdempotencyKey,
            ["Nexo.PayloadVersion"] = _ingress.PayloadVersion,
            ["Nexo.MediatRRequest"] = typeof(TRequest).Name,
        };

        using (_logger.BeginScope(state))
            return await next().ConfigureAwait(false);
    }
}
