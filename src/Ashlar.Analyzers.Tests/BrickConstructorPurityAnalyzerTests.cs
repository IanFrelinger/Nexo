using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using static Ashlar.Analyzers.Tests.TrustLoopAnalyzerHarness;

namespace Ashlar.Analyzers.Tests;

/// <summary>
/// ASHLAR0003/ASHLAR0004 triads: constructor side effects must fire, ExecuteAsync-side effects and
/// pure constructors must not, and indirection the analyzer cannot resolve must stay silent.
/// </summary>
public sealed class BrickConstructorPurityAnalyzerTests
{
    private static Task<IReadOnlyList<Diagnostic>> RunAsync(string source)
        => AnalyzeAsync(
            source,
            new BrickConstructorPurityAnalyzer(),
            // Anchor forwarded-type assemblies the samples reference (Dns lives in
            // System.Net.NameResolution, HttpClient in System.Net.Http).
            typeof(System.Net.Dns),
            typeof(System.Net.Http.HttpClient),
            typeof(System.Net.Sockets.Socket));

    // ---------------------------------------------------------- must fire

    [Fact]
    public async Task Ctor_file_read_is_flagged_as_constructor_io()
    {
        var diagnostics = await RunAsync(BrickSource(
            ctorBody: """var text = System.IO.File.ReadAllText("config.json");"""));

        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(BrickConstructorPurityAnalyzer.ConstructorIoId);
        diagnostic.GetMessage().Should().Contain("SampleBrick").And.Contain("File.ReadAllText")
            .And.Contain("ExecuteAsync", "the message must name the fix target");
    }

    [Fact]
    public async Task Field_initializer_stream_creation_is_flagged_as_constructor_io()
    {
        var diagnostics = await RunAsync(BrickSource(
            extraMembers: """private readonly System.IO.StreamReader _reader = new System.IO.StreamReader("data.txt");"""));

        diagnostics.Should().ContainSingle().Which.Id
            .Should().Be(BrickConstructorPurityAnalyzer.ConstructorIoId);
    }

    [Fact]
    public async Task Ctor_http_client_creation_is_flagged_as_constructor_network()
    {
        var diagnostics = await RunAsync(BrickSource(
            ctorBody: "var client = new System.Net.Http.HttpClient();"));

        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(BrickConstructorPurityAnalyzer.ConstructorNetworkId);
        diagnostic.GetMessage().Should().Contain("network");
    }

    [Fact]
    public async Task Ctor_dns_lookup_is_flagged_as_constructor_network()
    {
        var diagnostics = await RunAsync(BrickSource(
            ctorBody: "var host = System.Net.Dns.GetHostName();"));

        diagnostics.Should().ContainSingle().Which.Id
            .Should().Be(BrickConstructorPurityAnalyzer.ConstructorNetworkId);
    }

    // ------------------------------------------------------- must not fire

    [Fact]
    public async Task Same_io_inside_ExecuteAsync_is_not_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(
            body: """var text = System.IO.File.ReadAllText("config.json"); output.Set("t", text);"""));

        diagnostics.Should().BeEmpty("ExecuteAsync is where I/O belongs — budgeted and sandboxed");
    }

    [Fact]
    public async Task Pure_constructor_is_not_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(ctorBody: "var x = 1 + 1;"));

        diagnostics.Should().BeEmpty();
    }

    // -------------------------------------- deliberately unresolvable: silent

    [Fact]
    public async Task Io_behind_a_helper_method_is_left_alone()
    {
        var diagnostics = await RunAsync(BrickSource(
            ctorBody: "LoadConfig();",
            extraMembers: """
                private static string LoadConfig()
                    => System.IO.File.ReadAllText("config.json");
                """));

        diagnostics.Should().BeEmpty(
            "the analyzer does not chase call graphs; a helper it cannot see into is not judged");
    }

    [Fact]
    public async Task Io_in_a_non_brick_type_constructor_is_left_alone()
    {
        const string source = """
            public sealed class NotABrick
            {
                public NotABrick()
                {
                    var text = System.IO.File.ReadAllText("config.json");
                }
            }
            """;

        (await RunAsync(source)).Should().BeEmpty("the rule is scoped to brick construction");
    }
}
