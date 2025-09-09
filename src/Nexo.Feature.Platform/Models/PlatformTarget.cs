namespace Nexo.Feature.Platform.Models;

/// <summary>
/// Represents the target platform for code generation and deployment.
/// </summary>
public enum PlatformTarget 
{
    /// <summary>
    /// C# language platform
    /// </summary>
    CSharp,
    
    /// <summary>
    /// .NET 8.0 platform
    /// </summary>
    DotNet8,
    
    /// <summary>
    /// .NET 6.0 platform
    /// </summary>
    DotNet6,
    
    /// <summary>
    /// .NET Framework 4.8
    /// </summary>
    DotNetFramework48,
    
    /// <summary>
    /// .NET Standard 2.0
    /// </summary>
    DotNetStandard20,
    
    /// <summary>
    /// Unity 2022 LTS
    /// </summary>
    Unity2022,
    
    /// <summary>
    /// Unity 2023 LTS
    /// </summary>
    Unity2023,
    
    /// <summary>
    /// Unity WebGL
    /// </summary>
    UnityWebGL,
    
    /// <summary>
    /// iOS platform (Swift)
    /// </summary>
    iOS,
    
    /// <summary>
    /// Android platform (Kotlin/Java)
    /// </summary>
    Android,
    
    /// <summary>
    /// Xamarin cross-platform
    /// </summary>
    Xamarin,
    
    /// <summary>
    /// .NET MAUI cross-platform
    /// </summary>
    MAUI,
    
    /// <summary>
    /// WebAssembly
    /// </summary>
    WebAssembly,
    
    /// <summary>
    /// JavaScript
    /// </summary>
    JavaScript,
    
    /// <summary>
    /// TypeScript
    /// </summary>
    TypeScript,
    
    /// <summary>
    /// React framework
    /// </summary>
    React,
    
    /// <summary>
    /// Angular framework
    /// </summary>
    Angular,
    
    /// <summary>
    /// Vue.js framework
    /// </summary>
    Vue,
    
    /// <summary>
    /// Windows Forms
    /// </summary>
    WinForms,
    
    /// <summary>
    /// Windows Presentation Foundation
    /// </summary>
    WPF,
    
    /// <summary>
    /// Windows UI
    /// </summary>
    WinUI,
    
    /// <summary>
    /// Avalonia cross-platform UI
    /// </summary>
    Avalonia,
    
    /// <summary>
    /// Electron.js
    /// </summary>
    ElectronJS,
    
    /// <summary>
    /// Flutter cross-platform
    /// </summary>
    Flutter,
    
    /// <summary>
    /// React Native
    /// </summary>
    ReactNative,
    
    /// <summary>
    /// Swift (native iOS)
    /// </summary>
    Swift,
    
    /// <summary>
    /// Kotlin (native Android)
    /// </summary>
    Kotlin,
    
    /// <summary>
    /// C++ (native)
    /// </summary>
    CPlusPlus,
    
    /// <summary>
    /// F# (functional programming)
    /// </summary>
    FSharp,
    
    /// <summary>
    /// Visual Basic .NET
    /// </summary>
    VBNet,
    
    /// <summary>
    /// Python
    /// </summary>
    Python,
    
    /// <summary>
    /// Go
    /// </summary>
    Go,
    
    /// <summary>
    /// Rust
    /// </summary>
    Rust
}

