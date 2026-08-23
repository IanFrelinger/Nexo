namespace Ashlar.Infrastructure.Testing;

/// <summary>
/// Internal test hooks for platform compatibility checkers.
/// </summary>
internal static class CompatibilityTestHooks
{
    internal static Func<string, Type?>? TypeResolver { get; set; }

    internal static Func<bool>? ReflectionProbe { get; set; }

    internal static Func<string>? PlatformNameProvider { get; set; }

    internal static void Reset()
    {
        TypeResolver = null;
        ReflectionProbe = null;
        PlatformNameProvider = null;
    }

    internal static Type? ResolveType(string assemblyQualifiedName)
    {
        if (TypeResolver != null)
            return TypeResolver(assemblyQualifiedName);

        return Type.GetType(assemblyQualifiedName);
    }

    internal static bool ProbeReflection()
    {
        if (ReflectionProbe != null)
            return ReflectionProbe();

        var methods = typeof(object).GetMethods();
        return methods.Length > 0;
    }

    internal static string ResolvePlatformName()
    {
        if (PlatformNameProvider != null)
            return PlatformNameProvider();

        if (OperatingSystem.IsWindows())
            return "Windows";
        if (OperatingSystem.IsLinux())
            return "Linux";
        if (OperatingSystem.IsMacOS())
            return "macOS";
        return "Unknown";
    }
}
