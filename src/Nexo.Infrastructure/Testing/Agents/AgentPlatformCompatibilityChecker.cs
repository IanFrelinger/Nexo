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
                issues.Add("Microsoft.Playwright is not available (optional)");
            }
        }
        catch (Exception ex)
        {
            // Playwright is optional, so we don't mark as incompatible
            issues.Add($"Playwright check failed (optional): {ex.Message}");
        }

        return new AgentCompatibilityResult(platform, isCompatible, issues);
    }

    private static bool CheckLoggingAvailable()
    {
        var loggingType = CompatibilityTestHooks.ResolveType(
            "Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.Abstractions");
        return loggingType != null;
    }

    private static bool CheckPlaywrightAvailable()
    {
        var playwrightType = CompatibilityTestHooks.ResolveType(
            "Microsoft.Playwright.IPage, Microsoft.Playwright");
        return playwrightType != null;
    }

    private static string GetPlatformName() => CompatibilityTestHooks.ResolvePlatformName();
}
