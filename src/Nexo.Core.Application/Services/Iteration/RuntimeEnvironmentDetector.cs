using System;
using System.Runtime.InteropServices;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Iteration;

/// <summary>
/// Detects the current runtime environment for iteration strategy selection
/// </summary>
public static class RuntimeEnvironmentDetector
{
    /// <summary>
    /// Detect the current runtime environment
    /// </summary>
    /// <returns>Runtime environment profile</returns>
    public static RuntimeEnvironmentProfile DetectCurrent()
    {
        var platformType = DetectPlatformType();
        var cpuCores = System.Environment.ProcessorCount;
        var availableMemory = GetAvailableMemoryMB();
        var isConstrained = IsConstrainedEnvironment();
        var isMobile = IsMobileEnvironment();
        var isWeb = IsWebEnvironment();
        var isUnity = IsUnityEnvironment();
        var frameworkVersion = GetFrameworkVersion();
        var optimizationLevel = GetOptimizationLevel();
        var isDebugMode = IsDebugMode();
        
        return new RuntimeEnvironmentProfile
        {
            PlatformType = platformType,
            CpuCores = cpuCores,
            AvailableMemoryMB = availableMemory,
            IsConstrained = isConstrained,
            IsMobile = isMobile,
            IsWeb = isWeb,
            IsUnity = isUnity,
            FrameworkVersion = frameworkVersion,
            OptimizationLevel = optimizationLevel.ToString(),
            IsDebugMode = isDebugMode
        };
    }
    
    private static PlatformType DetectPlatformType()
    {
        // Check for Unity
        if (IsUnityEnvironment())
        {
            return PlatformType.Unity;
        }
        
        // Check for WebAssembly
        if (IsWebAssemblyEnvironment())
        {
            return PlatformType.WebAssembly;
        }
        
        // Check for mobile platforms
        if (IsMobileEnvironment())
        {
            return PlatformType.Mobile;
        }
        
        // Check for web environment
        if (IsWebEnvironment())
        {
            return PlatformType.Web;
        }
        
        // Check for JavaScript
        if (IsJavaScriptEnvironment())
        {
            return PlatformType.JavaScript;
        }
        
        // Check for native platforms
        if (IsNativeEnvironment())
        {
            return PlatformType.Native;
        }
        
        // Default to .NET
        return PlatformType.DotNet;
    }
    
    private static bool IsUnityEnvironment()
    {
        try
        {
            // Check for Unity-specific assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.FullName?.Contains("UnityEngine") == true ||
                    assembly.FullName?.Contains("UnityEditor") == true)
                {
                    return true;
                }
            }
            
            // Check for Unity-specific types
            return Type.GetType("UnityEngine.Application") != null ||
                   Type.GetType("UnityEngine.GameObject") != null;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool IsWebAssemblyEnvironment()
    {
        try
        {
            // Check for WebAssembly-specific runtime
            return RuntimeInformation.OSDescription.Contains("Browser") ||
                   System.Environment.GetEnvironmentVariable("DOTNET_RUNTIME_IDENTIFIER")?.Contains("browser") == true ||
                   Type.GetType("System.Runtime.InteropServices.JavaScript.JSImportAttribute") != null;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool IsMobileEnvironment()
    {
        try
        {
            // Check for mobile-specific environment variables or assemblies
            var runtimeId = System.Environment.GetEnvironmentVariable("DOTNET_RUNTIME_IDENTIFIER");
            return runtimeId?.Contains("android") == true ||
                   runtimeId?.Contains("ios") == true ||
                   runtimeId?.Contains("mobile") == true;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool IsWebEnvironment()
    {
        try
        {
            // Check for web-specific environment
            return System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != null ||
                   System.Environment.GetEnvironmentVariable("WEB_ENVIRONMENT") != null ||
                   Type.GetType("Microsoft.AspNetCore.Http.HttpContext") != null;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool IsJavaScriptEnvironment()
    {
        try
        {
            // Check for JavaScript-specific runtime
            return Type.GetType("System.Runtime.InteropServices.JavaScript.JSImportAttribute") != null ||
                   System.Environment.GetEnvironmentVariable("JAVASCRIPT_ENVIRONMENT") != null;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool IsNativeEnvironment()
    {
        try
        {
            // Check for native-specific runtime
            var runtimeId = System.Environment.GetEnvironmentVariable("DOTNET_RUNTIME_IDENTIFIER");
            return runtimeId?.Contains("native") == true ||
                   runtimeId?.Contains("linux") == true ||
                   runtimeId?.Contains("win") == true ||
                   runtimeId?.Contains("osx") == true;
        }
        catch
        {
            return false;
        }
    }
    
    private static long GetAvailableMemoryMB()
    {
        try
        {
            // Try to get available memory
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows-specific memory detection
                return GC.GetTotalMemory(false) / 1024 / 1024;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Unix-based systems
                return GC.GetTotalMemory(false) / 1024 / 1024;
            }
            else
            {
                // Fallback
                return GC.GetTotalMemory(false) / 1024 / 1024;
            }
        }
        catch
        {
            // Fallback to a reasonable default
            return 1024; // 1GB
        }
    }
    
    private static bool IsConstrainedEnvironment()
    {
        try
        {
            // Check for constrained environments
            var availableMemory = GetAvailableMemoryMB();
            var cpuCores = System.Environment.ProcessorCount;
            
            // Consider constrained if low memory or low CPU cores
            return availableMemory < 512 || cpuCores < 2;
        }
        catch
        {
            return false;
        }
    }
    
    private static string GetFrameworkVersion()
    {
        try
        {
            // Get .NET version
            var version = System.Environment.Version;
            return $".NET {version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            return ".NET 8.0";
        }
    }
    
    private static OptimizationLevel GetOptimizationLevel()
    {
        try
        {
            #if DEBUG
            return OptimizationLevel.Debug;
            #else
            // In release mode, determine based on CPU cores
            var cpuCores = System.Environment.ProcessorCount;
            if (cpuCores >= 8)
                return OptimizationLevel.Aggressive;
            else if (cpuCores >= 4)
                return OptimizationLevel.Balanced;
            else
                return OptimizationLevel.Basic;
            #endif
        }
        catch
        {
            return OptimizationLevel.Balanced;
        }
    }
    
    private static bool IsDebugMode()
    {
        try
        {
            #if DEBUG
            return true;
            #else
            return false;
            #endif
        }
        catch
        {
            return false;
        }
    }
}