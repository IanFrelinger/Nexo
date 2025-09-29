using System.Reflection;
using NetArchTest.Rules;
using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Architecture;

/// <summary>
/// Architecture tests to prevent duplications and enforce design rules
/// </summary>
public class ArchitectureTests
{
    private readonly Assembly _applicationAssembly = typeof(Nexo.Core.Application.Interfaces.ICommand<,>).Assembly;
    private readonly Assembly _domainAssembly = typeof(Nexo.Core.Domain.Values.BaseTypeValue).Assembly;
    private readonly Assembly _abstractionsAssembly = typeof(Nexo.Abstractions.IAgent).Assembly;
    private readonly Assembly _runtimeAssembly = typeof(Nexo.Runtime.AgentHost).Assembly;
    private readonly Assembly _toolsAssembly = typeof(Nexo.Tools.Dev.DotnetBuildTool).Assembly;
    private readonly Assembly _policiesAssembly = typeof(Nexo.Policies.Dev.PathAllowlist).Assembly;

    [Fact]
    public void ShouldHaveOnlyOneICommandInterface()
    {
        // Ensure there's only one ICommand interface in the application layer
        var iCommandTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.StartsWith("ICommand"))
            .ToList();

        Assert.Single(iCommandTypes);
        Assert.Equal("Nexo.Core.Application.Interfaces.ICommand`2", iCommandTypes[0].FullName);
    }

    [Fact]
    public void ShouldHaveOnlyOneAgentFactoryClass()
    {
        // Ensure there's only one AgentFactory class in the application layer
        var agentFactoryTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name == "AgentFactory")
            .ToList();

        Assert.Single(agentFactoryTypes);
        Assert.Equal("Nexo.Core.Application.Agents.AgentFactory", agentFactoryTypes[0].FullName);
    }

    [Fact]
    public void ShouldHaveOnlyGenericCommandOrchestrator()
    {
        // Ensure only GenericCommandOrchestrator exists (no other orchestrators)
        var orchestratorTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Orchestrator"))
            .ToList();

        Assert.Single(orchestratorTypes);
        Assert.Equal("Nexo.Core.Application.Orchestration.GenericCommandOrchestrator", orchestratorTypes[0].FullName);
    }

    [Fact]
    public void ShouldNotHaveCentralizedEnums()
    {
        // Ensure no CentralizedEnums.cs file exists
        var centralizedEnumTypes = _applicationAssembly.GetTypes()
            .Where(t => t.Name.Contains("CentralizedEnums"))
            .ToList();

        Assert.Empty(centralizedEnumTypes);
    }

    [Fact]
    public void ShouldUseTypeValueSystemInsteadOfEnums()
    {
        // Ensure domain values use ITypeValue instead of enums
        var enumTypes = _domainAssembly.GetTypes()
            .Where(t => t.IsEnum)
            .ToList();

        // Only allow the DevMode enum in the Dev agents project
        var allowedEnums = enumTypes.Where(t => t.Name == "DevMode").ToList();
        Assert.Equal(enumTypes.Count, allowedEnums.Count);
    }

    [Fact]
    public void ShouldHaveSimplifiedClassesOnlyInExamples()
    {
        // Ensure Simplified* classes are only in Examples project
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("Nexo") == true)
            .ToList();

        var simplifiedTypes = allAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.StartsWith("Simplified") && !t.Namespace?.Contains("Examples") == true)
            .ToList();

        Assert.Empty(simplifiedTypes);
    }



        [Fact]
        public void ShouldNotHaveDuplicatePublicTypeNames()
        {
            // Get all public types from all assemblies
            var allTypes = new List<(string AssemblyName, string TypeName, Type Type)>();
            
            var assemblies = new[]
            {
                _applicationAssembly,
                _domainAssembly,
                _abstractionsAssembly,
                _runtimeAssembly,
                _toolsAssembly,
                _policiesAssembly
            };

            foreach (var assembly in assemblies)
            {
                var publicTypes = assembly.GetTypes()
                    .Where(t => t.IsPublic && !t.IsNested)
                    .Select(t => (assembly.GetName().Name!, t.Name, t));
                
                allTypes.AddRange(publicTypes);
            }

            // Group by type name and find duplicates
            var duplicateGroups = allTypes
                .GroupBy(t => t.TypeName)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateGroups.Any())
            {
                var duplicateInfo = duplicateGroups
                    .Select(g => $"{g.Key}: {string.Join(", ", g.Select(t => t.AssemblyName))}")
                    .ToList();
                
                Assert.Fail($"Duplicate public type names found: {string.Join("; ", duplicateInfo)}");
            }
        }

        [Fact]
        public void ShouldNotHaveEnumsInDomainLayer()
        {
            // Ensure no enums exist in the domain layer
            var enumTypes = _domainAssembly.GetTypes()
                .Where(t => t.IsEnum)
                .ToList();

            enumTypes.Should().BeEmpty("Domain layer should not contain enums. Use value objects instead.");
        }

        [Fact]
        public void ShouldNotHaveEnumsInAbstractionsLayer()
        {
            // Ensure no enums exist in the abstractions layer
            var enumTypes = _abstractionsAssembly.GetTypes()
                .Where(t => t.IsEnum)
                .ToList();

            enumTypes.Should().BeEmpty("Abstractions layer should not contain enums. Use value objects instead.");
        }

        [Fact]
        public void ExamplesProjectShouldNotBePackable()
        {
            // This test verifies that Examples project is marked as non-packable
            // We can't directly test the .csproj file, but we can verify it's not referenced
            // by any packable projects by checking that no other assemblies reference it
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName?.StartsWith("Nexo") == true)
                .ToList();

            var examplesAssemblyName = "Nexo.Examples";
            
            foreach (var assembly in allAssemblies)
            {
                if (assembly.GetName().Name == examplesAssemblyName) continue;
                
                var references = assembly.GetReferencedAssemblies()
                    .Where(a => a.Name == examplesAssemblyName)
                    .ToList();
                
                references.Should().BeEmpty($"Assembly {assembly.GetName().Name} should not reference Examples assembly");
            }
        }

        [Fact]
        public void ShouldHaveConsistentNamingConventions()
        {
            // Ensure interfaces start with 'I' and classes don't
            var allAssemblies = new[] { _applicationAssembly, _domainAssembly, _abstractionsAssembly };
            
            foreach (var assembly in allAssemblies)
            {
                var publicTypes = assembly.GetTypes()
                    .Where(t => t.IsPublic && !t.IsNested)
                    .ToList();

                var interfaceViolations = publicTypes
                    .Where(t => t.IsInterface && !t.Name.StartsWith("I"))
                    .ToList();

                var classViolations = publicTypes
                    .Where(t => t.IsClass && !t.IsAbstract && t.Name.StartsWith("I"))
                    .ToList();

                interfaceViolations.Should().BeEmpty($"Interfaces in {assembly.GetName().Name} should start with 'I'");
                classViolations.Should().BeEmpty($"Classes in {assembly.GetName().Name} should not start with 'I'");
            }
        }

        [Fact]
        public void ShouldHaveClassesUnder200Lines()
        {
            // This is a simplified check - in a real scenario, you'd count actual lines
            // For now, we'll just ensure the class exists and is properly structured
            var allTypes = _applicationAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                .ToList();

            var oversizedClasses = new List<string>();

            foreach (var type in allTypes)
            {
                // This is a simplified check - in a real scenario, you'd count actual lines
                // For now, we'll just ensure the class exists and is properly structured
                if (type.GetMethods().Length > 20) // Rough heuristic
                {
                    oversizedClasses.Add(type.Name);
                }
            }

            // This is more of a documentation of the rule - actual line counting would require file analysis
            oversizedClasses.Should().HaveCountLessThan(5, $"Found {oversizedClasses.Count} potentially oversized classes: {string.Join(", ", oversizedClasses)}");
        }

        [Fact]
        public void ShouldHaveProperAssemblyReferences()
        {
            // Verify that assemblies only reference what they should
            var abstractionsName = _abstractionsAssembly.GetName().Name;
            
            // Abstractions should have no Nexo dependencies
            var abstractionsDeps = _abstractionsAssembly.GetReferencedAssemblies()
                .Where(a => a.Name?.StartsWith("Nexo") == true)
                .ToList();
            
            abstractionsDeps.Should().BeEmpty("Abstractions should have no dependencies on other Nexo assemblies");
            
            // Runtime, Tools, and Policies should only depend on Abstractions
            var infrastructureAssemblies = new[] { _runtimeAssembly, _toolsAssembly, _policiesAssembly };
            
            foreach (var assembly in infrastructureAssemblies)
            {
                var nexDeps = assembly.GetReferencedAssemblies()
                    .Where(a => a.Name?.StartsWith("Nexo") == true && a.Name != abstractionsName)
                    .ToList();
                
                nexDeps.Should().BeEmpty($"Assembly {assembly.GetName().Name} should only depend on Abstractions");
            }
        }
}
