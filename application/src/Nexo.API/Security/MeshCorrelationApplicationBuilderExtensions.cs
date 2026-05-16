namespace Nexo.API.Security;

public static class MeshCorrelationApplicationBuilderExtensions
{
    public static IApplicationBuilder UseNexoMeshCorrelation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MeshCorrelationMiddleware>();
    }
}
