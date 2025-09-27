namespace Nexo.Infrastructure.Services.Platform.Integrators;

/// <summary>
/// Utility functions for API wrapper generation
/// </summary>
public partial class ApiWrapperGenerator
{
    private string GetNamespaceForPlatform(string platform)
    {
        return platform.ToLower() switch
        {
            "windows" => "Nexo.Platform.Windows",
            "linux" => "Nexo.Platform.Linux",
            "macos" => "Nexo.Platform.macOS",
            "android" => "Nexo.Platform.Android",
            "ios" => "Nexo.Platform.iOS",
            _ => "Nexo.Platform.Common"
        };
    }
}
