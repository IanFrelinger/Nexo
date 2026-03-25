using FluentAssertions;
using System.Xml.Linq;
using Xunit;

namespace Nexo.Tests.Transport.Architecture;

public sealed class TransportLayeringTests
{
    [Fact]
    public void NexoTransportGrpc_MustNotReference_NexoRuntime()
    {
        var references = ReadProjectReferences("Nexo.Transport.Grpc/Nexo.Transport.Grpc.csproj");
        references.Should().NotContain(r => r.Contains("Nexo.Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NexoTransportGrpc_MustNotReference_NexoOrchestration()
    {
        var references = ReadProjectReferences("Nexo.Transport.Grpc/Nexo.Transport.Grpc.csproj");
        references.Should().NotContain(r => r.Contains("Nexo.Orchestration", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NexoTransportGrpcServer_MustNotReference_NexoCoreDomain()
    {
        var references = ReadProjectReferences("Nexo.Transport.Grpc.Server/Nexo.Transport.Grpc.Server.csproj");
        references.Should().NotContain(r => r.Contains("Nexo.Core.Domain", StringComparison.OrdinalIgnoreCase));
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
