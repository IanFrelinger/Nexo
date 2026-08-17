namespace Nexo.API.Security;

/// <summary>
/// Opt-in for the remote container-execution surface (<c>POST /api/execution/build</c> and
/// <c>POST /api/execution/run</c>). Those routes hand caller-chosen images, commands and host bind
/// mounts to the local Docker daemon, so they are dark by default and only meant for
/// <c>RemoteExecutionPlatform</c> callers (<c>NEXO_EXECUTION_REMOTE_URL</c>).
/// Binds from configuration section <see cref="SectionPath"/>.
/// </summary>
public sealed class NexoExecutionOptions
{
    /// <summary>Configuration section path (<c>Nexo:Execution</c>).</summary>
    public const string SectionPath = "Nexo:Execution";

    /// <summary>
    /// When true, the <c>/api/execution/*</c> routes are mapped. Requests are still refused (403) unless a
    /// built-in <see cref="NexoSecurityOptions.AuthorizationMode"/> other than <c>None</c> is active;
    /// the surface never runs unauthenticated.
    /// </summary>
    public bool ServeRemoteExecution { get; set; }

    /// <summary>
    /// Single host directory under which <c>VolumeMounts</c> host paths are accepted on
    /// <c>/api/execution/run</c>. When unset, any request carrying volume mounts is rejected (400).
    /// The remote caller only mounts a test-results directory, so point this at that tree, never at
    /// a filesystem root, a home directory or the Docker socket.
    /// </summary>
    public string? AllowedVolumeMountRoot { get; set; }
}
