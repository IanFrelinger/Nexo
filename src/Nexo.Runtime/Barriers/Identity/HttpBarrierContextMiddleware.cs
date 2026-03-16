#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Http;

namespace Nexo.Runtime.Barriers.Identity;

public sealed class HttpBarrierContextMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // TODO: Populate BarrierResolutionContext from HttpContext and run
        // IBarrierIdentityResolverPipeline. Not implemented in this PR.
        return next(context);
    }
}
#endif
