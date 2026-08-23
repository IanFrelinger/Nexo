using MediatR;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Middleware.Ports;

namespace Ashlar.Core.Application.Behaviors;

/// <summary>
/// Wraps MediatR handlers in a logging scope derived from <see cref="IAshlarIngressAccessor"/> when correlation is present.
/// </summary>
public sealed class IngressLoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAshlarIngressAccessor _ingress;
    private readonly ILogger<IngressLoggingPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>Creates a pipeline behavior that adds ingress correlation to log scopes.</summary>
    /// <param name="ingress">Accessor for current ingress context.</param>
    /// <param name="logger">Logger for the pipeline behavior.</param>
    public IngressLoggingPipelineBehavior(
        IAshlarIngressAccessor ingress,
        ILogger<IngressLoggingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _ingress = ingress;
        _logger = logger;
    }

    /// <summary>Wraps the next delegate in an ingress logging scope when correlation is present.</summary>
    /// <param name="request">MediatR request being handled.</param>
    /// <param name="next">Next delegate in the MediatR pipeline.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Handler response from the next delegate.</returns>
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
            ["Ashlar.CorrelationId"] = _ingress.CorrelationId,
            ["Ashlar.IngressTransport"] = _ingress.Transport,
            ["Ashlar.TenantId"] = _ingress.TenantId,
            ["Ashlar.AppId"] = _ingress.AppId,
            ["Ashlar.IdempotencyKey"] = _ingress.IdempotencyKey,
            ["Ashlar.PayloadVersion"] = _ingress.PayloadVersion,
            ["Ashlar.MediatRRequest"] = typeof(TRequest).Name,
        };

        using (_logger.BeginScope(state))
            return await next().ConfigureAwait(false);
    }
}
