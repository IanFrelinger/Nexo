using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Orchestration.Coordination;
using Nexo.Runtime.Barriers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

public sealed class BarrierAuditSinkArchitectureTests
{
    [Fact]
    public void IBarrierAuditSink_Implementations_MustResideInRuntime()
    {
        var sinkType = typeof(IBarrierAuditSink);

        var abstractionsImplementations = sinkType.Assembly.GetTypes()
            .Where(type => IsConcreteSink(type, sinkType))
            .ToList();
        abstractionsImplementations.Should().BeEmpty();

        var orchestrationImplementations = typeof(Orchestrator).Assembly.GetTypes()
            .Where(type => IsConcreteSink(type, sinkType))
            .ToList();
        orchestrationImplementations.Should().BeEmpty();

        var runtimeImplementations = typeof(StructuredBarrierAuditLog).Assembly.GetTypes()
            .Where(type => IsConcreteSink(type, sinkType))
            .ToList();

        runtimeImplementations.Should().Contain(type => type == typeof(NoOpBarrierAuditSink));
        runtimeImplementations.Should().Contain(type => type == typeof(StructuredLogBarrierAuditSink));
        runtimeImplementations.Should().Contain(type => type == typeof(FileBarrierAuditSink));
    }

    [Fact]
    public void FileBarrierAuditSink_MustNotBeReferencedByOrchestration()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../Nexo.Orchestration/Nexo.Orchestration.csproj"));
        File.Exists(projectPath).Should().BeTrue();

        var projectContents = File.ReadAllText(projectPath);
        projectContents.Should().NotContain("Nexo.Runtime", because: "orchestration should not directly depend on runtime sinks");

        var sourceRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../Nexo.Orchestration"));
        var allSourceFiles = Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories);
        foreach (var file in allSourceFiles)
        {
            File.ReadAllText(file).Should().NotContain(nameof(FileBarrierAuditSink));
        }
    }

    private static bool IsConcreteSink(Type candidate, Type sinkType)
        => sinkType.IsAssignableFrom(candidate)
           && candidate.IsClass
           && !candidate.IsAbstract;
}
