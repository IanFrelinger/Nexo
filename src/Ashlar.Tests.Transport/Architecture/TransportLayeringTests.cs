using FluentAssertions;
using System.Xml.Linq;
using Xunit;

namespace Ashlar.Tests.Transport.Architecture;

/// <summary>Tests for transport layering.</summary>
public sealed class TransportLayeringTests
{
    [Fact]
    public void AshlarTransportGrpc_MustNotReference_AshlarRuntime()
    {
        var references = ReadProjectReferences("Ashlar.Transport.Grpc/Ashlar.Transport.Grpc.csproj");
        references.Should().NotContain(r => r.Contains("Ashlar.Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AshlarTransportGrpc_MustNotReference_AshlarOrchestration()
    {
        var references = ReadProjectReferences("Ashlar.Transport.Grpc/Ashlar.Transport.Grpc.csproj");
        references.Should().NotContain(r => r.Contains("Ashlar.Orchestration", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AshlarTransportGrpcServer_MustNotReference_AshlarCoreDomain()
    {
        var references = ReadProjectReferences("Ashlar.Transport.Grpc.Server/Ashlar.Transport.Grpc.Server.csproj");
        references.Should().NotContain(r => r.Contains("Ashlar.Core.Domain", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AshlarTransportA2A_MustNotReference_AshlarRuntime()
    {
        var references = ReadProjectReferences("Ashlar.Transport.A2A/Ashlar.Transport.A2A.csproj");
        references.Should().NotContain(r => r.Contains("Ashlar.Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AshlarTransportA2A_MustNotReference_AshlarOrchestration()
    {
        var references = ReadProjectReferences("Ashlar.Transport.A2A/Ashlar.Transport.A2A.csproj");
        references.Should().NotContain(r => r.Contains("Ashlar.Orchestration", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AshlarTransportA2AServer_MustNotReference_AshlarCoreDomain()
    {
        var references = ReadProjectReferences("Ashlar.Transport.A2A.Server/Ashlar.Transport.A2A.Server.csproj");
        references.Should().NotContain(r => r.Contains("Ashlar.Core.Domain", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> ReadProjectReferences(string relativeProjectPath)
    {
        var srcRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var projectPath = Path.Combine(srcRoot, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(projectPath).Should().BeTrue();

        var doc = XDocument.Load(projectPath);
        return doc.Descendants("ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();
    }
}
