using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Nexo.Bricks.DepExtract.Gui.Tests;

/// <summary>
/// Drives the GUI through its actual HTTP surface (in-process TestServer, real
/// JSON serialization, real endpoint code) against the ten example C++ parsers
/// at escalating complexity (Fixtures/stress/T1..T10) — the same fixtures the
/// brick-level stress test used, but this time proving the front end's own
/// wiring works, not just the bricks it calls. A brick-level bug and a GUI
/// wiring bug (bad JSON shape, wrong dictionary key, swallowed exception) are
/// different failure classes; this suite is the one that would catch the latter.
/// </summary>
public sealed class GuiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly string _fixturesRoot;

    public GuiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _fixturesRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    }

    [Fact]
    public async Task Root_serves_the_static_ui()
    {
        var resp = await _client.GetAsync("/");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("srcDir").And.Contain("Run");
    }

    [Fact]
    public async Task Status_reports_docker_and_ollama()
    {
        var resp = await _client.GetAsync("/api/status");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await resp.Content.ReadFromJsonAsync<StatusResponse>();
        status.Should().NotBeNull();
        status!.Docker.Should().BeTrue("the WSL environment this suite runs in has Docker installed");
        status.Ollama.Should().BeTrue("the WSL environment this suite runs in has Ollama installed with a model pulled");
    }

    [Fact]
    public async Task Run_rejects_missing_srcDir()
    {
        var resp = await _client.PostAsJsonAsync("/api/run", new RunRequestDto("/does/not/exist", ["a.hpp"], null, null, null, false, false, null, null));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Run_rejects_no_entries()
    {
        var resp = await _client.PostAsJsonAsync("/api/run", new RunRequestDto(_fixturesRoot, [], null, null, null, false, false, null, null));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Run_manifestOnly_skips_adaptation()
    {
        var req = new RunRequestDto(
            Path.Combine(_fixturesRoot, "stress", "T4_VariableLengthTLV"),
            ["TlvEventStream.hpp", "TlvEventStream.cpp"],
            null, null, null, false, ManifestOnly: true, null, null);

        var resp = await _client.PostAsJsonAsync("/api/run", req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<RunResponseDto>();
        result!.AdapterCode.Should().BeNull("manifestOnly should skip the adaptation stage entirely");
        result.ManifestText.Should().Contain("TlvEventStream.hpp");
    }

    public static IEnumerable<object[]> Tiers()
    {
        yield return new object[] { "T1_TrivialCounter", new[] { "Counter.hpp" } };
        yield return new object[] { "T2_FixedRecordLog", new[] { "FixedLog.hpp", "FixedLog.cpp" } };
        yield return new object[] { "T3_HeaderedFixedLog", new[] { "TelemetryStream.hpp", "TelemetryStream.cpp" } };
        yield return new object[] { "T4_VariableLengthTLV", new[] { "TlvEventStream.hpp", "TlvEventStream.cpp" } };
        yield return new object[] { "T5_MultiClassCursor", new[] { "SessionLog.hpp", "SessionLog.cpp" } };
        yield return new object[] { "T6_AbstractBaseHierarchy", new[] { "RecordSource.hpp", "RecordSource.cpp" } };
        yield return new object[] { "T7_StatefulMultiPass", new[] { "ArchiveReader.hpp", "ArchiveReader.cpp" } };
        yield return new object[] { "T8_TemplatedRecordType", new[] { "TypedEventLog.hpp" } };
        yield return new object[] { "T9_ChunkedCompressed", new[] { "ChunkedTraceLog.hpp", "ChunkedTraceLog.cpp" } };
        yield return new object[] { "T10_IndexedMultiSection", new[] { "FlightRecorderArchive.hpp", "FlightRecorderArchive.cpp" } };
    }

    [Theory]
    [MemberData(nameof(Tiers))]
    public async Task Run_end_to_end_via_http_for_tier(string dir, string[] entries)
    {
        var req = new RunRequestDto(Path.Combine(_fixturesRoot, "stress", dir), entries, null, null, null, false, false, null, null);

        var resp = await _client.PostAsJsonAsync("/api/run", req);

        resp.StatusCode.Should().Be(HttpStatusCode.OK, $"[{dir}] the pipeline should complete through the real HTTP endpoint");
        var result = await resp.Content.ReadFromJsonAsync<RunResponseDto>();
        result.Should().NotBeNull();
        result!.AdapterCode.Should().NotBeNullOrWhiteSpace($"[{dir}] adaptation should produce a draft");
        result.AdapterCode.Should().Contain("CustomEventReader", $"[{dir}] the draft must target the real seam contract");
        result.SourceApiCoverage.Should().NotBeNull();
        result.LooksLikeTemplateEcho.Should().NotBeNull();
        result.PossibleHallucinations.Should().NotBeNull();
        result.DuplicatePath.Should().NotBeNullOrEmpty($"[{dir}] extraction should have produced a duplicate");
        Directory.Exists(result.DuplicatePath).Should().BeTrue($"[{dir}] the duplicate the response points at should actually exist on disk");
    }

    private sealed record StatusResponse(bool Docker, bool Ollama);

    private sealed record RunRequestDto(
        string SrcDir, string[] Entries, string? ScanDir, string[]? IncludeDirs, string[]? Defines,
        bool IncludeUpper, bool ManifestOnly, string? Model, int? MaxRetries);

    private sealed record RunResponseDto(
        string[] Lower, string[] Upper, string[] External, string ManifestText, string? DuplicatePath,
        string? AdapterCode, string? AdapterOutputPath, double? SourceApiCoverage,
        bool? LooksLikeTemplateEcho, string[]? PossibleHallucinations, string Summary);
}
