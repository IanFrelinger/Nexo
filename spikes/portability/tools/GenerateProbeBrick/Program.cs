using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Infrastructure.Adaptation;

var root = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath("spikes/portability/generated");
var version = args.Length > 1 ? args[1] : File.ReadAllText(Path.Combine(FindRepoRoot(), "VERSION")).Trim();

var generator = new NewBrickGenerator();
var manifest = await generator.GenerateAsync("ErrorSummaryExtractor").ConfigureAwait(false);

if (string.IsNullOrWhiteSpace(manifest.ImplementationSource))
{
    Console.Error.WriteLine("Generator did not emit ImplementationSource for ErrorSummaryExtractor.");
    return 1;
}

var brickDir = Path.Combine(root, "ErrorSummaryExtractorBrick");
Directory.CreateDirectory(brickDir);

var brickCsPath = Path.Combine(brickDir, "ErrorSummaryExtractorBrick.cs");
await File.WriteAllTextAsync(brickCsPath, manifest.ImplementationSource).ConfigureAwait(false);

var csprojPath = Path.Combine(brickDir, "ErrorSummaryExtractorBrick.csproj");
await File.WriteAllTextAsync(csprojPath, $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Nexo.Brick.Contracts" Version="{version}" />
    <PackageReference Include="Nexo.Authoring" Version="{version}" />
  </ItemGroup>
</Project>
""").ConfigureAwait(false);

var manifestPath = Path.Combine(root, "manifest.json");
var jsonOptions = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, jsonOptions)).ConfigureAwait(false);

Console.WriteLine($"Generated probe brick at {brickDir}");
Console.WriteLine($"Manifest: {manifestPath}");
return 0;

static string FindRepoRoot()
{
    var dir = Directory.GetCurrentDirectory();
    while (!string.IsNullOrEmpty(dir))
    {
        if (File.Exists(Path.Combine(dir, "VERSION")) && File.Exists(Path.Combine(dir, "Nexo.sln")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
    }

    throw new InvalidOperationException("Could not locate Nexo repo root.");
}
