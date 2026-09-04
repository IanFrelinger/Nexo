using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.HostProcess;
using Ashlar.Infrastructure.Testing.CodeAnalysis;

namespace Ashlar.Infrastructure.Adaptation.Generation;

/// <summary>
/// Builds and compiles a generated brick manifest into a runnable <see cref="DomainBrick"/> instance.
/// </summary>
public static class GeneratedBrickBuilder
{
    /// <summary>Build asynchronously.</summary>
    public static async Task<BuiltGeneratedBrick> BuildAsync(
        BrickManifest manifest,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifest.ImplementationSource))
            throw new InvalidOperationException("Manifest has no implementation source.");

        var projectDir = Path.Combine(Path.GetTempPath(), "ashlar-gen-brick", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDir);

        var className = manifest.GeneratedClassName ?? InferClassName(manifest.ImplementationSource);
        var brickTypeName = string.IsNullOrWhiteSpace(manifest.GeneratedNamespace)
            ? className
            : $"{manifest.GeneratedNamespace}.{className}";

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG")))
            return await BuildWithRoslynAsync(manifest, className, brickTypeName, projectDir, cancellationToken)
                .ConfigureAwait(false);

        var assemblyName = $"{className}_{Guid.NewGuid():N}";
        var sourcePath = Path.Combine(projectDir, $"{className}.cs");
        var projectPath = Path.Combine(projectDir, $"{assemblyName}.csproj");

        await File.WriteAllTextAsync(sourcePath, manifest.ImplementationSource, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(projectPath, CreateProjectFile(assemblyName), cancellationToken).ConfigureAwait(false);

        var buildDir = Path.Combine(projectDir, "bin");
        var build = await RunDotnetBuildAsync(projectPath, buildDir, cancellationToken).ConfigureAwait(false);
        if (build.ExitCode != 0)
            throw new InvalidOperationException($"Generated brick build failed: {build.Output}");

        var dllPath = Directory.GetFiles(buildDir, "*.dll", SearchOption.AllDirectories)
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException("Built generated brick assembly not found");

        var assemblyBytes = await File.ReadAllBytesAsync(dllPath, cancellationToken).ConfigureAwait(false);
        return LoadFencedBrick(assemblyBytes, brickTypeName, manifest, projectPath,
            Directory.GetFiles(buildDir, "*.dll", SearchOption.AllDirectories).Distinct().ToList());
    }

    private static async Task<BuiltGeneratedBrick> BuildWithRoslynAsync(
        BrickManifest manifest,
        string className,
        string brickTypeName,
        string projectDir,
        CancellationToken cancellationToken)
    {
        var assemblyName = $"{className}_{Guid.NewGuid():N}";
        var outputPath = Path.Combine(projectDir, $"{assemblyName}.dll");
        var references = new List<string>
        {
            typeof(DomainBrick).Assembly.Location,
            typeof(BrickInput).Assembly.Location
        };

        var compiler = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance);
        var compile = await compiler.CompileAsync(
            WrapForRoslynCompile(manifest.ImplementationSource!),
            assemblyName,
            outputPath,
            references,
            cancellationToken).ConfigureAwait(false);

        if (!compile.Success || string.IsNullOrWhiteSpace(compile.AssemblyPath))
            throw new InvalidOperationException($"Roslyn compile failed: {string.Join("; ", compile.Errors)}");

        var assemblyBytes = await File.ReadAllBytesAsync(compile.AssemblyPath, cancellationToken).ConfigureAwait(false);
        var projectPath = Path.Combine(projectDir, $"{className}.csproj");
        await File.WriteAllTextAsync(projectPath, CreateProjectFile(assemblyName), cancellationToken).ConfigureAwait(false);
        return LoadFencedBrick(assemblyBytes, brickTypeName, manifest, projectPath, references);
    }

    private static BuiltGeneratedBrick LoadFencedBrick(
        byte[] assemblyBytes,
        string brickTypeName,
        BrickManifest manifest,
        string projectPath,
        IReadOnlyList<string> references)
    {
        IlImportFence.Inspect(assemblyBytes);
        var assembly = Assembly.Load(assemblyBytes);
        var brickType = assembly.GetType(brickTypeName)
            ?? assembly.GetTypes().FirstOrDefault(t => typeof(DomainBrick).IsAssignableFrom(t) && !t.IsAbstract)
            ?? throw new InvalidOperationException("No DomainBrick type found in generated assembly");
        if (!typeof(DomainBrick).IsAssignableFrom(brickType) || brickType.IsAbstract)
            throw new InvalidOperationException($"Generated type '{brickType.FullName}' is not a concrete DomainBrick");

        var brick = (DomainBrick)Activator.CreateInstance(brickType)!;
        return new BuiltGeneratedBrick(
            brick,
            brickType.FullName!,
            manifest.ImplementationSource!,
            projectPath,
            references);
    }

    private static string WrapForRoslynCompile(string sourceCode) =>
        """
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainBrick = Ashlar.Core.Domain.Bricks.Brick;

""" + sourceCode;

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

    private static async Task<(int ExitCode, string Output)> RunDotnetBuildAsync(
        string csproj,
        string outputDir,
        CancellationToken cancellationToken)
    {
        var configFile = Environment.GetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG");
        var configArg = string.IsNullOrWhiteSpace(configFile)
            ? string.Empty
            : $" --configfile \"{configFile}\"";

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments =
                $"build \"{csproj}\" -c Release -o \"{outputDir}\" -v q{configArg} " +
                "-p:TreatWarningsAsErrors=false -p:NuGetAudit=false",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var run = await TimedProcess.RunAsync(psi, TimedProcess.RemediationTimeout, cancellationToken)
            .ConfigureAwait(false);
        return (run.ExitCode, run.StdOut + run.StdErr);
    }
}
