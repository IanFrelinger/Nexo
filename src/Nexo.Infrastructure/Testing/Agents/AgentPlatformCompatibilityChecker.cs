using System.Runtime.InteropServices;

namespace Nexo.Infrastructure.Testing.Agents;

/// <summary>
/// Checks platform compatibility for AI agents.
/// 
/// Validates that required dependencies and APIs are available
/// on the current platform.
/// </summary>
public static class AgentPlatformCompatibilityChecker
{
    /// <summary>
    /// Checks if AI agents are compatible with the current platform.
    /// </summary>
    public static AgentCompatibilityResult CheckCompatibility()
    {
        var platform = GetPlatformName();
        var isCompatible = true;
        var issues = new List<string>();

        // Check AutonomousDevAgent availability
        try
        {
            var autonomousDevAvailable = CheckAutonomousDevAgentAvailable();
            if (!autonomousDevAvailable)
            {
                isCompatible = false;
                issues.Add("AutonomousDevAgent is not available");
            }
        }
        catch (Exception ex)
        {
            isCompatible = false;
            issues.Add($"AutonomousDevAgent check failed: {ex.Message}");
        }

        // Check UniversalTesterAgent availability
        try
        {
            var universalTesterAvailable = CheckUniversalTesterAgentAvailable();
            if (!universalTesterAvailable)
            {
                isCompatible = false;
                issues.Add("UniversalTesterAgent is not available");
            }
        }
        catch (Exception ex)
        {
            isCompatible = false;
            issues.Add($"UniversalTesterAgent check failed: {ex.Message}");
        }

        // Check Microsoft.Extensions.Logging availability
        try
        {
            var loggingAvailable = CheckLoggingAvailable();
            if (!loggingAvailable)
            {
                isCompatible = false;
                issues.Add("Microsoft.Extensions.Logging is not available");
            }
        }
        catch (Exception ex)
        {
            isCompatible = false;
            issues.Add($"Logging check failed: {ex.Message}");
        }

        // Check optional dependencies
        try
        {
            var playwrightAvailable = CheckPlaywrightAvailable();
            if (!playwrightAvailable)
            {
                // Playwright is optional for UniversalTesterAgent (only needed for web testing)
                issues.Add("Microsoft.Playwright is not available (optional - only needed for web testing)");
            }
        }
        catch (Exception ex)
        {
            // Playwright is optional, so we don't mark as incompatible
            issues.Add($"Playwright check failed (optional): {ex.Message}");
        }

        return new AgentCompatibilityResult(platform, isCompatible, issues);
    }

    private static bool CheckAutonomousDevAgentAvailable()
    {
        try
        {
            // Try to use AutonomousDevAgent type
            var agentType = Type.GetType("Nexo.Agents.AutonomousDev.AutonomousDevAgent, Nexo.Agents.AutonomousDev");
            return agentType != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckUniversalTesterAgentAvailable()
    {
        try
        {
            // Try to use UniversalTesterAgent type
            var agentType = Type.GetType("Nexo.Agents.UniversalTester.UniversalTesterAgent, Nexo.Agents.UniversalTester");
            return agentType != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckLoggingAvailable()
    {
        try
        {
            // Check if Microsoft.Extensions.Logging is available
            var loggingType = Type.GetType("Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.Abstractions");
            return loggingType != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckPlaywrightAvailable()
    {
        try
        {
            // Check if Microsoft.Playwright is available (optional dependency)
            var playwrightType = Type.GetType("Microsoft.Playwright.IPage, Microsoft.Playwright");
            return playwrightType != null;
        }
        catch
        {
            return false;
        }
    }

    private static string GetPlatformName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";
        return "Unknown";
    }
}

/// <summary>
/// Result of agent platform compatibility check.
/// </summary>
public record AgentCompatibilityResult(
    string Platform,
    bool IsCompatible,
    List<string> Issues);
