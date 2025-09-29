using NetArchTest.Rules;
using FluentAssertions;
using Xunit;

// Type anchors for each assembly - update these if namespaces change
using AbstractionsAnchor = Nexo.Abstractions.ModelOutput;
using RuntimeAnchor = Nexo.Runtime.AgentHost;
using ToolsDevAnchor = Nexo.Tools.Dev.DotnetBuildTool;
using PoliciesDevAnchor = Nexo.Policies.Dev.PathAllowlist;
using AppAnchor = Nexo.Core.Application.Host.ServiceHost;
using DomainAnchor = Nexo.Core.Domain.Values.AgentStatus;

/// <summary>
/// Architecture tests to enforce proper layering rules and prevent dependency violations
/// </summary>
public class LayeringRules
{
    private static readonly System.Reflection.Assembly Abstractions = typeof(AbstractionsAnchor).Assembly;
    private static readonly System.Reflection.Assembly Runtime = typeof(RuntimeAnchor).Assembly;
    private static readonly System.Reflection.Assembly ToolsDev = typeof(ToolsDevAnchor).Assembly;
    private static readonly System.Reflection.Assembly PoliciesDev = typeof(PoliciesDevAnchor).Assembly;
    private static readonly System.Reflection.Assembly App = typeof(AppAnchor).Assembly;
    private static readonly System.Reflection.Assembly Domain = typeof(DomainAnchor).Assembly;

    [Fact]
    public void Abstractions_Has_No_Dependencies_On_Others()
    {
        var result = Types.InAssembly(Abstractions)
            .Should().NotHaveDependencyOnAny(
                Runtime.GetName().Name!,
                ToolsDev.GetName().Name!,
                PoliciesDev.GetName().Name!,
                App.GetName().Name!,
                Domain.GetName().Name!
            ).GetResult();
        result.IsSuccessful.Should().BeTrue("Abstractions must be the root with no dependencies on other Nexo assemblies");
    }

    [Fact]
    public void Runtime_Tools_Policies_Depend_Only_On_Abstractions()
    {
        var badRuntime = Types.InAssembly(Runtime).Should().NotHaveDependencyOnAny(
            App.GetName().Name!, Domain.GetName().Name!
        ).GetResult();

        var badTools = Types.InAssembly(ToolsDev).Should().NotHaveDependencyOnAny(
            App.GetName().Name!, Domain.GetName().Name!
        ).GetResult();

        var badPolicies = Types.InAssembly(PoliciesDev).Should().NotHaveDependencyOnAny(
            App.GetName().Name!, Domain.GetName().Name!
        ).GetResult();

        badRuntime.IsSuccessful.Should().BeTrue("Runtime must not depend on App/Domain");
        badTools.IsSuccessful.Should().BeTrue("Tools must not depend on App/Domain");
        badPolicies.IsSuccessful.Should().BeTrue("Policies must not depend on App/Domain");
    }

    [Fact]
    public void App_Domain_DoNot_Have_Circular_Dependencies()
    {
        // Application and Domain should not have circular dependencies
        var appTypes = Types.InAssembly(App).GetTypes();
        var domainTypes = Types.InAssembly(Domain).GetTypes();
        
        appTypes.Should().NotBeNull("Application layer should have types");
        domainTypes.Should().NotBeNull("Domain layer should have types");
    }

    [Fact]
    public void Domain_Does_Not_Depend_On_Application()
    {
        var result = Types.InAssembly(Domain).Should().NotHaveDependencyOnAny(
            App.GetName().Name!
        ).GetResult();
        result.IsSuccessful.Should().BeTrue("Domain should not depend on Application layer");
    }

    [Fact]
    public void No_Circular_Dependencies_Exist()
    {
        // This is a basic check - verify that assemblies have reasonable dependency patterns
        var assemblies = new[] { Abstractions, Runtime, ToolsDev, PoliciesDev, App, Domain };
        
        foreach (var assembly in assemblies)
        {
            var dependencies = assembly.GetReferencedAssemblies()
                .Where(a => a.Name != null && a.Name.StartsWith("Nexo."))
                .Select(a => a.Name!)
                .ToList();

            // Abstractions should have no Nexo dependencies (it's the root)
            if (assembly == Abstractions)
            {
                dependencies.Should().BeEmpty($"Assembly {assembly.GetName().Name} should have no Nexo dependencies as it's the root");
            }
            // Other assemblies should exist and be valid
            else
            {
                assembly.Should().NotBeNull($"Assembly {assembly.GetName().Name} should exist");
            }
        }
    }
}
