using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexo.API.Endpoints;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Descriptors;
using Nexo.GameDomain.Mapping;
using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Scoping;
using Nexo.GameDomain.Session;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

[Trait("Category", "E2E")]
public sealed class ForgeEndpointsTests : IDisposable
{
    public ForgeEndpointsTests()
    {
        ResetStore();
    }

    public void Dispose()
    {
        ResetStore();
    }

    [Fact(Timeout = 15000)]
    public async Task CreateSession_ReturnsNewSession()
    {
        var request = new ForgeCreateSessionRequest("TestSession", MaxPlayers: 4);
        var handler = GetHandler("CreateSessionAsync");

        var result = await InvokeAsync(handler, request);

        var session = ExtractOkValue<SessionState>(result);
        session.Should().NotBeNull();
        session.Name.Should().Be("TestSession");
        session.MaxPlayers.Should().Be(4);
        session.SessionId.Should().NotBeNullOrWhiteSpace();
        session.GameRules.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task GetSession_AfterCreate_ReturnsState()
    {
        var createRequest = new ForgeCreateSessionRequest("GetTest", MaxPlayers: 6);
        await InvokeAsync(GetHandler("CreateSessionAsync"), createRequest);

        var handler = GetHandler("GetSessionAsync");
        var result = await InvokeAsync(handler);

        var session = ExtractOkValue<SessionState>(result);
        session.Should().NotBeNull();
        session.Name.Should().Be("GetTest");
        session.MaxPlayers.Should().Be(6);
    }

    [Fact(Timeout = 15000)]
    public async Task ExportSession_ReturnsJson()
    {
        var createRequest = new ForgeCreateSessionRequest("ExportTest");
        await InvokeAsync(GetHandler("CreateSessionAsync"), createRequest);

        var handler = GetHandler("ExportSessionAsync");
        var result = await InvokeAsync(handler);

        var export = ExtractOkValue<ForgeSessionExportResponse>(result);
        export.Should().NotBeNull();
        export.Json.Should().NotBeNullOrWhiteSpace();
        export.Json.Should().Contain("ExportTest");
    }

    [Fact(Timeout = 15000)]
    public async Task ImportSession_RestoresState()
    {
        var createRequest = new ForgeCreateSessionRequest("ImportSource", MaxPlayers: 10);
        await InvokeAsync(GetHandler("CreateSessionAsync"), createRequest);

        var exportResult = await InvokeAsync(GetHandler("ExportSessionAsync"));
        var exported = ExtractOkValue<ForgeSessionExportResponse>(exportResult);

        ResetStore();

        var importRequest = new ForgeSessionImportRequest(exported.Json);
        var handler = GetHandler("ImportSessionAsync");
        var result = await InvokeAsync(handler, importRequest);

        var session = ExtractOkValue<SessionState>(result);
        session.Should().NotBeNull();
        session.Name.Should().Be("ImportSource");
        session.MaxPlayers.Should().Be(10);
    }

    [Fact(Timeout = 15000)]
    public async Task AddSetting_AppearsInSession()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("SettingTest"));

        var setting = new ScopedSetting
        {
            SettingId = "gravity",
            Value = 9.8,
            Scope = new SettingScope { Type = SettingScopeType.Server },
            CreatedBy = "test",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var handler = GetHandler("ApplySettingAsync");
        var result = await InvokeAsync(handler, setting);

        var session = ExtractOkValue<SessionState>(result);
        session.ScopedSettings.Should().ContainSingle(s => s.SettingId == "gravity");
    }

