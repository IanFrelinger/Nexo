using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Models.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Services.Iteration;

/// <summary>
/// Detects runtime environment characteristics for strategy optimization
/// </summary>
public static class RuntimeEnvironmentDetector
{
    public static RuntimeEnvironmentProfile DetectCurrent()
    {
        return new RuntimeEnvironmentProfile
        {
            PlatformType = DetectPlatformType(),
            CpuCores = Environment.ProcessorCount,
            AvailableMemoryMB = GetAvailableMemoryMB(),
            IsDebugMode = IsDebugBuild(),
            FrameworkVersion = GetFrameworkVersion(),
            OptimizationLevel = DetectOptimizationLevel()
        };
    }
    
    private static PlatformCompatibility DetectPlatformType()
    {
        var platform = PlatformCompatibility.None;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            platform |= PlatformCompatibility.DotNet;
        }
        
        // Unity detection
        #if UNITY_2022_1_OR_NEWER || UNITY_2023_1_OR_NEWER
        platform |= PlatformCompatibility.Unity;
        #endif
        
        // WebAssembly detection
        if (RuntimeInformation.OSDescription.Contains("WebAssembly", StringComparison.OrdinalIgnoreCase))
        {
            platform |= PlatformCompatibility.WebAssembly;
        }
        
        // Mobile detection (heuristic based on environment variables)
        var mobileIndicators = new[]
        {
            "XAMARIN", "MAUI", "UNITY_MOBILE", "ANDROID", "IOS"
        };
        
        foreach (var indicator in mobileIndicators)
        {
            if (Environment.GetEnvironmentVariable(indicator) != null)
            {
                platform |= PlatformCompatibility.Mobile;
                break;
            }
        }
        
        // Server detection (heuristic)
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != null ||
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") != null ||
            Environment.GetEnvironmentVariable("SERVER_ENVIRONMENT") != null)
        {
            platform |= PlatformCompatibility.Server;
        }
        
        // Browser detection
        if (Environment.GetEnvironmentVariable("BROWSER") != null ||
            RuntimeInformation.OSDescription.Contains("Browser", StringComparison.OrdinalIgnoreCase))
        {
            platform |= PlatformCompatibility.Browser;
        }
        
        return platform != PlatformCompatibility.None ? platform : PlatformCompatibility.DotNet;
    }
    
    private static long GetAvailableMemoryMB()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.WorkingSet64 / (1024 * 1024);
        }
        catch
        {
            return 1024; // Default assumption
        }
    }
    
    private static bool IsDebugBuild()
    {
        #if DEBUG
        return true;
        #else
        return false;
        #endif
    }
    
    private static string GetFrameworkVersion()
    {
        return RuntimeInformation.FrameworkDescription;
    }
    
    private static OptimizationLevel DetectOptimizationLevel()
    {
        if (IsDebugBuild())
            return OptimizationLevel.Debug;
        
        return Environment.ProcessorCount > 4 ? 
            OptimizationLevel.Aggressive : 
            OptimizationLevel.Balanced;
    }
}
