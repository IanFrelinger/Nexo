using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

public class ArchitectureRules
{
    private static readonly Assembly App  = typeof(Nexo.Core.Application.Host.ServiceHost).Assembly;
    private static readonly Assembly Dom  = typeof(Nexo.Core.Domain.Values.AgentStatus).Assembly;
    private static readonly Assembly Shd  = typeof(Nexo.Shared.Values.BaseTypeValue).Assembly;
    private static readonly Assembly? Ex  = App.GetType("Nexo.Examples.Simplified.SimplifiedCommand")?.Assembly;

    [Fact]
    public void OnlyOne_ICommand_Interface_In_Solution()
    {
        var types = Types.InAssemblies(new[] { App, Dom, Shd })
            .That().HaveName("ICommand")
            .GetTypes();
        types.Length.Should().Be(1, "ICommand must have a single source of truth");
    }

    [Fact]
    public void Only_GenericCommandOrchestrator_Is_Allowed()
    {
        var bad = Types.InAssembly(App)
            .That().HaveNameEndingWith("Orchestrator")
            .And().DoNotHaveName("GenericCommandOrchestrator")
            .GetTypes();

        bad.Should().BeEmpty("per-domain orchestrators cause duplication");
    }

    [Fact]
    public void No_Enums_In_Domain_Or_Shared()
    {
        var enums = Types.InAssemblies(new[] { Dom, Shd })
            .That().AreEnums()
            .GetTypes();

        enums.Should().BeEmpty("use BaseTypeValue instead of enums");
    }

    [Fact]
    public void Only_One_AgentFactory_Type()
    {
        var factories = Types.InAssemblies(new[] { App, Dom, Shd })
            .That().HaveName("AgentFactory")
            .GetTypes();
        factories.Length.Should().Be(1, "AgentFactory must be single");
    }

    [Fact]
    public void Simplified_Classes_Internal_And_In_Examples_Only()
    {
        if (Ex is null) return;
        var notInternal = Types.InAssembly(Ex)
            .That().HaveNameStartingWith("Simplified")
            .And().ArePublic()
            .GetTypes();

        notInternal.Should().BeEmpty("examples must be internal");

        var leaked = Types.InAssemblies(new[] { App, Dom, Shd })
            .That().HaveNameStartingWith("Simplified")
            .GetTypes();

        leaked.Should().BeEmpty("Simplified* must not appear outside Nexo.Examples");
    }
}
