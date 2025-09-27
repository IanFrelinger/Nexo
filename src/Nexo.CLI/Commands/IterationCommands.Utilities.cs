using System;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.CLI.Commands;

/// <summary>
/// Utility methods for iteration commands
/// </summary>
public partial class IterationCommands
{
    private PlatformTarget ParsePlatform(string platform)
    {
        return platform.ToLower() switch
        {
            "auto" => PlatformTarget.DotNet,
            "dotnet" => PlatformTarget.DotNet,
            "unity" => PlatformTarget.Unity2023,
            "web" => PlatformTarget.JavaScript,
            "mobile" => PlatformTarget.Swift,
            "server" => PlatformTarget.Server,
            _ => PlatformTarget.DotNet
        };
    }

    private PlatformType ParsePlatformType(string platform)
    {
        return platform.ToLower() switch
        {
            "auto" => PlatformType.DotNet,
            "dotnet" => PlatformType.DotNet,
            "unity" => PlatformType.Unity,
            "web" => PlatformType.Web,
            "mobile" => PlatformType.Mobile,
            "server" => PlatformType.Server,
            _ => PlatformType.DotNet
        };
    }

    private PlatformTarget GetPlatformTargetFromProfile(RuntimeEnvironmentProfile profile)
    {
        return profile.PlatformType switch
        {
            PlatformType.Unity => PlatformTarget.Unity2023,
            PlatformType.Web => PlatformTarget.JavaScript,
            PlatformType.Mobile => PlatformTarget.Swift,
            PlatformType.Server => PlatformTarget.Server,
            _ => PlatformTarget.DotNet
        };
    }
}
