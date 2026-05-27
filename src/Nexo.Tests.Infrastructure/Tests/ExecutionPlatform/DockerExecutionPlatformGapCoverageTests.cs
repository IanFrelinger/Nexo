using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Docker.DotNet;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Infrastructure.Testing.ExecutionPlatform;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.ExecutionPlatform;

public sealed class DockerExecutionPlatformGapCoverageTests
{
    [Fact]
    public void Constructor_accepts_injected_docker_client_without_disposing_it()
    {
        var client = (DockerClient)RuntimeHelpers.GetUninitializedObject(typeof(DockerClient));
        var platform = new DockerExecutionPlatform(NullLogger<DockerExecutionPlatform>.Instance, client);

        platform.PlatformName.Should().Be("Docker");
        platform.Dispose();
    }

    [Fact]
    public void GetDockerUri_returns_platform_specific_endpoint()
    {
        var uri = InvokeGetDockerUri();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            uri.Scheme.Should().Be("npipe");
        else
            uri.Scheme.Should().Be("unix");
    }

    private static Uri InvokeGetDockerUri()
    {
        var method = typeof(DockerExecutionPlatform).GetMethod(
            "GetDockerUri",
            BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return (Uri)method!.Invoke(null, null)!;
    }
}
