using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Testing.CodeAnalysis;

namespace Nexo.Tests.Infrastructure.Certification.Reuse;

/// <summary>
/// Minimal Roslyn loader for untouched certified brick source (consumer-side, not the cert gate).
/// </summary>
internal static class CertifiedBrickCompiler
{
    public static DomainBrick InstantiateBrick(string source, string brickTypeName)
    {
        var assemblyName = $"CertifiedBrick_{Guid.NewGuid():N}";
        var outputPath = Path.Combine(Path.GetTempPath(), $"{assemblyName}.dll");
        var references = new List<string>
        {
            typeof(DomainBrick).Assembly.Location,
            typeof(BrickInput).Assembly.Location
        };

        var compiler = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance);
        var compile = compiler.CompileAsync(
            WrapForRoslynCompile(source),
            assemblyName,
            outputPath,
            references).GetAwaiter().GetResult();

        if (!compile.Success || string.IsNullOrWhiteSpace(compile.AssemblyPath))
        {
            throw new InvalidOperationException(
                $"DomainBrick compilation failed: {string.Join("; ", compile.Errors)}");
        }

        var assemblyBytes = File.ReadAllBytes(compile.AssemblyPath);
        var assembly = Assembly.Load(assemblyBytes);
        var type = assembly.GetType(brickTypeName, throwOnError: true)
            ?? throw new InvalidOperationException($"Type {brickTypeName} not found in certified brick assembly.");
        return (DomainBrick)Activator.CreateInstance(type)!;
    }

    private static string WrapForRoslynCompile(string sourceCode) =>
        """
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

""" + sourceCode;
}
