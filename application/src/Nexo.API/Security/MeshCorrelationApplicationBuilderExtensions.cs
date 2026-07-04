namespace Nexo.API.Security;

/// <summary>Mesh correlation application builder extensions.</summary>
public static class MeshCorrelationApplicationBuilderExtensions
{
    /// <summary>Enables mesh correlation ID propagation for mesh and brick execute routes.</summary>
    public static IApplicationBuilder UseNexoMeshCorrelation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MeshCorrelationMiddleware>();
    }
}
