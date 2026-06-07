using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexo.API.Forge;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Descriptors;
using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Mapping;
using Nexo.GameDomain.Materials;
using Nexo.GameDomain.Scoping;
using Nexo.GameDomain.Session;
using Nexo.GameDomain.Contracts;

namespace Nexo.API.Endpoints;

/// <summary>
/// Nexo Forge HTTP API: sessions, settings, macros, aesthetics, map adaptation planning,
/// and optional LiteDB-backed persistence via <see cref="IForgeStateService"/>.
/// </summary>
public static class ForgeEndpoints
{
    public static IEndpointRouteBuilder MapForgeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/forge").WithTags("Forge");

        group.MapGet("/session", GetSessionAsync)
            .WithName("GetForgeSession")
            .WithSummary("Return the current Forge session state")
            .Produces<SessionState>(StatusCodes.Status200OK);

        group.MapPost("/session/create", CreateSessionAsync)
            .WithName("CreateForgeSession")
            .WithSummary("Create a new empty session with the given game rule")
            .Produces<SessionState>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/session/export", ExportSessionAsync)
            .WithName("ExportForgeSession")
            .WithSummary("Export the current session as JSON")
            .Produces<ForgeSessionExportResponse>(StatusCodes.Status200OK);

        group.MapPost("/session/import", ImportSessionAsync)
            .WithName("ImportForgeSession")
            .WithSummary("Import a session from a JSON body")
            .Produces<SessionState>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/generate", GenerateContentAsync)
            .WithName("ForgeGenerate")
            .WithSummary("Generate a stub descriptor from a prompt (LLM wired later)")
            .Produces<ForgeGenerateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/setting", ApplySettingAsync)
            .WithName("ApplyForgeSetting")
            .WithSummary("Apply a scoped setting to the current session")
            .Produces<SessionState>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/setting/{settingId}", RemoveSettingAsync)
            .WithName("RemoveForgeSetting")
            .WithSummary("Remove a scoped setting by its setting identifier")
            .Produces<SessionState>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/settings/resolve", ResolveSettingsAsync)
            .WithName("ResolveForgeSettings")
            .WithSummary("Resolve all settings for a given scope context")
            .Produces<ForgeResolvedSettingsResponse>(StatusCodes.Status200OK);

        group.MapGet("/macros", ListMacrosAsync)
            .WithName("ListForgeMacros")
            .WithSummary("List all registered macros")
            .Produces<IReadOnlyList<MacroDefinition>>(StatusCodes.Status200OK);

        group.MapPost("/macro", RegisterMacroAsync)
            .WithName("RegisterForgeMacro")
            .WithSummary("Register a new macro in the macro registry")
            .Produces<MacroDefinition>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/macro/{macroId}/export", ExportMacroAsync)
            .WithName("ExportForgeMacro")
            .WithSummary("Export a registered macro as JSON")
            .Produces<ForgeMacroExportResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/macro/import", ImportMacroAsync)
            .WithName("ImportForgeMacro")
            .WithSummary("Import a macro from a JSON body")
            .Produces<MacroDefinition>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/aesthetics", GetAestheticsAsync)
            .WithName("GetForgeAesthetics")
            .WithSummary("Return the built-in aesthetic packs")
            .Produces<IReadOnlyList<AestheticPack>>(StatusCodes.Status200OK);

        group.MapPost("/aesthetic/apply", ApplyAestheticAsync)
            .WithName("ApplyForgeAesthetic")
            .WithSummary("Apply an aesthetic pack at a given scope")
            .Produces<SessionState>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/aesthetic/apply-pack", ApplyCustomAestheticPackAsync)
            .WithName("ApplyForgeCustomAestheticPack")
            .WithSummary("Apply a full AestheticPack JSON (custom id, bindings, pipeline kind) after validation")
            .Produces<SessionState>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/map/adaptation-plan", GetMapAdaptationPlanAsync)
            .WithName("GetForgeMapAdaptationPlan")
            .WithSummary("Return map adaptation plan for the active aesthetic")
            .Produces<MapAdaptationPlan>(StatusCodes.Status200OK);

        group.MapGet("/map/tile-pyramid", GetTilePyramidAsync)
            .WithName("GetForgeMapTilePyramid")
            .WithSummary("LOD tile pyramid (zoom per tier) from active aesthetic LodLevels and finestZoom")
            .Produces<ForgeTilePyramidResponse>(StatusCodes.Status200OK);

        group.MapGet("/map/material-hints", GetMaterialHintsAsync)
            .WithName("GetForgeMapMaterialHints")
            .WithSummary("Heuristic material / surface hints from the active aesthetic and optional vector parse kind")
            .Produces<ForgeMaterialHintsResponse>(StatusCodes.Status200OK);