    [Fact(Timeout = 15000)]
    public async Task RemoveSetting_RemovesFromSession()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("RemoveTest"));

        var setting = new ScopedSetting
        {
            SettingId = "time_scale",
            Value = 1.5,
            Scope = new SettingScope { Type = SettingScopeType.Server },
            CreatedBy = "test",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await InvokeAsync(GetHandler("ApplySettingAsync"), setting);

        var handler = GetHandler("RemoveSettingAsync");
        var result = await InvokeAsync(handler, "time_scale");

        var session = ExtractOkValue<SessionState>(result);
        session.ScopedSettings.Should().NotContain(s => s.SettingId == "time_scale");
    }

    [Fact(Timeout = 15000)]
    public async Task ResolveSettings_ReturnsCorrectValues()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("ResolveTest"));

        var serverSetting = new ScopedSetting
        {
            SettingId = "speed",
            Value = "slow",
            Scope = new SettingScope { Type = SettingScopeType.Server },
            CreatedBy = "test",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await InvokeAsync(GetHandler("ApplySettingAsync"), serverSetting);

        var playerSetting = new ScopedSetting
        {
            SettingId = "speed",
            Value = "fast",
            Scope = new SettingScope { Type = SettingScopeType.Player, Target = "player-1" },
            CreatedBy = "test",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await InvokeAsync(GetHandler("ApplySettingAsync"), playerSetting);

        var handler = GetHandler("ResolveSettingsAsync");
        var result = await InvokeAsync(handler, "player-1", null, null, null, null);

        var response = ExtractOkValue<ForgeResolvedSettingsResponse>(result);
        response.Should().NotBeNull();
        response.ResolvedSettings.Should().ContainKey("speed");
        response.ResolvedSettings["speed"].ToString().Should().Be("fast");
    }

    [Fact(Timeout = 15000)]
    public async Task RegisterMacro_AppearsInList()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("MacroTest"));

        var macro = new MacroDefinition
        {
            MacroId = "test-macro-1",
            DisplayName = "Test Macro",
            Description = "A test macro",
            Author = "tester"
        };

        await InvokeAsync(GetHandler("RegisterMacroAsync"), macro);

        var listHandler = GetHandler("ListMacrosAsync");
        var result = await InvokeAsync(listHandler);

        var macros = ExtractOkValue<IReadOnlyList<MacroDefinition>>(result);
        macros.Should().Contain(m => m.MacroId == "test-macro-1");
    }

    [Fact(Timeout = 15000)]
    public async Task ListAesthetics_Returns6Packs()
    {
        var handler = GetHandler("GetAestheticsAsync");
        var result = await InvokeAsync(handler);

        var packs = ExtractOkValue<IReadOnlyList<AestheticPack>>(result);
        packs.Should().HaveCount(6);
        packs.Select(p => p.Id).Should().Contain(new[] { "voxel", "low_poly", "pixel_art", "pbr", "wireframe", "sketch" });
        packs.Single(p => p.Id == "voxel").MapRenderingProfile.Should().Be(MapRenderingProfiles.VoxelGrid);
        packs.Single(p => p.Id == "pbr").MapRenderingProfile.Should().Be(MapRenderingProfiles.HeightfieldMesh);
    }

    [Fact(Timeout = 15000)]
    public async Task ApplyAesthetic_AddsScopedSetting()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("AestheticTest"));

        var request = new ForgeApplyAestheticRequest("voxel");
        var handler = GetHandler("ApplyAestheticAsync");
        var result = await InvokeAsync(handler, request);

        var session = ExtractOkValue<SessionState>(result);
        session.ScopedSettings.Should().Contain(s => s.SettingId == "aesthetic" && s.Value.ToString() == "voxel");
        session.AestheticPacks.Should().Contain(a => a.Id == "voxel");
    }

    [Fact(Timeout = 15000)]
    public async Task ApplyAesthetic_MapProfileOverride_StoredOnPack()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("MapStyleTest"));

        var request = new ForgeApplyAestheticRequest(
            "voxel",
            Scope: null,
            MapRenderingProfile: MapRenderingProfiles.VectorOverlay);
        var handler = GetHandler("ApplyAestheticAsync");
        var result = await InvokeAsync(handler, request);

        var session = ExtractOkValue<SessionState>(result);
        session.AestheticPacks.Should().ContainSingle(a =>
            a.Id == "voxel" && a.MapRenderingProfile == MapRenderingProfiles.VectorOverlay);
    }

    [Fact(Timeout = 15000)]
    public async Task ListAesthetics_BuiltInPacks_HaveExpectedMapRenderingProfiles()
    {
        var handler = GetHandler("GetAestheticsAsync");
        var result = await InvokeAsync(handler);

        var packs = ExtractOkValue<IReadOnlyList<AestheticPack>>(result);
        var byId = packs.ToDictionary(p => p.Id, StringComparer.Ordinal);

        byId["voxel"].MapRenderingProfile.Should().Be(MapRenderingProfiles.VoxelGrid);
        byId["low_poly"].MapRenderingProfile.Should().Be(MapRenderingProfiles.FlatShadedPolys);
        byId["pixel_art"].MapRenderingProfile.Should().Be(MapRenderingProfiles.OrthographicTile);
        byId["pbr"].MapRenderingProfile.Should().Be(MapRenderingProfiles.HeightfieldMesh);
        byId["wireframe"].MapRenderingProfile.Should().Be(MapRenderingProfiles.VectorOverlay);
        byId["sketch"].MapRenderingProfile.Should().Be(MapRenderingProfiles.VectorOverlay);
    }

    [Fact(Timeout = 15000)]
    public async Task ApplyAesthetic_NoOverride_UsesBuiltInMapRenderingProfile()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("DefaultProfileTest"));

        var request = new ForgeApplyAestheticRequest("voxel");
        var result = await InvokeAsync(GetHandler("ApplyAestheticAsync"), request);

        var session = ExtractOkValue<SessionState>(result);
        session.AestheticPacks.Should().ContainSingle(a =>
            a.Id == "voxel" && a.MapRenderingProfile == MapRenderingProfiles.VoxelGrid);
    }

    [Fact(Timeout = 15000)]
    public async Task ApplyAesthetic_ReapplyWithoutOverride_RestoresBuiltInMapProfile()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("ReapplyTest"));

        await InvokeAsync(GetHandler("ApplyAestheticAsync"),
            new ForgeApplyAestheticRequest("voxel", MapRenderingProfile: MapRenderingProfiles.VectorOverlay));

        var second = await InvokeAsync(GetHandler("ApplyAestheticAsync"),
            new ForgeApplyAestheticRequest("voxel"));

        var session = ExtractOkValue<SessionState>(second);
        session.AestheticPacks.Should().ContainSingle(a =>
            a.Id == "voxel" && a.MapRenderingProfile == MapRenderingProfiles.VoxelGrid);
    }

    [Fact(Timeout = 15000)]
    public async Task ExportImport_PreservesMapRenderingProfileOnAppliedPack()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("ExportMapProfile"));

        await InvokeAsync(GetHandler("ApplyAestheticAsync"),
            new ForgeApplyAestheticRequest("pbr", MapRenderingProfile: MapRenderingProfiles.VectorOverlay));

        var exportResult = await InvokeAsync(GetHandler("ExportSessionAsync"));
        var exported = ExtractOkValue<ForgeSessionExportResponse>(exportResult);

        ResetStore();

        var importResult = await InvokeAsync(GetHandler("ImportSessionAsync"),
            new ForgeSessionImportRequest(exported.Json));
        var imported = ExtractOkValue<SessionState>(importResult);

        imported.AestheticPacks.Should().ContainSingle(a =>
            a.Id == "pbr" && a.MapRenderingProfile == MapRenderingProfiles.VectorOverlay);
    }

    [Fact(Timeout = 15000)]
    public async Task ApplyAesthetic_UnknownPack_ReturnsBadRequest()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("UnknownPack"));

        var result = await InvokeAsync(GetHandler("ApplyAestheticAsync"),
            new ForgeApplyAestheticRequest("not-a-real-pack"));

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task ApplyAesthetic_WhitespaceOnlyOverride_KeepsBuiltInMapProfile()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("WhitespaceOverride"));

        var request = new ForgeApplyAestheticRequest("low_poly", MapRenderingProfile: "   \t  ");
        var result = await InvokeAsync(GetHandler("ApplyAestheticAsync"), request);

        var session = ExtractOkValue<SessionState>(result);
        session.AestheticPacks.Should().ContainSingle(a =>
            a.Id == "low_poly" && a.MapRenderingProfile == MapRenderingProfiles.FlatShadedPolys);
    }

    [Fact(Timeout = 15000)]
    public async Task GetMapAdaptationPlan_AfterApplyAesthetic_MatchesVoxelPipeline()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("MapPlanTest"));
        await InvokeAsync(GetHandler("ApplyAestheticAsync"),
            new ForgeApplyAestheticRequest("voxel"));

        var result = await InvokeAsync(GetHandler("GetMapAdaptationPlanAsync"));
        var plan = ExtractOkValue<MapAdaptationPlan>(result);
        plan.ActiveAestheticId.Should().Be("voxel");
        plan.EffectiveMapRenderingProfile.Should().Be(MapRenderingProfiles.VoxelGrid);
        plan.PipelineStages.Should().Contain("voxel_rasterize");
    }

    [Fact(Timeout = 15000)]
    public async Task GetMapAdaptationPlan_WithoutAesthetic_NotesNoSetting()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("NoAestheticPlan"));

        var result = await InvokeAsync(GetHandler("GetMapAdaptationPlanAsync"));
        var plan = ExtractOkValue<MapAdaptationPlan>(result);
        plan.ActiveAestheticId.Should().BeEmpty();
        plan.Notes.Should().NotBeNullOrEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task PreviewVectorMapSource_GeoJsonOk()
    {
        var root = Path.Combine(Path.GetTempPath(), "forge-geojson-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string rel = "x.geojson";
            await File.WriteAllTextAsync(
                Path.Combine(root, rel),
                """{"type":"FeatureCollection","features":[]}""");

            var result = await InvokeAsync(
                GetHandler("PreviewVectorMapSourceAsync"),
                root,
                rel,
                CancellationToken.None);

            var preview = ExtractOkValue<VectorMapInspectResult>(result);
            preview.Ok.Should().BeTrue();
            preview.FormatHint.Should().Be("geojson");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact(Timeout = 15000)]
    public async Task PreviewVectorMapSource_Traversal_ReturnsBadRequest()
    {
        var result = await InvokeAsync(
            GetHandler("PreviewVectorMapSourceAsync"),
            Path.GetTempPath(),
            "../etc/passwd",
            CancellationToken.None);

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task CreateSession_EmptyName_ReturnsBadRequest()
    {
        var result = await InvokeAsync(GetHandler("CreateSessionAsync"),
            new ForgeCreateSessionRequest("   "));

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
        ExtractProblemTitle(result).Should().Be("Name is required");
    }

    [Fact(Timeout = 15000)]
    public async Task ApplySetting_NullBody_ReturnsBadRequest()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("X"));

        var result = await InvokeAsync(GetHandler("ApplySettingAsync"), (ScopedSetting?)null);

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
        ExtractProblemTitle(result).Should().Contain("SettingId");
    }

    [Fact(Timeout = 15000)]
    public async Task ApplySetting_EmptySettingId_ReturnsBadRequest()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("X"));

        var bad = new ScopedSetting
        {
            SettingId = "  ",
            Value = 1,
            Scope = new SettingScope { Type = SettingScopeType.Server },
            CreatedBy = "t",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await InvokeAsync(GetHandler("ApplySettingAsync"), bad);

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task GenerateContent_EmptyPrompt_ReturnsBadRequest()
    {
        var result = await InvokeAsync(GetHandler("GenerateContentAsync"),
            new ForgeGenerateRequest("   ", null));

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
        ExtractProblemTitle(result).Should().Be("Prompt is required");
    }

    [Fact(Timeout = 15000)]
    public async Task GenerateContent_MapElementCategory_ReturnsMapElementDescriptor()
    {
        var result = await InvokeAsync(GetHandler("GenerateContentAsync"),
            new ForgeGenerateRequest("Stone barrier", "map_element"));

        var response = ExtractOkValue<ForgeGenerateResponse>(result);
        response.Category.Should().Be("map_element");
        response.Descriptor.Should().BeOfType<MapElementDescriptor>();
    }

    [Fact(Timeout = 15000)]
    public async Task ImportSession_EmptyJson_ReturnsBadRequest()
    {
        var result = await InvokeAsync(GetHandler("ImportSessionAsync"),
            new ForgeSessionImportRequest("   "));

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
        ExtractProblemTitle(result).Should().Contain("JSON");
    }

    [Fact(Timeout = 15000)]
    public async Task ImportSession_InvalidJson_ReturnsBadRequest()
    {
        var result = await InvokeAsync(GetHandler("ImportSessionAsync"),
            new ForgeSessionImportRequest("{ not json"));

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
        ExtractProblemTitle(result).Should().Contain("Invalid");
    }

    [Fact(Timeout = 15000)]
    public async Task RegisterMacro_Null_ReturnsBadRequest()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("Mac"));

        var result = await InvokeAsync(GetHandler("RegisterMacroAsync"), (MacroDefinition?)null);

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task RegisterMacro_EmptyMacroId_ReturnsBadRequest()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("Mac"));

        var macro = new MacroDefinition { MacroId = "  ", DisplayName = "X", Author = "a" };
        var result = await InvokeAsync(GetHandler("RegisterMacroAsync"), macro);

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task ExportMacro_UnknownId_ReturnsNotFound()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("Mac"));

        var result = await InvokeAsync(GetHandler("ExportMacroAsync"), "missing-macro");

        AssertStatusCode(result, StatusCodes.Status404NotFound);
    }

    [Fact(Timeout = 15000)]
    public async Task ImportMacro_EmptyJson_ReturnsBadRequest()
    {
        var result = await InvokeAsync(GetHandler("ImportMacroAsync"),
            new ForgeMacroImportRequest("  "));

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task ImportMacro_InvalidJson_ReturnsBadRequest()
    {
        var result = await InvokeAsync(GetHandler("ImportMacroAsync"),
            new ForgeMacroImportRequest("{"));

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
        ExtractProblemTitle(result).Should().Contain("Invalid");
    }

    [Fact(Timeout = 15000)]
    public async Task MacroExportImport_RoundTrips()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("MacroRt"));

        var macro = new MacroDefinition
        {
            MacroId = "rt-macro",
            DisplayName = "Round Trip",
            Description = "test",
            Author = "t"
        };
        await InvokeAsync(GetHandler("RegisterMacroAsync"), macro);

        var exportResult = await InvokeAsync(GetHandler("ExportMacroAsync"), "rt-macro");
        var export = ExtractOkValue<ForgeMacroExportResponse>(exportResult);
        export.Json.Should().NotBeNullOrWhiteSpace();

        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("Fresh"));

        var importResult = await InvokeAsync(GetHandler("ImportMacroAsync"),
            new ForgeMacroImportRequest(export.Json));
        var imported = ExtractOkValue<MacroDefinition>(importResult);
        imported.MacroId.Should().Be("rt-macro");
    }

    [Fact(Timeout = 15000)]
    public async Task GetMapAdaptationPlan_ScopedAestheticOnly_FallsBackToBuiltInCatalog()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("CatalogFallback"));

        await InvokeAsync(GetHandler("ApplySettingAsync"), new ScopedSetting
        {
            SettingId = "aesthetic",
            Value = "pbr",
            Scope = new SettingScope { Type = SettingScopeType.Server },
            CreatedBy = "test",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var result = await InvokeAsync(GetHandler("GetMapAdaptationPlanAsync"));
        var plan = ExtractOkValue<MapAdaptationPlan>(result);
        plan.ActiveAestheticId.Should().Be("pbr");
        plan.EffectiveMapRenderingProfile.Should().Be(MapRenderingProfiles.HeightfieldMesh);
        plan.PipelineStages.Should().Contain("mesh_emit");
    }

    [Fact(Timeout = 15000)]
    public async Task GetMapAdaptationPlan_VectorOverlayOverride_UsesOverlayPipeline()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("OverlayPlan"));
        await InvokeAsync(GetHandler("ApplyAestheticAsync"),
            new ForgeApplyAestheticRequest("voxel", MapRenderingProfile: MapRenderingProfiles.VectorOverlay));

        var result = await InvokeAsync(GetHandler("GetMapAdaptationPlanAsync"));
        var plan = ExtractOkValue<MapAdaptationPlan>(result);
        plan.EffectiveMapRenderingProfile.Should().Be(MapRenderingProfiles.VectorOverlay);
        plan.PipelineStages.Should().Contain("overlay_rasterize");
        plan.PipelineStages.Should().NotContain("voxel_rasterize");
    }

    [Fact(Timeout = 15000)]
    public async Task PreviewVectorMapSource_MissingFile_ReturnsBadRequest()
    {
        var root = Path.Combine(Path.GetTempPath(), "forge-missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var result = await InvokeAsync(
                GetHandler("PreviewVectorMapSourceAsync"),
                root,
                "nope.geojson",
                CancellationToken.None);

            AssertStatusCode(result, StatusCodes.Status400BadRequest);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact(Timeout = 15000)]
    public async Task PreviewVectorMapSource_EmptyRoot_ReturnsBadRequest()
    {
        var result = await InvokeAsync(
            GetHandler("PreviewVectorMapSourceAsync"),
            "   ",
            "x.geojson",
            CancellationToken.None);

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task PreviewVectorMapSource_OsmXml_SniffsFormat()
    {
        var root = Path.Combine(Path.GetTempPath(), "forge-osm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string rel = "sample.osm";
            await File.WriteAllTextAsync(Path.Combine(root, rel),
                """<?xml version="1.0"?><osm version="0.6"><node id="1"/></osm>""");

            var result = await InvokeAsync(
                GetHandler("PreviewVectorMapSourceAsync"),
                root,
                rel,
                CancellationToken.None);

            var preview = ExtractOkValue<VectorMapInspectResult>(result);
            preview.FormatHint.Should().Be("osm_xml");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact(Timeout = 15000)]
    public async Task ApplyAesthetic_EmptyAestheticId_ReturnsBadRequest()
    {
        await InvokeAsync(GetHandler("CreateSessionAsync"), new ForgeCreateSessionRequest("BadAesthetic"));

        var result = await InvokeAsync(GetHandler("ApplyAestheticAsync"),
            new ForgeApplyAestheticRequest("  "));

        AssertStatusCode(result, StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task GenerateStub_ReturnsDescriptor()
    {
        var request = new ForgeGenerateRequest("Plasma Rifle", "weapon");
        var handler = GetHandler("GenerateContentAsync");
        var result = await InvokeAsync(handler, request);

        var response = ExtractOkValue<ForgeGenerateResponse>(result);
        response.Should().NotBeNull();
        response.Prompt.Should().Be("Plasma Rifle");
        response.Category.Should().Be("weapon");
        response.Descriptor.Should().NotBeNull();
        response.Descriptor.Should().BeOfType<WeaponDescriptor>();
        var weapon = (WeaponDescriptor)response.Descriptor;
        weapon.Name.Should().Contain("Plasma Rifle");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static MethodInfo GetHandler(string name)
    {
        var method = typeof(ForgeEndpoints).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull($"handler '{name}' should exist on ForgeEndpoints");
        return method!;
    }

    private static async Task<IResult> InvokeAsync(MethodInfo handler, params object?[] args)
    {
        var task = (Task<IResult>)handler.Invoke(null, args)!;
        return await task;
    }

    private static T ExtractOkValue<T>(IResult result)
    {
        var valueProp = result.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        valueProp.Should().NotBeNull();
        var value = valueProp!.GetValue(result);
        value.Should().BeAssignableTo<T>();
        return (T)value!;
    }

    private static void AssertStatusCode(IResult result, int expected)
    {
        var status = result as IStatusCodeHttpResult;
        status.Should().NotBeNull($"result type {result.GetType().FullName} should expose HTTP status");
        status!.StatusCode.Should().Be(expected);
    }

    private static string ExtractProblemTitle(IResult result)
    {
        var valueProp = result.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        var value = valueProp?.GetValue(result);
        if (value is ProblemDetails p && !string.IsNullOrEmpty(p.Title))
            return p.Title;
        throw new InvalidOperationException(
            $"Expected ProblemDetails in IResult; got {value?.GetType().FullName ?? "null"} for {result.GetType().FullName}.");
    }

    private static void ResetStore()
    {
        var storeType = typeof(ForgeEndpoints).Assembly
            .GetType("Nexo.API.Endpoints.ForgeSessionStore");
        if (storeType is null) return;

        var sessionProp = storeType.GetProperty("Session", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        var registryProp = storeType.GetProperty("Registry", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        var defaultSession = new SessionState
        {
            SessionId = Guid.NewGuid().ToString("D"),
            Name = "Default Forge Session",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            MaxPlayers = 8,
            GameRules = new GameRuleDescriptor
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = "Default",
                Mode = "deathmatch"
            }
        };

        sessionProp?.SetValue(null, defaultSession);
        registryProp?.SetValue(null, new MacroRegistry());
    }
}
