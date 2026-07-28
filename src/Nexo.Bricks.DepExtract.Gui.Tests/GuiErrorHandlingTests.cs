using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Nexo.Bricks.DepExtract.Gui;
using Xunit;

namespace Nexo.Bricks.DepExtract.Gui.Tests;

public sealed class GuiErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly string _fixturesRoot;

    public GuiErrorHandlingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _fixturesRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    }

    [Fact]
    public async Task Status_includes_detail_fields()
    {
        var resp = await _client.GetAsync("/api/status");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("docker", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("ollama", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("dockerDetail", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("extractReady", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("adaptReady", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("parserUiUrl", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("compileGateDefault", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("preferScaffoldDefault", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Run_preferScaffold_on_T1_produces_adapter_without_ollama()
    {
        var prevRoot = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
        Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
        Environment.SetEnvironmentVariable("COMPILE_GATE", "0");
        try
        {
            var src = Path.Combine(_fixturesRoot, "stress", "T1_TrivialCounter");
            Directory.Exists(src).Should().BeTrue("T1 fixture must be copied to Gui.Tests output");
            var resp = await _client.PostAsJsonAsync("/api/run", new
            {
                srcDir = src,
                entries = new[] { "Counter.hpp" },
                preferScaffold = true,
                requireCompile = false,
                maxRetries = 1
            });
            resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var code = doc.RootElement.GetProperty("adapterCode").GetString();
            code.Should().NotBeNullOrWhiteSpace();
            code.Should().Contain("SimpleCounter");
            code.Should().Contain("CustomEventReader");
            code.Should().Contain("Deterministic scaffold");
            // The generic profile engine reports the scaffold path as the Templated
            // strategy; the old brick said "Scaffolded". Same path, new vocabulary.
            doc.RootElement.GetProperty("summary").GetString().Should().Contain("Templated");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", prevRoot);
            Environment.SetEnvironmentVariable("COMPILE_GATE", null);
        }
    }

    [Fact]
    public async Task Run_returns_400_when_srcDir_outside_workspace_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "gui-ws-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORKSPACE_ROOT", root);
        try
        {
            var outside = Path.Combine(_fixturesRoot, "stress", "T1_TrivialCounter");
            var resp = await _client.PostAsJsonAsync("/api/run", new
            {
                srcDir = outside,
                entries = new[] { "Counter.hpp" },
                manifestOnly = true
            });
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("error").GetString().Should().Be("invalid_input");
            doc.RootElement.GetProperty("detail").GetString().Should().Contain("WORKSPACE_ROOT");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Run_returns_structured_400_for_traversal_entry()
    {
        var basic = Path.Combine(_fixturesRoot, "stress", "T1_TrivialCounter");
        var resp = await _client.PostAsJsonAsync("/api/run", new
        {
            srcDir = basic,
            entries = new[] { "../T2_FixedRecordLog/FixedLog.hpp" },
            manifestOnly = true
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetString().Should().Be("invalid_input");
        doc.RootElement.GetProperty("detail").GetString().Should().Contain("..");
    }

    [Fact]
    public async Task Run_returns_404_for_missing_entry()
    {
        var basic = Path.Combine(_fixturesRoot, "stress", "T1_TrivialCounter");
        var resp = await _client.PostAsJsonAsync("/api/run", new
        {
            srcDir = basic,
            entries = new[] { "Missing.hpp" },
            manifestOnly = true
        });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetString().Should().Be("not_found");
    }

    [Fact]
    public async Task Run_complex_T11_manifestOnly_via_http()
    {
        var src = Path.Combine(_fixturesRoot, "stress", "T11_MultiRootIncludes");
        if (!Directory.Exists(src))
            return; // fixtures synced by test runner; skip if not present yet

        var resp = await _client.PostAsJsonAsync("/api/run", new
        {
            srcDir = src,
            entries = new[] { "src/MultiRootLog.hpp", "src/MultiRootLog.cpp" },
            includeDirs = new[] { "include", "vendor/byteio" },
            manifestOnly = true
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        if (doc.RootElement.TryGetProperty("adapterCode", out var ac))
            ac.ValueKind.Should().BeOneOf(JsonValueKind.Null, JsonValueKind.Undefined);
        doc.RootElement.GetProperty("lower").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Run_returns_400_for_bad_define_format()
    {
        var basic = Path.Combine(_fixturesRoot, "stress", "T1_TrivialCounter");
        var resp = await _client.PostAsJsonAsync("/api/run", new
        {
            srcDir = basic,
            entries = new[] { "Counter.hpp" },
            defines = new[] { "NOT_A_VALID_DEFINE" },
            manifestOnly = true
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetString().Should().Be("invalid_input");
    }

    [Fact]
    public async Task Run_extractOnly_T13_returns_duplicate_without_adapter()
    {
        var src = Path.Combine(_fixturesRoot, "stress", "T13_GuiWarningFacade");
        if (!Directory.Exists(src)) return;

        var resp = await _client.PostAsJsonAsync("/api/run", new
        {
            srcDir = src,
            entries = new[] { "WarningFacade.hpp", "WarningFacade.cpp" },
            extractOnly = true
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var dup = doc.RootElement.GetProperty("duplicatePath").GetString();
        dup.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(dup!).Should().BeTrue();
        if (doc.RootElement.TryGetProperty("adapterCode", out var ac))
            ac.ValueKind.Should().BeOneOf(JsonValueKind.Null, JsonValueKind.Undefined);
    }

    [Fact]
    public async Task Run_complex_T14_nested_relative_manifestOnly()
    {
        var src = Path.Combine(_fixturesRoot, "stress", "T14_NestedRelativeGraph");
        if (!Directory.Exists(src)) return;

        var resp = await _client.PostAsJsonAsync("/api/run", new
        {
            srcDir = src,
            entries = new[] { "engine/core/GraphReader.hpp", "engine/core/GraphReader.cpp" },
            manifestOnly = true
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var lower = doc.RootElement.GetProperty("lower").EnumerateArray()
            .Select(e => e.GetString()!.Replace('\\', '/')).ToArray();
        lower.Should().Contain(p => p.Contains("PluginBridge.hpp"));
        lower.Should().Contain(p => p.Contains("Endian.hpp"));
    }

    [Fact]
    public async Task Install_writes_adapted_reader_without_rebuild()
    {
        var root = Path.Combine(Path.GetTempPath(), "install-ws-" + Guid.NewGuid());
        var poco = Path.Combine(root, "poco");
        Directory.CreateDirectory(Path.Combine(poco, "poco"));
        Directory.CreateDirectory(Path.Combine(poco, "common"));
        File.WriteAllText(Path.Combine(poco, "poco", "main.cpp"), "// stub\n");
        File.WriteAllText(Path.Combine(poco, "common", "adapted_reader.hpp"), "// old\n");
        Environment.SetEnvironmentVariable("WORKSPACE_ROOT", root);
        Environment.SetEnvironmentVariable("POCO_CONTEXT", poco);
        Environment.SetEnvironmentVariable("EVTX_HOST_PORT", "18080");
        try
        {
            var code = "#pragma once\n// drafted\n";
            var resp = await _client.PostAsJsonAsync("/api/install", new
            {
                adapterCode = code,
                pocoContext = poco,
                rebuildImage = false
            });
            resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("parserUiUrl").GetString().Should().Contain("18080");
            File.ReadAllText(Path.Combine(poco, "common", "adapted_reader.hpp")).Should().Contain("drafted");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
            Environment.SetEnvironmentVariable("POCO_CONTEXT", null);
            Environment.SetEnvironmentVariable("EVTX_HOST_PORT", null);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Plan_revise_then_implement_on_T1_scaffold()
    {
        var prevRoot = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
        Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
        Environment.SetEnvironmentVariable("COMPILE_GATE", "0");
        try
        {
            var src = Path.Combine(_fixturesRoot, "stress", "T1_TrivialCounter");
            Directory.Exists(src).Should().BeTrue();

            var planResp = await _client.PostAsJsonAsync("/api/plan", new
            {
                srcDir = src,
                entries = new[] { "Counter.hpp" },
                preferScaffold = true,
                requireCompile = false
            });
            planResp.StatusCode.Should().Be(HttpStatusCode.OK, await planResp.Content.ReadAsStringAsync());
            using var planDoc = JsonDocument.Parse(await planResp.Content.ReadAsStringAsync());
            var planId = planDoc.RootElement.GetProperty("planId").GetString();
            planId.Should().NotBeNullOrWhiteSpace();
            planDoc.RootElement.GetProperty("diagramMermaid").GetString().Should().Contain("flowchart");
            planDoc.RootElement.GetProperty("adaptationSummary").GetString().Should().Contain("CustomEventReader");
            planDoc.RootElement.GetProperty("strategy").GetString().Should().Be("scaffold");

            planDoc.RootElement.GetProperty("risks").GetArrayLength().Should().BeGreaterThan(0);
            planDoc.RootElement.GetProperty("constraints").GetArrayLength().Should().BeGreaterThan(0);
            planDoc.RootElement.GetProperty("exportPath").GetString().Should().Contain(planId);

            var reviseResp = await _client.PostAsJsonAsync("/api/plan/revise", new
            {
                planId,
                feedback = (string?)null,
                must = new[] { "Expose the counter value in Event.fields[value]." },
                mustNot = new[] { "Invent a proprietary seek API" }
            });
            reviseResp.StatusCode.Should().Be(HttpStatusCode.OK, await reviseResp.Content.ReadAsStringAsync());
            using var revDoc = JsonDocument.Parse(await reviseResp.Content.ReadAsStringAsync());
            revDoc.RootElement.GetProperty("status").GetString().Should().Be("revised");
            var constraintText = string.Join(" ", revDoc.RootElement.GetProperty("constraints").EnumerateArray()
                .Select(e => e.GetProperty("text").GetString()));
            constraintText.Should().Contain("fields[value]");
            constraintText.Should().Contain("seek");

            // Persisted plan should reload by id
            var getResp = await _client.GetAsync($"/api/plan/{planId}");
            getResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var exportResp = await _client.GetAsync($"/api/plan/{planId}/export.md");
            exportResp.StatusCode.Should().Be(HttpStatusCode.OK);
            (await exportResp.Content.ReadAsStringAsync()).Should().Contain("## Constraints");

            var implResp = await _client.PostAsJsonAsync("/api/plan/implement", new
            {
                planId,
                preferScaffold = true,
                requireCompile = false
            });
            implResp.StatusCode.Should().Be(HttpStatusCode.OK, await implResp.Content.ReadAsStringAsync());
            using var implDoc = JsonDocument.Parse(await implResp.Content.ReadAsStringAsync());
            implDoc.RootElement.GetProperty("adapterCode").GetString().Should().Contain("CustomEventReader");
            implDoc.RootElement.GetProperty("planId").GetString().Should().Be(planId);

            File.Exists(Path.Combine(PlanSessionStore.ResolvePlansDirectory(), planId + ".json")).Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", prevRoot);
            Environment.SetEnvironmentVariable("COMPILE_GATE", null);
        }
    }

    [Fact]
    public async Task Workspace_requires_workspace_root()
    {
        Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
        var resp = await _client.GetAsync("/api/workspace");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Workspace_lists_entries_under_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "ws-tree-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "proprietary"));
        File.WriteAllText(Path.Combine(root, "proprietary", "a.hpp"), "#pragma once\n");
        Environment.SetEnvironmentVariable("WORKSPACE_ROOT", root);
        try
        {
            var resp = await _client.GetAsync("/api/workspace?path=proprietary&depth=1");
            resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("workspaceRoot").GetString().Should().Be(Path.GetFullPath(root));
            var names = doc.RootElement.GetProperty("entries").EnumerateArray()
                .Select(e => e.GetProperty("name").GetString()).ToArray();
            names.Should().Contain("a.hpp");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
