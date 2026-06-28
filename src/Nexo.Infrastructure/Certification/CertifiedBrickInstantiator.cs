using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure.Testing.CodeAnalysis;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Compiles and instantiates certified brick source for Tier-2 state transition replay.
/// </summary>
public sealed class CertifiedBrickInstantiator
{
    private static readonly Regex TypeNamePattern = new(
        @"public\s+(?:sealed\s+)?class\s+(?<name>\w+)\s*:\s*Brick\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly RoslynCodeAnalysisService _compiler = new(NullLogger<RoslynCodeAnalysisService>.Instance);

    public Brick Instantiate(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Brick source is required.", nameof(source));

        var typeName = ResolveTypeName(source);
        var assemblyName = $"CertifiedBrick_{Guid.NewGuid():N}";
        var outputPath = Path.Combine(Path.GetTempPath(), $"{assemblyName}.dll");
        var references = new List<string>
        {
            typeof(Brick).Assembly.Location,
            typeof(Nexo.Core.Domain.Execution.BrickInput).Assembly.Location,
            typeof(System.Security.Cryptography.SHA256).Assembly.Location
        };

        var compile = _compiler.CompileAsync(
            WrapForRoslynCompile(source),
            assemblyName,
            outputPath,
            references).GetAwaiter().GetResult();

        if (!compile.Success || string.IsNullOrWhiteSpace(compile.AssemblyPath))
        {
            throw new InvalidOperationException(
                $"Brick compilation failed: {string.Join("; ", compile.Errors)}");
        }

        var assemblyBytes = File.ReadAllBytes(compile.AssemblyPath);
        var assembly = Assembly.Load(assemblyBytes);
        var type = assembly.GetType(typeName, throwOnError: true)
            ?? throw new InvalidOperationException($"Type {typeName} not found in certified brick assembly.");
        return (Brick)Activator.CreateInstance(type)!;
    }

    internal static string ResolveTypeName(string source)
    {
        var match = TypeNamePattern.Match(source);
        if (!match.Success)
            throw new InvalidOperationException("Certified brick source must declare a public class inheriting Brick.");

        var className = match.Groups["name"].Value;
        var namespaceMatch = Regex.Match(
            source,
            @"namespace\s+(?<ns>[\w.]+)\s*;",
            RegexOptions.CultureInvariant);

        return namespaceMatch.Success
            ? $"{namespaceMatch.Groups["ns"].Value}.{className}"
            : className;
    }

    private static string WrapForRoslynCompile(string sourceCode) =>
        """
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

""" + sourceCode;
}
