namespace Nexo.API.Security;

public static class MeshSecurityApplicationBuilderExtensions
{
    public static IApplicationBuilder UseNexoMeshSecurity(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MeshSecurityMiddleware>();
    }
}
