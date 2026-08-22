namespace Ashlar.API.Security;

/// <summary>
/// Phase 2: optional hardening for <c>/api/mesh</c> and remote brick execute paths.
/// Binds from configuration section <see cref="SectionPath"/>.
/// </summary>
public sealed class MeshSecurityOptions
{
    /// <summary>Configuration section path (<c>Ashlar:Security:Mesh</c>).</summary>
    public const string SectionPath = "Ashlar:Security:Mesh";

    /// <summary>
    /// When non-empty, mutating requests to <c>/api/mesh</c> (POST, PATCH, DELETE) must send the same value in <see cref="MeshTokenHeaderName"/>.
    /// Use a long random secret; workers and director share it in v1 unless you terminate TLS at a gateway that injects identity.
    /// </summary>
    public string? MeshMutatingToken { get; set; }

    /// <summary>
    /// Header name for <see cref="MeshMutatingToken"/> (default X-Ashlar-Mesh-Token).
    /// </summary>
    public string MeshTokenHeaderName { get; set; } = "X-Ashlar-Mesh-Token";

    /// <summary>
    /// When non-empty, <c>POST /api/bricks/.../execute</c> must include this value in <see cref="BrickExecuteTokenHeaderName"/>.
    /// When <see cref="BrickExecuteToken"/> is empty but <see cref="MeshMutatingToken"/> is set, the same secret is accepted in either
    /// <see cref="BrickExecuteTokenHeaderName"/> or <see cref="MeshTokenHeaderName"/> (single-secret operators).
    /// </summary>
    public string? BrickExecuteToken { get; set; }

    /// <summary>Header name for <see cref="BrickExecuteToken"/> (default X-Ashlar-Brick-Execute-Token).</summary>
    public string BrickExecuteTokenHeaderName { get; set; } = "X-Ashlar-Brick-Execute-Token";

    /// <summary>
    /// Maximum allowed JSON body size in bytes for mesh and brick-execute POST bodies (Content-Length check). 0 disables the check.
    /// </summary>
    public int MaxJsonBodyBytes { get; set; } = 524_288;

    /// <summary>
    /// Maximum mutating requests per client IP per window (mesh + brick execute). 0 disables rate limiting.
    /// </summary>
    public int RateLimitPermitLimit { get; set; } = 120;

    /// <summary>
    /// Window length in seconds for <see cref="RateLimitPermitLimit"/>.
    /// </summary>
    public int RateLimitWindowSeconds { get; set; } = 60;
}
