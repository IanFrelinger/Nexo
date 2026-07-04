using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Infrastructure.Analysis.Rules;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

/// <summary>Tests for infrastructure analysis gap coverage.</summary>
public class InfrastructureAnalysisGapCoverageTests
{
    [Fact]
    public void CodeQualityRule_exposes_metadata()
    {
        var rule = new CodeQualityRule(NullLogger<CodeQualityRule>.Instance);
        rule.Name.Should().Be("CodeQuality");
        rule.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CodeQualityRule_skips_non_managed_assemblies()
    {
        var path = Path.Combine(Path.GetTempPath(), "not-an-assembly-" + Guid.NewGuid() + ".dll");
        await File.WriteAllTextAsync(path, "not a pe file");
        try
        {
            var rule = new CodeQualityRule(NullLogger<CodeQualityRule>.Instance);
            var violations = await rule.AnalyzeAsync(new FileInfo(path));
            violations.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task CodeQualityRule_handles_missing_files_gracefully()
    {
        var rule = new CodeQualityRule(NullLogger<CodeQualityRule>.Instance);
        var violations = await rule.AnalyzeAsync(new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll")));
        violations.Should().BeEmpty();
    }

    [Fact]
    public void SecurityAnalysisRule_exposes_metadata()
    {
        var rule = new SecurityAnalysisRule(NullLogger<SecurityAnalysisRule>.Instance);
        rule.Name.Should().Be("SecurityScan");
    }

    [Fact]
    public async Task SecurityAnalysisRule_reports_invalid_pe_scan_errors_as_findings()
    {
        var path = Path.Combine(Path.GetTempPath(), "not-managed-" + Guid.NewGuid() + ".dll");
        await File.WriteAllTextAsync(path, "not a pe file");
        try
        {
            var rule = new SecurityAnalysisRule(NullLogger<SecurityAnalysisRule>.Instance);
            var violations = await rule.AnalyzeAsync(new FileInfo(path));
            violations.Should().ContainSingle(v =>
                v.Rule == "SecurityScan" &&
                v.Severity == Nexo.Core.Domain.Values.RiskLevel.High &&
                v.Message.Contains("finding", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task SecurityAnalysisRule_records_scan_errors_as_violations()
    {
        var rule = new SecurityAnalysisRule(NullLogger<SecurityAnalysisRule>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var violations = await rule.AnalyzeAsync(
            new FileInfo(Path.Combine(Path.GetTempPath(), "scan-" + Guid.NewGuid() + ".dll")),
            cts.Token);

        violations.Should().ContainSingle(v => v.Rule == "SecurityScan" && v.Message.Contains("Security scan error"));
    }

    [Fact]
    public async Task CodeQualityRule_analyzes_managed_test_assembly()
    {
        var rule = new CodeQualityRule(NullLogger<CodeQualityRule>.Instance);
        var assemblyPath = typeof(InfrastructureAnalysisGapCoverageTests).Assembly.Location;

        var violations = await rule.AnalyzeAsync(new FileInfo(assemblyPath));

        violations.Should().NotBeNull();
        violations.Should().Contain(v =>
            v.Rule == "CodeQuality" &&
            v.Message.Contains("complexity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SecurityAnalysisRule_analyzes_managed_test_assembly()
    {
        var rule = new SecurityAnalysisRule(NullLogger<SecurityAnalysisRule>.Instance);
        var assemblyPath = typeof(InfrastructureAnalysisGapCoverageTests).Assembly.Location;

        var violations = await rule.AnalyzeAsync(new FileInfo(assemblyPath));

        violations.Should().NotBeNull();
    }

    [Fact]
    public void CodeQualityRule_constructor_rejects_null_logger()
    {
        var act = () => new CodeQualityRule(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CodeQualityRule_logs_and_returns_empty_on_unexpected_analysis_errors()
    {
        var rule = new CodeQualityRule(NullLogger<CodeQualityRule>.Instance);
        var dir = Path.Combine(Path.GetTempPath(), "nexo-not-an-assembly-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var violations = await rule.AnalyzeAsync(new FileInfo(dir));
            violations.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(dir);
        }
    }

    [Fact]
    public void SecurityAnalysisRule_constructor_rejects_null_logger()
    {
        var act = () => new SecurityAnalysisRule(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SecurityAnalysisRule_reports_findings_when_dangerous_apis_present()
    {
        var assemblyPath = await BuildDangerousTestAssemblyAsync();
        try
        {
            var rule = new SecurityAnalysisRule(NullLogger<SecurityAnalysisRule>.Instance);
            var violations = await rule.AnalyzeAsync(new FileInfo(assemblyPath));

            violations.Should().ContainSingle(v =>
                v.Rule == "SecurityScan" &&
                v.Severity == Nexo.Core.Domain.Values.RiskLevel.High &&
                v.Message.Contains("finding", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            var dir = Path.GetDirectoryName(assemblyPath);
            while (!string.IsNullOrEmpty(dir))
            {
                if (Path.GetFileName(dir).StartsWith("nexo-dangerous-asm-", StringComparison.Ordinal))
                {
                    if (Directory.Exists(dir))
                        Directory.Delete(dir, recursive: true);
                    break;
                }

                dir = Path.GetDirectoryName(dir);
            }
        }
    }

    private static async Task<string> BuildDangerousTestAssemblyAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-dangerous-asm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        await File.WriteAllTextAsync(Path.Combine(root, "Dangerous.cs"), """
            using System.Reflection;
            namespace Nexo.TestDangerous;
            /// <summary>Tests for dangerous.</summary>
            public static class Dangerous
            {
                /// <summary>Trigger.</summary>
                public static void Trigger() => Assembly.Load("System.Runtime");
            }
            """);

        await File.WriteAllTextAsync(Path.Combine(root, "Dangerous.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        var build = await RunProcessAsync("dotnet", $"build \"{root}/Dangerous.csproj\" -c Release -v q");
        build.ExitCode.Should().Be(0, because: build.StdErr);

        var dll = Directory
            .EnumerateFiles(Path.Combine(root, "bin", "Release", "net8.0"), "*.dll")
            .First(path => Path.GetFileName(path).StartsWith("Dangerous", StringComparison.Ordinal));
        return dll;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(string file, string args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stdout, stderr);
    }
}
