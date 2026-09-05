using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Certification;

namespace Ashlar.Infrastructure.Adaptation.Generation;

/// <summary>
/// Compiles a generated brick manifest under the same closed-world emit the disk
/// certifier uses, then activates those bytes. The resulting
/// <see cref="GateEmittedArtifact"/> is what generate→certify binds.
/// </summary>
public static class GeneratedBrickBuilder
{
    /// <summary>Build asynchronously.</summary>
    public static Task<BuiltGeneratedBrick> BuildAsync(
        BrickManifest manifest,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(manifest.ImplementationSource))
            throw new InvalidOperationException("Manifest has no implementation source.");

        var projectDir = Path.Combine(Path.GetTempPath(), "ashlar-gen-brick", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDir);

        var className = manifest.GeneratedClassName ?? InferClassName(manifest.ImplementationSource);
        var references = BrickCertificationProjectLoader.DefaultCompilationReferences();
        var artifact = GateEmittedArtifactCompiler.Compile(manifest.ImplementationSource, references);
        var brick = CertifiedBrickActivator.Activate(artifact);

        var projectPath = Path.Combine(projectDir, $"{className}.csproj");
        File.WriteAllText(projectPath, CreateProjectFile(className));

        return Task.FromResult(new BuiltGeneratedBrick(
            brick,
            artifact.BrickTypeName,
            manifest.ImplementationSource,
            projectPath,
            references,
            artifact));
    }

    private static string CreateProjectFile(string? assemblyName = null) => $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    {(string.IsNullOrWhiteSpace(assemblyName) ? string.Empty : $"<AssemblyName>{assemblyName}</AssemblyName>")}
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.0" />
    <PackageReference Include="Ashlar.Authoring" Version="0.1.0" />
  </ItemGroup>
</Project>
""";

    private static string InferClassName(string source)
    {
        var match = System.Text.RegularExpressions.Regex.Match(source, @"public\s+sealed\s+class\s+(\w+)");
        return match.Success ? match.Groups[1].Value : "GeneratedBrick";
    }
}
