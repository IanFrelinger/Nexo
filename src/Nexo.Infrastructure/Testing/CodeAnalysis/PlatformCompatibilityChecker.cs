using System.Runtime.InteropServices;

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
        var result = new CodeAnalysisCompatibilityResult
        {
            Platform = GetPlatformName(),
            IsCompatible = true,
            Issues = new List<string>()
        };

        // Check Roslyn availability
        try
        {
            var roslynAvailable = CheckRoslynAvailable();
            if (!roslynAvailable)
            {
                result.IsCompatible = false;
                result.Issues.Add("Microsoft.CodeAnalysis.CSharp (Roslyn) is not available");
            }
        }
        catch (Exception ex)
        {
            result.IsCompatible = false;
            result.Issues.Add($"Roslyn check failed: {ex.Message}");
        }

        // Check ICSharpCode.Decompiler availability
        try
        {
            var decompilerAvailable = CheckDecompilerAvailable();
            if (!decompilerAvailable)
            {
                result.IsCompatible = false;
                result.Issues.Add("ICSharpCode.Decompiler is not available");
            }
        }
        catch (Exception ex)
        {
            result.IsCompatible = false;
            result.Issues.Add($"Decompiler check failed: {ex.Message}");
        }

        // Check System.Reflection capabilities
        try
        {
            var reflectionAvailable = CheckReflectionAvailable();
            if (!reflectionAvailable)
            {
                result.IsCompatible = false;
                result.Issues.Add("System.Reflection capabilities are limited");
            }
        }
        catch (Exception ex)
        {
            result.IsCompatible = false;
            result.Issues.Add($"Reflection check failed: {ex.Message}");
        }

        return result;
    }

    private static bool CheckRoslynAvailable()
    {
        try
        {
            // Try to use Roslyn types
            var syntaxTreeType = Type.GetType("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree, Microsoft.CodeAnalysis.CSharp");
            return syntaxTreeType != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckDecompilerAvailable()
    {
        try
        {
            // Try to use ICSharpCode.Decompiler types
            var decompilerType = Type.GetType("ICSharpCode.Decompiler.CSharp.CSharpDecompiler, ICSharpCode.Decompiler");
            return decompilerType != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckReflectionAvailable()
    {
        try
        {
            // Check if basic reflection works
            var testType = typeof(object);
            var methods = testType.GetMethods();
            return methods.Length > 0;
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
/// Result of platform compatibility check.
/// </summary>
public record CodeAnalysisCompatibilityResult(
    string Platform,
    bool IsCompatible,
    List<string> Issues)
{
    public CodeAnalysisCompatibilityResult() : this("Unknown", true, new List<string>())
    {
    }
}
