using System;

namespace Nexo.NativeConsole;

/// <summary>
/// Data models for the native interface
/// </summary>
public partial class LovableNativeInterface
{
    // Data models are defined as separate classes below
}

public class AppType
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public string[] Technologies { get; set; } = Array.Empty<string>();
}

public class Feature
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class QuickExample
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Platform { get; set; } = "";
}
