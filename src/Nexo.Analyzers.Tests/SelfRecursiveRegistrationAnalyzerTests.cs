using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Nexo.Analyzers.Tests.TrustLoopAnalyzerHarness;

namespace Nexo.Analyzers.Tests;

/// <summary>
/// NEXO0005 triad: a factory resolving its own service type must fire; factories resolving
/// other types must not; indirection the analyzer cannot resolve (method groups, typeof-based
/// registration) must stay silent by design.
/// </summary>
public sealed class SelfRecursiveRegistrationAnalyzerTests
{
    private static Task<IReadOnlyList<Diagnostic>> RunAsync(string registration)
        => AnalyzeAsync(
            Sample(registration),
            new SelfRecursiveRegistrationAnalyzer(),
            typeof(IServiceCollection),
            typeof(ServiceCollection));

    private static string Sample(string registration) => $$"""
        using System;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.DependencyInjection.Extensions;

        public interface IFoo { }
        public interface IBar { }
        public sealed class Foo : IFoo { public Foo() { } public Foo(IFoo inner) { } public Foo(IBar bar) { } }
        public sealed class Bar : IBar { }

        public static class Registrations
        {
            public static IFoo CreateFoo(IServiceProvider sp) => sp.GetRequiredService<IFoo>();

            public static void Register(IServiceCollection services)
            {
                {{registration}}
            }
        }
        """;

    // ---------------------------------------------------------- must fire

    [Fact]
    public async Task Factory_resolving_its_own_service_type_is_flagged()
    {
        var diagnostics = await RunAsync(
            "services.AddSingleton<IFoo>(sp => sp.GetRequiredService<IFoo>());");

        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(SelfRecursiveRegistrationAnalyzer.SelfRecursiveRegistrationId);
        diagnostic.GetMessage().Should().Contain("IFoo").And.Contain("Lazy",
            "the message must name the fix target");
    }

    [Fact]
    public async Task Factory_passing_its_own_service_type_into_the_implementation_is_flagged()
    {
        var diagnostics = await RunAsync(
            "services.AddSingleton<IFoo>(sp => new Foo(sp.GetRequiredService<IFoo>()));");

        diagnostics.Should().ContainSingle().Which.Id
            .Should().Be(SelfRecursiveRegistrationAnalyzer.SelfRecursiveRegistrationId);
    }

    [Fact]
    public async Task TryAdd_shapes_are_covered_too()
    {
        var diagnostics = await RunAsync(
            "services.TryAddSingleton<IFoo>(sp => sp.GetService<IFoo>()!);");

        diagnostics.Should().ContainSingle().Which.Id
            .Should().Be(SelfRecursiveRegistrationAnalyzer.SelfRecursiveRegistrationId);
    }

    // ------------------------------------------------------- must not fire

    [Fact]
    public async Task Factory_resolving_a_different_type_is_not_flagged()
    {
        var diagnostics = await RunAsync(
            "services.AddSingleton<IFoo>(sp => new Foo(sp.GetRequiredService<IBar>()));");

        diagnostics.Should().BeEmpty("resolving a different service is the normal factory pattern");
    }

    [Fact]
    public async Task Plain_type_registration_is_not_flagged()
    {
        var diagnostics = await RunAsync("services.AddSingleton<IFoo, Foo>();");

        diagnostics.Should().BeEmpty();
    }

    // -------------------------------------- deliberately unresolvable: silent

    [Fact]
    public async Task Method_group_factory_is_left_alone()
    {
        var diagnostics = await RunAsync(
            "services.AddSingleton<IFoo>(Registrations.CreateFoo);");

        diagnostics.Should().BeEmpty(
            "the resolution happens inside a method the analyzer would have to chase; it never guesses");
    }

    [Fact]
    public async Task Non_generic_typeof_registration_is_left_alone()
    {
        var diagnostics = await RunAsync(
            "services.AddSingleton(typeof(IFoo), sp => sp.GetRequiredService<IFoo>());");

        diagnostics.Should().BeEmpty(
            "without an explicit service type argument the registered type is not statically bound to the resolution");
    }
}
