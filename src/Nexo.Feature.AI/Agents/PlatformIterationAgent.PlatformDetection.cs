using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Agents.Specialized;

namespace Nexo.Feature.AI.Agents;

/// <summary>
/// Platform type detection and conversion functionality
/// </summary>
public partial class PlatformIterationAgent
{
    private PlatformType ExtractPlatformType(string response)
    {
        if (response.Contains("Unity"))
            return PlatformType.Unity;
        if (response.Contains("JavaScript") || response.Contains("Web"))
            return PlatformType.JavaScript;
        if (response.Contains("Swift") || response.Contains("iOS"))
            return PlatformType.iOS;
        if (response.Contains("Kotlin") || response.Contains("Android"))
            return PlatformType.Android;
        if (response.Contains("WebAssembly"))
            return PlatformType.WebAssembly;
        if (response.Contains("Mobile"))
            return PlatformType.Mobile;
        if (response.Contains("Server"))
            return PlatformType.Server;
        
        return PlatformType.DotNet;
    }
    
    private RuntimeEnvironmentProfile CreateEnvironmentProfileFromAnalysis(PlatformType platformType)
    {
        return platformType switch
        {
            PlatformType.Unity => new RuntimeEnvironmentProfile
            {
                PlatformType = PlatformType.Unity,
                CpuCores = 4,
                AvailableMemoryMB = 1024,
                IsConstrained = true,
                IsMobile = false,
                IsWeb = false,
                IsUnity = true
            },
            PlatformType.Mobile => new RuntimeEnvironmentProfile
            {
                PlatformType = PlatformType.Mobile,
                CpuCores = 2,
                AvailableMemoryMB = 512,
                IsConstrained = true,
                IsMobile = true,
                IsWeb = false,
                IsUnity = false
            },
            PlatformType.Server => new RuntimeEnvironmentProfile
            {
                PlatformType = PlatformType.Server,
                CpuCores = 8,
                AvailableMemoryMB = 8192,
                IsConstrained = false,
                IsMobile = false,
                IsWeb = false,
                IsUnity = false
            },
            _ => RuntimeEnvironmentDetector.DetectCurrent()
        };
    }
    
    private PlatformTarget GetPlatformTargetFromType(PlatformType platformType)
    {
        return platformType switch
        {
            PlatformType.Unity => PlatformTarget.Unity2023,
            PlatformType.JavaScript => PlatformTarget.JavaScript,
            PlatformType.iOS => PlatformTarget.Swift,
            PlatformType.Android => PlatformTarget.Kotlin,
            PlatformType.WebAssembly => PlatformTarget.WebAssembly,
            PlatformType.Server => PlatformTarget.Server,
            _ => PlatformTarget.DotNet
        };
    }
    
    private static PlatformTarget ConvertToPlatformTarget(Nexo.Core.Domain.Entities.Infrastructure.PlatformType platformType)
    {
        return platformType switch
        {
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.DotNet => PlatformTarget.DotNet,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Unity => PlatformTarget.Unity,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.WebAssembly => PlatformTarget.WebAssembly,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Mobile => PlatformTarget.Mobile,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Server => PlatformTarget.Server,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Browser => PlatformTarget.Browser,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Native => PlatformTarget.Native,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows => PlatformTarget.Windows,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Linux => PlatformTarget.Linux,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.macOS => PlatformTarget.macOS,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web => PlatformTarget.Web,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.JavaScript => PlatformTarget.JavaScript,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.iOS => PlatformTarget.Swift,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Android => PlatformTarget.Kotlin,
            _ => PlatformTarget.DotNet
        };
    }
}