/// <summary>
/// Extension methods for PlatformTarget enum.
/// </summary>
public static class PlatformTargetExtensions 
{
    /// <summary>
    /// Gets the file extension for the target platform.
    /// </summary>
    public static string GetFileExtension(this PlatformTarget target) => target switch
    {
        PlatformTarget.DotNet8 or PlatformTarget.DotNet6 or PlatformTarget.DotNetFramework48 or PlatformTarget.DotNetStandard20 => ".cs",
        PlatformTarget.Unity2022 or PlatformTarget.Unity2023 or PlatformTarget.UnityWebGL => ".cs",
        PlatformTarget.JavaScript => ".js",
        PlatformTarget.TypeScript => ".ts",
        PlatformTarget.React => ".tsx",
        PlatformTarget.Angular => ".ts",
        PlatformTarget.Vue => ".vue",
        PlatformTarget.Swift or PlatformTarget.iOS => ".swift",
        PlatformTarget.Kotlin or PlatformTarget.Android => ".kt",
        PlatformTarget.CPlusPlus => ".cpp",
        PlatformTarget.FSharp => ".fs",
        PlatformTarget.VBNet => ".vb",
        PlatformTarget.Python => ".py",
        PlatformTarget.Go => ".go",
        PlatformTarget.Rust => ".rs",
        PlatformTarget.Flutter => ".dart",
        PlatformTarget.ReactNative => ".tsx",
        PlatformTarget.ElectronJS => ".js",
        PlatformTarget.MAUI or PlatformTarget.Xamarin => ".cs",
        _ => ".txt"
    };
    
    /// <summary>
    /// Determines if the target platform requires Unity support.
    /// </summary>
    public static bool RequiresUnitySupport(this PlatformTarget target) => target switch
    {
        PlatformTarget.Unity2022 or PlatformTarget.Unity2023 or PlatformTarget.UnityWebGL => true,
        _ => false
    };
    
    /// <summary>
    /// Determines if the target platform is a .NET platform.
    /// </summary>
    public static bool IsDotNetPlatform(this PlatformTarget target) => target switch
    {
        PlatformTarget.DotNet8 or PlatformTarget.DotNet6 or PlatformTarget.DotNetFramework48 or PlatformTarget.DotNetStandard20 => true,
        PlatformTarget.Unity2022 or PlatformTarget.Unity2023 or PlatformTarget.UnityWebGL => true,
        PlatformTarget.MAUI or PlatformTarget.Xamarin => true,
        PlatformTarget.WinForms or PlatformTarget.WPF or PlatformTarget.WinUI or PlatformTarget.Avalonia => true,
        _ => false
    };
    
    /// <summary>
    /// Determines if the target platform is a web platform.
    /// </summary>
    public static bool IsWebPlatform(this PlatformTarget target) => target switch
    {
        PlatformTarget.WebAssembly or PlatformTarget.JavaScript or PlatformTarget.TypeScript => true,
        PlatformTarget.React or PlatformTarget.Angular or PlatformTarget.Vue => true,
        PlatformTarget.ElectronJS => true,
        PlatformTarget.UnityWebGL => true,
        _ => false
    };
    
    /// <summary>
    /// Determines if the target platform is a mobile platform.
    /// </summary>
    public static bool IsMobilePlatform(this PlatformTarget target) => target switch
    {
        PlatformTarget.iOS or PlatformTarget.Android => true,
        PlatformTarget.Xamarin or PlatformTarget.MAUI => true,
        PlatformTarget.Flutter or PlatformTarget.ReactNative => true,
        _ => false
    };
    
    /// <summary>
    /// Gets the target framework string for .NET platforms.
    /// </summary>
    public static string? GetTargetFramework(this PlatformTarget target) => target switch
    {
        PlatformTarget.DotNet8 => "net8.0",
        PlatformTarget.DotNet6 => "net6.0",
        PlatformTarget.DotNetFramework48 => "net48",
        PlatformTarget.DotNetStandard20 => "netstandard2.0",
        PlatformTarget.MAUI => "net8.0",
        PlatformTarget.Xamarin => "net6.0",
        _ => null
    };
    