        group.MapPost("/map/pipeline/run", RunMapPipelineAsync)
            .WithName("RunForgeMapPipeline")
            .WithSummary("Run (or dry-run) the map adaptation pipeline for the current plan")
            .Produces<MapPipelineRunResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/engine/{engineId}/aesthetic-manifest", GetEngineAestheticManifestAsync)
            .WithName("GetForgeEngineAestheticManifest")
            .WithSummary("JSON manifest for an engine from the active aesthetic pack")
            .Produces<ForgeEngineManifestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> GetSessionAsync(IForgeStateService forge)
    {
        await Task.CompletedTask;
        return Results.Ok(forge.Session);
    }

    private static async Task<IResult> CreateSessionAsync(
        IForgeStateService forge,
        [FromBody] ForgeCreateSessionRequest request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request?.Name))
            return Results.BadRequest(new ProblemDetails { Title = "Name is required" });

        var session = new SessionState
        {
            SessionId = Guid.NewGuid().ToString("D"),
            Name = request.Name.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            MaxPlayers = request.MaxPlayers > 0 ? request.MaxPlayers : 8,
            GameRules = request.GameRule is not null
                ? request.GameRule
                : new GameRuleDescriptor { Id = Guid.NewGuid().ToString("D"), Name = "Default", Mode = "deathmatch" }
        };

        forge.Session = session;
        forge.Registry = new MacroRegistry();
        forge.Save();
        return Results.Ok(session);
    }

    private static async Task<IResult> ExportSessionAsync(IForgeStateService forge)
    {
        await Task.CompletedTask;
        var json = SessionExporter.ExportToJson(forge.Session);
        return Results.Ok(new ForgeSessionExportResponse(json));
    }

    private static async Task<IResult> ImportSessionAsync(
        IForgeStateService forge,
        [FromBody] ForgeSessionImportRequest request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request?.Json))
            return Results.BadRequest(new ProblemDetails { Title = "JSON body is required" });

        try
        {
            var session = SessionExporter.ImportFromJson(request.Json);
            forge.Session = session;
            forge.Save();
            return Results.Ok(session);
        }
        catch (JsonException ex)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid session JSON",
                Detail = ex.Message
            });
        }
    }

    private static async Task<IResult> GenerateContentAsync(
        [FromBody] ForgeGenerateRequest request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request?.Prompt))
            return Results.BadRequest(new ProblemDetails { Title = "Prompt is required" });

        var category = (request.Category ?? "weapon").ToLowerInvariant();
        object descriptor = category switch
        {
            "weapon" => new WeaponDescriptor
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = $"Generated: {request.Prompt.Trim()}",
                DamagePerHit = 25,
                FireRatePerSecond = 5,
                Range = 50,
                MagazineSize = 30,
                ReloadTimeSeconds = 2.0,
                ProjectileType = "hitscan"
            },
            "ability" => new AbilityDescriptor
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = $"Generated: {request.Prompt.Trim()}",
                CooldownSeconds = 8,
                Duration = 3,
                EnergyCost = 40,
                EffectType = "utility"
            },
            "map_element" => new MapElementDescriptor
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = $"Generated: {request.Prompt.Trim()}",
                Category = "structure",
                Dimensions = new Dimensions(2.0, 3.0, 2.0),
                MaterialClass = "stone"
            },
            "game_rule" => new GameRuleDescriptor
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = $"Generated: {request.Prompt.Trim()}",
                Mode = "deathmatch",
                RoundDurationSeconds = 300,
                MaxScore = 25
            },
            "ai_behavior" => new AiBehaviorDescriptor
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = $"Generated: {request.Prompt.Trim()}",
                Archetype = "patrol",
                Aggression = 0.5,
                Accuracy = 0.5,
                ReactionTimeSeconds = 0.3,
                PreferredRange = 15.0
            },
            _ => new WeaponDescriptor
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = $"Generated: {request.Prompt.Trim()}",
                DamagePerHit = 25,
                FireRatePerSecond = 5,
                Range = 50,
                MagazineSize = 30,
                ReloadTimeSeconds = 2.0,
                ProjectileType = "hitscan"
            }
        };

        return Results.Ok(new ForgeGenerateResponse(request.Prompt.Trim(), category, descriptor));
    }

    private static async Task<IResult> ApplySettingAsync(
        IForgeStateService forge,
        [FromBody] ScopedSetting? setting)
    {
        await Task.CompletedTask;

        if (setting is null || string.IsNullOrWhiteSpace(setting.SettingId))
            return Results.BadRequest(new ProblemDetails { Title = "A valid ScopedSetting with a SettingId is required" });

        var session = forge.Session;
        session.ScopedSettings.Add(setting);
        session.LastModifiedAtUtc = DateTimeOffset.UtcNow;
        forge.Save();
        return Results.Ok(session);
    }

    private static async Task<IResult> RemoveSettingAsync(
        IForgeStateService forge,
        string settingId)
    {
        await Task.CompletedTask;

        var session = forge.Session;
        var removed = session.ScopedSettings.RemoveAll(s => s.SettingId == settingId);
        if (removed == 0)
            return Results.NotFound(new ProblemDetails { Title = $"No setting found with id '{settingId}'" });

        session.LastModifiedAtUtc = DateTimeOffset.UtcNow;
        forge.Save();
        return Results.Ok(session);
    }

    private static async Task<IResult> ResolveSettingsAsync(
        IForgeStateService forge,
        [FromQuery] string? playerId,
        [FromQuery] string? teamId,
        [FromQuery] string? zoneId,
        [FromQuery] string? objectId,
        [FromQuery] string? moment)
    {
        await Task.CompletedTask;

        var context = new ScopeContext
        {
            PlayerId = playerId,
            TeamId = teamId,
            ZoneId = zoneId,
            ObjectId = objectId,
            ActiveMoment = moment
        };

        var resolver = new ScopeResolver(forge.Session.ScopedSettings);
        var resolved = resolver.ResolveAll(context);
        return Results.Ok(new ForgeResolvedSettingsResponse(context, resolved));
    }

    private static async Task<IResult> ListMacrosAsync(IForgeStateService forge)
    {
        await Task.CompletedTask;
        return Results.Ok(forge.Registry.List());
    }

    private static async Task<IResult> RegisterMacroAsync(
        IForgeStateService forge,
        [FromBody] MacroDefinition? macro)
    {
        await Task.CompletedTask;

        if (macro is null || string.IsNullOrWhiteSpace(macro.MacroId))
            return Results.BadRequest(new ProblemDetails { Title = "A valid MacroDefinition with a MacroId is required" });

        forge.Registry.Register(macro);
        forge.Save();
        return Results.Ok(macro);
    }

    private static async Task<IResult> ExportMacroAsync(
        IForgeStateService forge,
        string macroId)
    {
        await Task.CompletedTask;

        var macro = forge.Registry.Export(macroId);
        if (macro is null)
            return Results.NotFound(new ProblemDetails { Title = $"Macro '{macroId}' not found" });

        var json = MacroExporter.ExportToJson(macro);
        return Results.Ok(new ForgeMacroExportResponse(macroId, json));
    }

    private static async Task<IResult> ImportMacroAsync(
        IForgeStateService forge,
        [FromBody] ForgeMacroImportRequest request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request?.Json))
            return Results.BadRequest(new ProblemDetails { Title = "JSON body is required" });

        try
        {
            var macro = MacroExporter.ImportFromJson(request.Json);
            forge.Registry.Import(macro);
            forge.Save();
            return Results.Ok(macro);
        }
        catch (JsonException ex)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid macro JSON",
                Detail = ex.Message
            });
        }
    }

    private static async Task<IResult> GetAestheticsAsync()
    {
        await Task.CompletedTask;
        return Results.Ok(BuiltInAestheticPacks.Catalog);
    }

    private static async Task<IResult> ApplyAestheticAsync(
        IForgeStateService forge,
        [FromBody] ForgeApplyAestheticRequest request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request?.AestheticId))
            return Results.BadRequest(new ProblemDetails { Title = "AestheticId is required" });

        var pack = BuiltInAestheticPacks.Catalog
            .FirstOrDefault(a => a.Id == request.AestheticId);

        if (pack is null)
            return Results.BadRequest(new ProblemDetails { Title = $"Unknown aesthetic pack '{request.AestheticId}'" });

        var setting = new ScopedSetting
        {
            SettingId = "aesthetic",
            Value = request.AestheticId,
            Scope = request.Scope ?? new SettingScope(),
            CreatedBy = "forge",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var session = forge.Session;
        session.ScopedSettings.RemoveAll(s =>
            s.SettingId == "aesthetic" &&
            s.Scope.Type == setting.Scope.Type &&
            s.Scope.Target == setting.Scope.Target);
        session.ScopedSettings.Add(setting);

        if (!session.AestheticPacks.Any(a => a.Id == pack.Id))
            session.AestheticPacks.Add(pack);

        session.LastModifiedAtUtc = DateTimeOffset.UtcNow;
        forge.Save();
        return Results.Ok(session);
    }

    private static async Task<IResult> ApplyCustomAestheticPackAsync(
        IForgeStateService forge,
        [FromBody] ForgeApplyCustomAestheticPackRequest? request)
    {
        await Task.CompletedTask;

        if (request?.Pack is null)
            return Results.BadRequest(new ProblemDetails { Title = "Pack is required" });

        var pack = request.Pack;
        if (string.IsNullOrWhiteSpace(pack.Id))
            return Results.BadRequest(new ProblemDetails { Title = "AestheticPack.Id is required" });

        var validationOptions = new AestheticPackValidationOptions
        {
            RequireKnownEngineIds = request.RequireKnownEngineIds ?? false
        };
        var issues = AestheticPackValidation.Validate(pack, validationOptions);
        if (!AestheticPackValidation.IsValid(issues, treatUndocumentedAsNonBlocking: true))
        {
            var detail = string.Join("; ", issues.Select(i => $"{i.Code}: {i.Message}"));
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Aesthetic pack validation failed",
                Detail = detail
            });
        }

        var setting = new ScopedSetting
        {
            SettingId = "aesthetic",
            Value = pack.Id,
            Scope = request.Scope ?? new SettingScope(),
            CreatedBy = "forge",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var session = forge.Session;
        session.ScopedSettings.RemoveAll(s =>
            s.SettingId == "aesthetic" &&
            s.Scope.Type == setting.Scope.Type &&
            s.Scope.Target == setting.Scope.Target);
        session.ScopedSettings.Add(setting);

        session.AestheticPacks.RemoveAll(a => a.Id == pack.Id);
        session.AestheticPacks.Add(pack);

        session.LastModifiedAtUtc = DateTimeOffset.UtcNow;
        forge.Save();
        return Results.Ok(session);
    }

    private static async Task<IResult> GetMapAdaptationPlanAsync(IForgeStateService forge)
    {
        await Task.CompletedTask;
        var plan = MapAdaptationPlanner.Plan(forge.Session, BuiltInAestheticPacks.Catalog);
        return Results.Ok(plan);
    }

    private static async Task<IResult> RunMapPipelineAsync(
        IForgeStateService forge,
        MapPipelineRunner pipeline,
        [FromBody] MapPipelineRunRequest? body)
    {
        var req = body ?? new MapPipelineRunRequest();
        var plan = MapAdaptationPlanner.Plan(forge.Session, BuiltInAestheticPacks.Catalog);

        if (req.DryRun)
        {
            await Task.CompletedTask;
            var dry = MapPipelineDryRun.Execute(plan, req);
            return Results.Ok(dry);
        }

        var result = await pipeline.RunAsync(plan, req).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEngineAestheticManifestAsync(
        IForgeStateService forge,
        string engineId)
    {
        await Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(engineId))
            return Results.BadRequest(new ProblemDetails { Title = "engineId is required" });

        var pack = MapAdaptationPlanner.GetActivePack(forge.Session, BuiltInAestheticPacks.Catalog);
        var json = EngineAestheticManifestBuilder.BuildJson(engineId, pack);
        return Results.Ok(new ForgeEngineManifestResponse(json));
    }

    private static async Task<IResult> GetTilePyramidAsync(
        IForgeStateService forge,
        [FromQuery] int? finestZoom)
    {
        await Task.CompletedTask;
        var z = finestZoom is >= 0 and <= 22 ? finestZoom.Value : MapLodPyramidPlanner.DefaultFinestZoom;
        var pack = MapAdaptationPlanner.GetActivePack(forge.Session, BuiltInAestheticPacks.Catalog);
        var tiers = MapLodPyramidPlanner.Build(pack.LodLevels, z);
        return Results.Ok(new ForgeTilePyramidResponse(z, tiers));
    }

    private static async Task<IResult> GetMaterialHintsAsync(
        IForgeStateService forge,
        IMaterialIntelligenceService materials,
        [FromQuery] string? parseKind)
    {
        var pack = MapAdaptationPlanner.GetActivePack(forge.Session, BuiltInAestheticPacks.Catalog);
        var result = await materials.SuggestAsync(pack, parseKind).ConfigureAwait(false);
        return Results.Ok(new ForgeMaterialHintsResponse(result.Summary, result.Hints));
    }
}

public sealed record ForgeCreateSessionRequest(
    string Name,
    int MaxPlayers = 8,
    GameRuleDescriptor? GameRule = null);

public sealed record ForgeSessionExportResponse(string Json);

public sealed record ForgeSessionImportRequest(string Json);

public sealed record ForgeGenerateRequest(string Prompt, string? Category);

public sealed record ForgeGenerateResponse(string Prompt, string Category, object Descriptor);

public sealed record ForgeResolvedSettingsResponse(
    ScopeContext Context,
    Dictionary<string, object> ResolvedSettings);

public sealed record ForgeMacroExportResponse(string MacroId, string Json);

public sealed record ForgeMacroImportRequest(string Json);

public sealed record ForgeApplyAestheticRequest(
    string AestheticId,
    SettingScope? Scope = null);

public sealed record ForgeEngineManifestResponse(string Json);

public sealed record ForgeTilePyramidResponse(int FinestZoom, IReadOnlyList<MapTilePyramidTier> Tiers);

public sealed record ForgeMaterialHintsResponse(string Summary, IReadOnlyList<MaterialSurfaceHint> Hints);
