using FluentAssertions;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks.Ports;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>Port (b): DockerSandboxProvider builds SandboxSpec from artifact + scratch.</summary>
public sealed class DockerSandboxProviderTests
{
    [Fact]
    public void BuildSpec_ReturnsSandboxSpec_WithNetworkNoneAndGxxEntrypoint()
    {
        var poco = CreateMinimalPoco();
        var scratch = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "sbx-" + Guid.NewGuid().ToString("n"))).FullName;
        try
        {
            var provider = new DockerSandboxProvider(new DockerSandboxProviderOptions
            {
                PocoContext = poco,
                Image = "dep-extract:latest",
                Entrypoint = "g++"
            });

            var specObj = provider.BuildSpec(
                new GeneratedArtifact { Content = "#pragma once\nclass X{};\n" },
                scratch);

            var spec = specObj.Should().BeOfType<SandboxSpec>().Subject;
            spec.Network.Should().Be(NetworkAccess.None);
            spec.Entrypoint.Should().Be("g++");
            spec.Image.Should().Be("dep-extract:latest");
            spec.Mounts.Should().Contain(m => m.ContainerPath == "/common" && m.ReadOnly);
            spec.Mounts.Should().Contain(m => m.ContainerPath == "/work" && !m.ReadOnly);
            File.Exists(Path.Combine(scratch, "adapted_reader.hpp")).Should().BeTrue();
            File.Exists(Path.Combine(scratch, "smoke.cpp")).Should().BeTrue();
        }
        finally
        {
            TryDelete(poco);
            TryDelete(scratch);
        }
    }

    [Fact]
    public void BuildSpec_MountsParserAndShims_WhenConfigured()
    {
        var poco = CreateMinimalPoco();
        var parser = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "p-" + Guid.NewGuid().ToString("n"))).FullName;
        var shims = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "s-" + Guid.NewGuid().ToString("n"))).FullName;
        var scratch = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "w-" + Guid.NewGuid().ToString("n"))).FullName;
        File.WriteAllText(Path.Combine(parser, "A.hpp"), "#pragma once\n");
        try
        {
            var provider = new DockerSandboxProvider(new DockerSandboxProviderOptions
            {
                PocoContext = poco,
                ParserDir = parser,
                ShimDir = shims
            });

            var spec = (SandboxSpec)provider.BuildSpec(new GeneratedArtifact { Content = "x" }, scratch);

            spec.Mounts.Should().Contain(m => m.ContainerPath == "/parser");
            spec.Mounts.Should().Contain(m => m.ContainerPath == "/shims");
            spec.Command.Should().Contain("-I/shims");
            spec.Command.Should().Contain("-I/parser");
        }
        finally
        {
            TryDelete(poco);
            TryDelete(parser);
            TryDelete(shims);
            TryDelete(scratch);
        }
    }

    [Fact]
    public void BuildSpec_Throws_WhenPocoMissing()
    {
        var provider = new DockerSandboxProvider(new DockerSandboxProviderOptions
        {
            PocoContext = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("n"))
        });
        var scratch = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "w-" + Guid.NewGuid().ToString("n"))).FullName;
        try
        {
            var act = () => provider.BuildSpec(new GeneratedArtifact { Content = "x" }, scratch);
            act.Should().Throw<InvalidOperationException>().WithMessage("*processing.hpp*");
        }
        finally
        {
            TryDelete(scratch);
        }
    }

    private static string CreateMinimalPoco()
    {
        var root = Path.Combine(Path.GetTempPath(), "poco-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(root, "common"));
        File.WriteAllText(Path.Combine(root, "common", "processing.hpp"), "#pragma once\n");
        return root;
    }

    private static void TryDelete(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch { /* ignore */ }
    }
}