    /// <summary>
    /// Gets the platform-specific compiler directives.
    /// </summary>
    public static string[] GetCompilerDirectives(this PlatformTarget target) => target switch
    {
        PlatformTarget.Unity2022 or PlatformTarget.Unity2023 or PlatformTarget.UnityWebGL => new[] { "UNITY_2022_3_OR_NEWER", "UNITY" },
        PlatformTarget.iOS => new[] { "IOS", "APPLE" },
        PlatformTarget.Android => new[] { "ANDROID" },
        PlatformTarget.WebAssembly => new[] { "WASM", "BLAZOR" },
        PlatformTarget.MAUI => new[] { "MAUI", "MULTIPLATFORM" },
        PlatformTarget.Xamarin => new[] { "XAMARIN", "MULTIPLATFORM" },
        PlatformTarget.WinForms => new[] { "WINDOWS", "WINFORMS" },
        PlatformTarget.WPF => new[] { "WINDOWS", "WPF" },
        PlatformTarget.WinUI => new[] { "WINDOWS", "WINUI" },
        PlatformTarget.Avalonia => new[] { "AVALONIA", "CROSSPLATFORM" },
        _ => Array.Empty<string>()
    };
    
    /// <summary>
    /// Gets the platform-specific package manager.
    /// </summary>
    public static string GetPackageManager(this PlatformTarget target) => target switch
    {
        PlatformTarget.DotNet8 or PlatformTarget.DotNet6 or PlatformTarget.DotNetFramework48 or PlatformTarget.DotNetStandard20 => "NuGet",
        PlatformTarget.Unity2022 or PlatformTarget.Unity2023 or PlatformTarget.UnityWebGL => "Unity Package Manager",
        PlatformTarget.JavaScript or PlatformTarget.TypeScript or PlatformTarget.React or PlatformTarget.Angular or PlatformTarget.Vue => "npm",
        PlatformTarget.iOS or PlatformTarget.Swift => "Swift Package Manager",
        PlatformTarget.Android or PlatformTarget.Kotlin => "Gradle",
        PlatformTarget.Flutter => "pub",
        PlatformTarget.Python => "pip",
        PlatformTarget.Go => "go mod",
        PlatformTarget.Rust => "cargo",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Gets the platform-specific build tool.
    /// </summary>
    public static string GetBuildTool(this PlatformTarget target) => target switch
    {
        PlatformTarget.DotNet8 or PlatformTarget.DotNet6 or PlatformTarget.DotNetFramework48 or PlatformTarget.DotNetStandard20 => "dotnet",
        PlatformTarget.Unity2022 or PlatformTarget.Unity2023 or PlatformTarget.UnityWebGL => "Unity",
        PlatformTarget.JavaScript or PlatformTarget.TypeScript or PlatformTarget.React or PlatformTarget.Angular or PlatformTarget.Vue => "webpack/vite",
        PlatformTarget.iOS or PlatformTarget.Swift => "Xcode",
        PlatformTarget.Android or PlatformTarget.Kotlin => "Gradle",
        PlatformTarget.Flutter => "flutter",
        PlatformTarget.Python => "python",
        PlatformTarget.Go => "go",
        PlatformTarget.Rust => "cargo",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Gets the platform-specific runtime requirements.
    /// </summary>
    public static string[] GetRuntimeRequirements(this PlatformTarget target) => target switch
    {
        PlatformTarget.DotNet8 => new[] { ".NET 8.0 Runtime" },
        PlatformTarget.DotNet6 => new[] { ".NET 6.0 Runtime" },
        PlatformTarget.DotNetFramework48 => new[] { ".NET Framework 4.8" },
        PlatformTarget.Unity2022 or PlatformTarget.Unity2023 or PlatformTarget.UnityWebGL => new[] { "Unity Runtime" },
        PlatformTarget.JavaScript or PlatformTarget.TypeScript or PlatformTarget.React or PlatformTarget.Angular or PlatformTarget.Vue => new[] { "Node.js", "Web Browser" },
        PlatformTarget.iOS => new[] { "iOS Runtime" },
        PlatformTarget.Android => new[] { "Android Runtime" },
        PlatformTarget.Flutter => new[] { "Flutter Runtime" },
        PlatformTarget.Python => new[] { "Python Interpreter" },
        PlatformTarget.Go => new[] { "Go Runtime" },
        PlatformTarget.Rust => new[] { "Rust Runtime" },
        _ => Array.Empty<string>()
    };
}
