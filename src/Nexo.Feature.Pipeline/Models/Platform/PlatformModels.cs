using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models;

/// <summary>
/// Platform feature.
/// </summary>
public partial class PlatformFeature
{
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public bool IsAvailable { get; set; }
    
    public string Version { get; set; } = string.Empty;
    
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Platform capability.
/// </summary>
public partial class PlatformCapability
{
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Type { get; set; } = string.Empty;
    
    public bool IsAvailable { get; set; }
    
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Native API.
/// </summary>
public partial class NativeAPI
{
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public bool IsAvailable { get; set; }
    
    public string Version { get; set; } = string.Empty;
    
    public List<string> Permissions { get; set; } = new List<string>();
}
