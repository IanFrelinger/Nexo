namespace Nexo.Infrastructure.Testing.CodeAnalysis;

/// <summary>
/// Checks platform compatibility for code analysis services.
/// 
/// Validates that required dependencies and APIs are available
/// on the current platform.
/// </summary>
public static class PlatformCompatibilityChecker
{
    /// <summary>
    /// Checks if code analysis services are compatible with the current platform.
    /// </summary>
    public static CodeAnalysisCompatibilityResult CheckCompatibility()
    {
        var platform = GetPlatformName();
        var isCompatible = true;
        var issues = new List<string>();

        // Check Roslyn availability
        try
        {
            var roslynAvailable = CheckRoslynAvailable();
            if (!roslynAvailable)
            {
                isCompatible = false;
                issues.Add("Microsoft.CodeAnalysis.CSharp (Roslyn) is not available");
            }
        }
        catch (Exception ex)
        {
            isCompatible = false;
            issues.Add($"Roslyn check failed: {ex.Message}");
        }

        // Check ICSharpCode.Decompiler availability
        try
        {
            var decompilerAvailable = CheckDecompilerAvailable();
            if (!decompilerAvailable)
            {
                isCompatible = false;
                issues.Add("ICSharpCode.Decompiler is not available");
            }
        }
        catch (Exception ex)
        {
            isCompatible = false;
            issues.Add($"Decompiler check failed: {ex.Message}");
        }

        // Check System.Reflection capabilities
        try
        {
            var reflectionAvailable = CheckReflectionAvailable();
            if (!reflectionAvailable)
            {
                isCompatible = false;
                issues.Add("System.Reflection capabilities are limited");
            }
        }
        catch (Exception ex)
        {
            isCompatible = false;
            issues.Add($"Reflection check failed: {ex.Message}");
        }

        return new CodeAnalysisCompatibilityResult(platform, isCompatible, issues);
    }

    private static bool CheckRoslynAvailable()
    {
        var syntaxTreeType = CompatibilityTestHooks.ResolveType(
            "Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree, Microsoft.CodeAnalysis.CSharp");
        return syntaxTreeType != null;
    }

    private static bool CheckDecompilerAvailable()
    {
        var decompilerType = CompatibilityTestHooks.ResolveType(
            "ICSharpCode.Decompiler.CSharp.CSharpDecompiler, ICSharpCode.Decompiler");
        return decompilerType != null;
    }

    private static bool CheckReflectionAvailable() => CompatibilityTestHooks.ProbeReflection();

    private static string GetPlatformName() => CompatibilityTestHooks.ResolvePlatformName();
}
