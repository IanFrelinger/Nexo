namespace Director.Core.Protocol;

/// <summary>
/// Standard command types for Director RPC protocol
/// </summary>
public static class CommandTypes
{
    /// <summary>Run a Nexo CLI command</summary>
    public const string RunNexo = "RunNexo";
    
    /// <summary>Open a Unity scene</summary>
    public const string OpenScene = "OpenScene";
    
    /// <summary>Toggle Unity play mode</summary>
    public const string TogglePlay = "TogglePlay";
    
    /// <summary>Apply UI modifications to Unity Editor</summary>
    public const string ApplyUIMod = "ApplyUIMod";
    
    /// <summary>Get Unity project information</summary>
    public const string GetProjectInfo = "GetProjectInfo";
    
    /// <summary>List available Unity scenes</summary>
    public const string ListScenes = "ListScenes";
    
    /// <summary>Ping/health check</summary>
    public const string Ping = "Ping";
}
