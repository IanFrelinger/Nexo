// Parse deps.json and copy all required assemblies to output directory.
// This is a C# program that can be compiled and run with: dotnet run --project <path> -- <args>
// Or compiled to a single file and executed.

using System;
using System.IO;
using System.Text.Json;
using System.Linq;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: copy-assemblies <deps.json> <output-dir> [nuget-packages-root]");
    Environment.Exit(1);
}

var depsJsonPath = args[0];
var outputDir = args[1];
// Windows callers can pass an escaped quote suffix when an output path ends in '\' and
// the argument itself is quoted by MSBuild Exec. Trim any accidental wrapping quotes and
// normalize trailing separators so directory creation always receives a valid path.
outputDir = outputDir.Trim().Trim('"');
outputDir = outputDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
if (string.IsNullOrWhiteSpace(outputDir))
{
    Console.Error.WriteLine("Output directory argument is empty after normalization.");
    Environment.Exit(1);
}
var nugetRoot = args.Length > 2 ? args[2] : Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".nuget", "packages");

if (!File.Exists(depsJsonPath))
{
    Console.Error.WriteLine($"deps.json not found: {depsJsonPath}");
    Environment.Exit(1);
}

// Ensure output directory exists
Directory.CreateDirectory(outputDir);

// Parse deps.json
string jsonContent = File.ReadAllText(depsJsonPath);
using JsonDocument doc = JsonDocument.Parse(jsonContent);
var root = doc.RootElement;

if (!root.TryGetProperty("targets", out var targetsElement))
{
    Console.Error.WriteLine("deps.json does not contain 'targets' property");
    Environment.Exit(1);
}

// Find the runtime target (use runtimeTarget from root, or first NETCoreApp target)
string? targetName = null;
if (root.TryGetProperty("runtimeTarget", out var rtElement) && rtElement.TryGetProperty("name", out var rtName))
    targetName = rtName.GetString();

JsonElement runtime;
if (!string.IsNullOrEmpty(targetName) && targetsElement.TryGetProperty(targetName, out var rtValue))
    runtime = rtValue;
else
{
    var first = targetsElement.EnumerateObject().FirstOrDefault(p => p.Name.Contains("NETCoreApp", StringComparison.OrdinalIgnoreCase));
    if (first.Value.ValueKind == JsonValueKind.Undefined)
    {
        Console.Error.WriteLine("Could not find runtime target in deps.json");
        Environment.Exit(1);
    }
    runtime = first.Value;
}
int copied = 0;
int failed = 0;

foreach (var libProperty in runtime.EnumerateObject())
{
    var libName = libProperty.Name;
    
    // Skip if not a package reference (package references have '/' separator)
    if (!libName.Contains('/'))
        continue;
    
    if (!libProperty.Value.TryGetProperty("runtime", out var runtimePaths))
        continue;
    
    // Extract package name and version
    var parts = libName.Split('/', 2);
    if (parts.Length != 2)
        continue;
    
    var pkgName = parts[0];
    var pkgVersion = parts[1];
    var pkgNameLower = pkgName.ToLowerInvariant(); // NuGet package folders are lowercase
    
    // Process each runtime path
    foreach (var runtimePathProperty in runtimePaths.EnumerateObject())
    {
        var path = runtimePathProperty.Name;
        
        // Only process .dll files
        if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            continue;
        
        // Build source path - normalize path separators
        var libPath = path.Replace('/', Path.DirectorySeparatorChar);
        var sourcePath = Path.Combine(nugetRoot, pkgNameLower, pkgVersion, libPath);
        
        // Build destination path
        var dllName = Path.GetFileName(path);
        var destPath = Path.Combine(outputDir, dllName);
        
        // Copy if source exists
        if (File.Exists(sourcePath))
        {
            try
            {
                File.Copy(sourcePath, destPath, overwrite: true);
                copied++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to copy {sourcePath}: {ex.Message}");
                failed++;
            }
        }
        // Don't fail on missing assemblies - some might be in different locations
    }
}

Console.WriteLine($"Copied {copied} assemblies, {failed} failures");
Environment.Exit(failed == 0 ? 0 : 1);
