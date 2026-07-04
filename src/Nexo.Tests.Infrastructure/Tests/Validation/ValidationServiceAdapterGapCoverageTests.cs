using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Validation.Models;
using Nexo.Infrastructure.Validation.Adapters;
using Nexo.Infrastructure.Validation.Parsers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Validation;

/// <summary>Tests for validation service adapter gap coverage.</summary>
[Collection("ValidationAdapterCwd")]
public class ValidationServiceAdapterGapCoverageTests
{
    [Fact]
    public async Task ValidateAsync_returns_skipped_result_when_no_test_projects()
    {
        var adapter = CreateAdapter();
        var original = Directory.GetCurrentDirectory();
        var temp = Path.Combine(Path.GetTempPath(), "nexo-validate-" + Guid.NewGuid());
        Directory.CreateDirectory(temp);

        try
        {
            Directory.SetCurrentDirectory(temp);
            var result = await adapter.ValidateAsync("filter", progress: null, CancellationToken.None);
            result.Passed.Should().BeTrue();
            result.Message.Should().Contain("skipped");
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_reports_progress_when_no_projects_found()
    {
        var adapter = CreateAdapter();
        var original = Directory.GetCurrentDirectory();
        var temp = Path.Combine(Path.GetTempPath(), "nexo-validate-progress-" + Guid.NewGuid());
        Directory.CreateDirectory(temp);
        var reports = new List<ProgressReport>();

        try
        {
            Directory.SetCurrentDirectory(temp);
            await adapter.ValidateAsync(null, new Progress<ProgressReport>(r => reports.Add(r)), CancellationToken.None);
            reports.Should().NotBeEmpty();
            reports.Should().Contain(r => r.Percentage == 100 || r.Message.Contains("skipped", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_counts_build_failure_as_failed_test()
    {
        var adapter = CreateAdapter();
        var original = Directory.GetCurrentDirectory();
        var temp = Path.Combine(Path.GetTempPath(), "nexo-validate-buildfail-" + Guid.NewGuid());
        Directory.CreateDirectory(temp);
        var csprojDir = Path.Combine(temp, "tests");
        Directory.CreateDirectory(csprojDir);
        await File.WriteAllTextAsync(
            Path.Combine(csprojDir, "BrokenTests.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup><Compile Include="Missing.cs" /></ItemGroup>
            </Project>
            """);

        try
        {
            Directory.SetCurrentDirectory(temp);
            var result = await adapter.ValidateAsync("filter", progress: null, CancellationToken.None);
            result.Passed.Should().BeFalse();
            result.TestsFailed.Should().BeGreaterThan(0);
            result.TestResults.Should().Contain(r => !r.Passed && r.Message != null && r.Message.Contains("build failed", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var parser = Mock.Of<ITestResultParser>();
        var act = () => new ValidationServiceAdapter(null!, parser);
        act.Should().Throw<ArgumentNullException>();

        var act2 = () => new ValidationServiceAdapter(NullLogger<ValidationServiceAdapter>.Instance, null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidateAsync_runs_passing_test_project_and_aggregates_trx_results()
    {
        var parser = new Mock<ITestResultParser>();
        parser.Setup(p => p.ParseAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new TestResult { Name = "PassTests.Ok", Passed = true },
                new TestResult { Name = "PassTests.Other", Passed = true },
            });

        var adapter = new ValidationServiceAdapter(NullLogger<ValidationServiceAdapter>.Instance, parser.Object);
        var original = Directory.GetCurrentDirectory();
        var temp = CreatePassingTestProjectDir();

        try
        {
            Directory.SetCurrentDirectory(temp);
            var reports = new List<ProgressReport>();
            var result = await adapter.ValidateAsync(
                "FullyQualifiedName~Ok",
                new Progress<ProgressReport>(r => reports.Add(r)),
                CancellationToken.None);

            result.Passed.Should().BeTrue();
            result.TestsRun.Should().Be(2);
            result.TestsPassed.Should().Be(2);
            result.TestsFailed.Should().Be(0);
            reports.Should().Contain(r => r.Percentage == 100);
            parser.Verify(p => p.ParseAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_reports_failed_tests_from_trx_parser()
    {
        var parser = new Mock<ITestResultParser>();
        parser.Setup(p => p.ParseAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new TestResult { Name = "FailTests.Boom", Passed = false, Message = "assert failed" },
            });

        var adapter = new ValidationServiceAdapter(NullLogger<ValidationServiceAdapter>.Instance, parser.Object);
        var original = Directory.GetCurrentDirectory();
        var temp = CreateFailingTestProjectDir();

        try
        {
            Directory.SetCurrentDirectory(temp);
            var result = await adapter.ValidateAsync(null, progress: null, CancellationToken.None);

            result.Passed.Should().BeFalse();
            result.TestsFailed.Should().BeGreaterThan(0);
            result.Message.Should().Contain("failed");
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_uses_exit_code_when_trx_parser_returns_empty()
    {
        var parser = new Mock<ITestResultParser>();
        parser.Setup(p => p.ParseAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TestResult>());

        var adapter = new ValidationServiceAdapter(NullLogger<ValidationServiceAdapter>.Instance, parser.Object);
        var original = Directory.GetCurrentDirectory();
        var temp = CreatePassingTestProjectDir();

        try
        {
            Directory.SetCurrentDirectory(temp);
            var result = await adapter.ValidateAsync(null, progress: null, CancellationToken.None);

            result.Passed.Should().BeTrue();
            result.TestsRun.Should().Be(0);
            result.TestsPassed.Should().Be(0);
            parser.Verify(p => p.ParseAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_returns_error_when_cancelled_with_test_projects()
    {
        var adapter = CreateAdapter();
        var original = Directory.GetCurrentDirectory();
        var temp = Path.Combine(Path.GetTempPath(), "nexo-validate-cancel-" + Guid.NewGuid());
        Directory.CreateDirectory(temp);
        await File.WriteAllTextAsync(Path.Combine(temp, "SampleTests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            Directory.SetCurrentDirectory(temp);
            var result = await adapter.ValidateAsync("filter", progress: null, cts.Token);
            result.Passed.Should().BeFalse();
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_counts_parser_failure_as_failed_test()
    {
        var parser = new Mock<ITestResultParser>();
        parser.Setup(p => p.ParseAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("trx parse failed"));

        var adapter = new ValidationServiceAdapter(NullLogger<ValidationServiceAdapter>.Instance, parser.Object);
        var original = Directory.GetCurrentDirectory();
        var temp = CreatePassingTestProjectDir();

        try
        {
            Directory.SetCurrentDirectory(temp);
            var result = await adapter.ValidateAsync(null, progress: null, CancellationToken.None);

            result.Passed.Should().BeFalse();
            result.TestsFailed.Should().BeGreaterThan(0);
            result.TestResults.Should().Contain(r =>
                !r.Passed && r.Message != null && r.Message.Contains("trx parse failed", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    private static string CreatePassingTestProjectDir()
    {
        var temp = Path.Combine(Path.GetTempPath(), "nexo-validate-pass-" + Guid.NewGuid());
        var testsDir = Path.Combine(temp, "tests");
        Directory.CreateDirectory(testsDir);
        File.WriteAllText(Path.Combine(testsDir, "PassTests.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <IsPackable>false</IsPackable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
                <PackageReference Include="xunit" Version="2.9.3" />
                <PackageReference Include="xunit.runner.visualstudio" Version="3.0.0">
                  <PrivateAssets>all</PrivateAssets>
                  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
                </PackageReference>
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(testsDir, "PassTests.cs"), """
            using Xunit;
            /// <summary>Tests for pass.</summary>
            public class PassTests
            {
                /// <summary>Ok.</summary>
                [Fact] public void Ok() { }
                [Fact] public void Other() { }
            }
            """);
        return temp;
    }

    private static string CreateFailingTestProjectDir()
    {
        var temp = Path.Combine(Path.GetTempPath(), "nexo-validate-fail-" + Guid.NewGuid());
        var testsDir = Path.Combine(temp, "tests");
        Directory.CreateDirectory(testsDir);
        File.WriteAllText(Path.Combine(testsDir, "FailTests.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <IsPackable>false</IsPackable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
                <PackageReference Include="xunit" Version="2.9.3" />
                <PackageReference Include="xunit.runner.visualstudio" Version="3.0.0">
                  <PrivateAssets>all</PrivateAssets>
                  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
                </PackageReference>
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(testsDir, "FailTests.cs"), """
            using Xunit;
            /// <summary>Tests for fail.</summary>
            public class FailTests
            {
                /// <summary>Boom.</summary>
                [Fact] public void Boom() => Assert.True(false);
            }
            """);
        return temp;
    }

    private static ValidationServiceAdapter CreateAdapter()
    {
        var parser = new Mock<ITestResultParser>();
        parser.Setup(p => p.ParseAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TestResult>());
        /// <summary>Validation service adapter.</summary>
        return new ValidationServiceAdapter(NullLogger<ValidationServiceAdapter>.Instance, parser.Object);
    }
}
