using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nexo.API.Endpoints;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Descriptors;
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
