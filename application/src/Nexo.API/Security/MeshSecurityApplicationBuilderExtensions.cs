namespace Nexo.API.Security;

/// <summary>Mesh security application builder extensions.</summary>
public static class MeshSecurityApplicationBuilderExtensions
{
    /// <summary>Enables mesh and brick execute hardening (tokens, body size, rate limits).</summary>
    public static IApplicationBuilder UseNexoMeshSecurity(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MeshSecurityMiddleware>();
    }
}
