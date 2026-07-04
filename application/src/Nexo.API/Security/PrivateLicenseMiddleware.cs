using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>
/// Blocks mutating API routes when the Private license is expired or invalid.
/// </summary>
public sealed class PrivateLicenseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly NexoPrivateLicenseOptions _options;
    private readonly IPrivateLicenseValidator _validator;

    /// <summary>Creates middleware that gates mutating routes on private license validity.</summary>
    public PrivateLicenseMiddleware(
        RequestDelegate next,
        IOptions<NexoPrivateLicenseOptions> optionsAccessor,
        IPrivateLicenseValidator validator)
    {
        _next = next;
        _options = optionsAccessor.Value;
        _validator = validator;
    }

    /// <summary>Blocks mutating routes when the private license is expired or invalid.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.EnforceLicense || !ShouldEvaluate(context.Request))
        {
            await _next(context);
            return;
        }

        var status = _validator.GetStatus();
        if (!status.BlocksMutatingRequests)
        {
            await _next(context);
            return;
        }

        if (_options.AllowReadOnlyWhenExpired &&
            !IsMutatingMethod(context.Request.Method) &&
            context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = status.State == PrivateLicenseState.Expired
            ? StatusCodes.Status402PaymentRequired
            : StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var title = status.State == PrivateLicenseState.Expired ? "License expired" : "License invalid";
        var detail = status.Detail ?? title;
        await context.Response.WriteAsync(
            "{\"title\":\"" + EscapeJson(title) + "\",\"detail\":\"" + EscapeJson(detail) + "\"}");
    }

    private static bool ShouldEvaluate(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            return false;

        if (request.Path.StartsWithSegments("/api/support/diagnostics", StringComparison.OrdinalIgnoreCase))
            return false;

        return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMutatingMethod(string method) =>
        HttpMethods.IsPost(method) ||
        HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) ||
        HttpMethods.IsDelete(method);

    private static string EscapeJson(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
