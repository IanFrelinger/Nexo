using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Contracts;
using Nexo.GameDomain.Descriptors;
using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Scoping;
using Nexo.GameDomain.Session;

namespace Nexo.API.Endpoints;

/// <summary>
/// Extension methods for mapping Nexo Forge API endpoints.
/// <para>
/// The Forge endpoints expose a prototyping surface for game design sessions, including
/// session management, procedural content generation stubs, scoped-setting resolution,
/// macro orchestration, and aesthetic-pack application.  All state is held in-memory via
/// <see cref="ForgeSessionStore"/> — persistence is deferred to a future workstream.
/// </para>
/// </summary>
public static class ForgeEndpoints
{
    public static IEndpointRouteBuilder MapForgeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/forge").WithTags("Forge");

        // ── Session management ──────────────────────────────────────────

        group.MapGet("/session", GetSessionAsync)
            .WithName("GetForgeSession")
            .WithSummary("Return the current in-memory session state")
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

        // ── Content generation ──────────────────────────────────────────

        group.MapPost("/generate", GenerateContentAsync)
            .WithName("ForgeGenerate")
            .WithSummary("Generate a stub descriptor from a prompt (LLM wired later)")
            .Produces<ForgeGenerateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── Scoped settings ─────────────────────────────────────────────

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

        // ── Macros ──────────────────────────────────────────────────────

        group.MapGet("/macros", ListMacrosAsync)
            .WithName("ListForgeMacros")
            .WithSummary("List all registered macros")
            .Produces<IReadOnlyList<MacroDefinition>>(StatusCodes.Status200OK);

        group.MapPost("/macro", RegisterMacroAsync)
            .WithName("RegisterForgeMacro")
            .WithSummary("Register a new macro in the in-memory registry")
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

        // ── Aesthetics ──────────────────────────────────────────────────

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

        return app;
    }

    // ── Session handlers ────────────────────────────────────────────────

    private static async Task<IResult> GetSessionAsync()
    {
        await Task.CompletedTask;
        return Results.Ok(ForgeSessionStore.Session);
    }

    private static async Task<IResult> CreateSessionAsync(
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

        ForgeSessionStore.Session = session;
        ForgeSessionStore.Registry = new MacroRegistry();
        return Results.Ok(session);
    }

    private static async Task<IResult> ExportSessionAsync()
    {
        await Task.CompletedTask;
        var json = SessionExporter.ExportToJson(ForgeSessionStore.Session);
        return Results.Ok(new ForgeSessionExportResponse(json));
    }

    private static async Task<IResult> ImportSessionAsync(
        [FromBody] ForgeSessionImportRequest request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request?.Json))
            return Results.BadRequest(new ProblemDetails { Title = "JSON body is required" });

        try
        {
            var session = SessionExporter.ImportFromJson(request.Json);
            ForgeSessionStore.Session = session;
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

    // ── Content generation handler ──────────────────────────────────────

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

    // ── Scoped settings handlers ────────────────────────────────────────

    private static async Task<IResult> ApplySettingAsync(
        [FromBody] ScopedSetting? setting)
    {
        await Task.CompletedTask;

        if (setting is null || string.IsNullOrWhiteSpace(setting.SettingId))
            return Results.BadRequest(new ProblemDetails { Title = "A valid ScopedSetting with a SettingId is required" });

        var session = ForgeSessionStore.Session;
        session.ScopedSettings.Add(setting);
        session.LastModifiedAtUtc = DateTimeOffset.UtcNow;
        return Results.Ok(session);
    }

    private static async Task<IResult> RemoveSettingAsync(string settingId)
    {
        await Task.CompletedTask;

        var session = ForgeSessionStore.Session;
        var removed = session.ScopedSettings.RemoveAll(s => s.SettingId == settingId);
        if (removed == 0)
            return Results.NotFound(new ProblemDetails { Title = $"No setting found with id '{settingId}'" });

        session.LastModifiedAtUtc = DateTimeOffset.UtcNow;
        return Results.Ok(session);
    }

    private static async Task<IResult> ResolveSettingsAsync(
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

        var resolver = new ScopeResolver(ForgeSessionStore.Session.ScopedSettings);
        var resolved = resolver.ResolveAll(context);
        return Results.Ok(new ForgeResolvedSettingsResponse(context, resolved));
    }

    // ── Macro handlers ──────────────────────────────────────────────────

    private static async Task<IResult> ListMacrosAsync()
    {
        await Task.CompletedTask;
        return Results.Ok(ForgeSessionStore.Registry.List());
    }

    private static async Task<IResult> RegisterMacroAsync(
        [FromBody] MacroDefinition? macro)
    {
        await Task.CompletedTask;

        if (macro is null || string.IsNullOrWhiteSpace(macro.MacroId))
            return Results.BadRequest(new ProblemDetails { Title = "A valid MacroDefinition with a MacroId is required" });

        ForgeSessionStore.Registry.Register(macro);
        return Results.Ok(macro);
    }

    private static async Task<IResult> ExportMacroAsync(string macroId)
    {
        await Task.CompletedTask;

        var macro = ForgeSessionStore.Registry.Export(macroId);
        if (macro is null)
            return Results.NotFound(new ProblemDetails { Title = $"Macro '{macroId}' not found" });

        var json = MacroExporter.ExportToJson(macro);
        return Results.Ok(new ForgeMacroExportResponse(macroId, json));
    }

    private static async Task<IResult> ImportMacroAsync(
        [FromBody] ForgeMacroImportRequest request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request?.Json))
            return Results.BadRequest(new ProblemDetails { Title = "JSON body is required" });

        try
        {
            var macro = MacroExporter.ImportFromJson(request.Json);
            ForgeSessionStore.Registry.Import(macro);
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

    // ── Aesthetics handlers ─────────────────────────────────────────────

    private static async Task<IResult> GetAestheticsAsync()
    {
        await Task.CompletedTask;
        return Results.Ok(ForgeSessionStore.BuiltInAesthetics);
    }

    private static async Task<IResult> ApplyAestheticAsync(
        [FromBody] ForgeApplyAestheticRequest request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request?.AestheticId))
            return Results.BadRequest(new ProblemDetails { Title = "AestheticId is required" });

        var pack = ForgeSessionStore.BuiltInAesthetics
            .FirstOrDefault(a => a.Id == request.AestheticId);

        if (pack is null)
            return Results.BadRequest(new ProblemDetails { Title = $"Unknown aesthetic pack '{request.AestheticId}'" });

        var builtInIssues = AestheticPackValidation.Validate(pack);
        if (!AestheticPackValidation.IsValid(builtInIssues))
        {
            var detail = string.Join("; ", builtInIssues.Select(i => $"{i.Code}: {i.Message}"));
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Built-in aesthetic pack failed validation",
                Detail = detail
            });
        }
        var setting = new ScopedSetting
        {
            SettingId = "aesthetic",
            Value = request.AestheticId,
            Scope = request.Scope ?? new SettingScope(),
            CreatedBy = "forge",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var session = ForgeSessionStore.Session;
        session.ScopedSettings.RemoveAll(s =>
            s.SettingId == "aesthetic" &&
            s.Scope.Type == setting.Scope.Type &&
            s.Scope.Target == setting.Scope.Target);
        session.ScopedSettings.Add(setting);

        if (!session.AestheticPacks.Any(a => a.Id == pack.Id))
            session.AestheticPacks.Add(pack);

        session.LastModifiedAtUtc = DateTimeOffset.UtcNow;
        return Results.Ok(session);
    }

    private static async Task<IResult> ApplyCustomAestheticPackAsync(
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

        var session = ForgeSessionStore.Session;
        session.ScopedSettings.RemoveAll(s =>
            s.SettingId == "aesthetic" &&
            s.Scope.Type == setting.Scope.Type &&
            s.Scope.Target == setting.Scope.Target);
        session.ScopedSettings.Add(setting);

        session.AestheticPacks.RemoveAll(a => a.Id == pack.Id);
        session.AestheticPacks.Add(pack);

        session.LastModifiedAtUtc = DateTimeOffset.UtcNow;
        return Results.Ok(session);
    }
}

// ── In-memory store ─────────────────────────────────────────────────────

/// <summary>
/// Static in-memory store for the current Forge session and macro registry.
/// <para>
/// PROTOTYPE ONLY — This static mutable state is not suitable for production.
/// Replace with proper persistence (e.g., database-backed session store) in a future workstream.
/// The <c>_session</c> field uses <see langword="volatile"/> to ensure atomic reference swaps
/// are visible across threads. <c>MacroRegistry</c> is backed by <c>ConcurrentDictionary</c>
/// and is inherently thread-safe for individual operations.
/// </para>
/// </summary>
internal static class ForgeSessionStore
{
    private static volatile SessionState _session = CreateDefaultSession();
    private static volatile MacroRegistry _registry = new();

    internal static SessionState Session
    {
        get => _session;
        set => _session = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal static MacroRegistry Registry
    {
        get => _registry;
        set => _registry = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal static readonly IReadOnlyList<AestheticPack> BuiltInAesthetics =
    [
        new AestheticPack
        {
            Id = "voxel", Name = "Voxel", GeometryStrategy = "voxel",
            DefaultPaletteColors = ["#4CAF50", "#2196F3", "#FF9800", "#9C27B0"],
            LodLevels = [new LodLevel(0, 1.0), new LodLevel(1, 0.5), new LodLevel(2, 0.25)]
        },
        new AestheticPack
        {
            Id = "low_poly", Name = "Low Poly", GeometryStrategy = "low_poly",
            RenderingPipelineKind = RenderingPipelineKinds.ForwardStylized,
            DefaultPaletteColors = ["#81C784", "#64B5F6", "#FFB74D", "#CE93D8"],
            LodLevels = [new LodLevel(0, 1.0), new LodLevel(1, 0.5)]
        },
        new AestheticPack
        {
            Id = "pixel_art", Name = "Pixel Art", GeometryStrategy = "pixel_art",
            DefaultPaletteColors = ["#388E3C", "#1976D2", "#F57C00", "#7B1FA2"],
            LodLevels = [new LodLevel(0, 1.0)]
        },
        new AestheticPack
        {
            Id = "pbr", Name = "PBR", GeometryStrategy = "pbr",
            RenderingPipelineKind = RenderingPipelineKinds.ForwardPbr,
            DefaultPaletteColors = ["#E0E0E0", "#BDBDBD", "#9E9E9E"],
            LodLevels = [new LodLevel(0, 1.0), new LodLevel(1, 0.75), new LodLevel(2, 0.5), new LodLevel(3, 0.25)],
            PostProcessEffects = ["bloom", "ambient_occlusion", "tone_mapping"]
        },
        new AestheticPack
        {
            Id = "wireframe", Name = "Wireframe", GeometryStrategy = "wireframe",
            DefaultPaletteColors = ["#00E676", "#00B0FF"],
            LodLevels = [new LodLevel(0, 1.0), new LodLevel(1, 0.5)]
        },
        new AestheticPack
        {
            Id = "sketch", Name = "Sketch", GeometryStrategy = "sketch",
            DefaultPaletteColors = ["#212121", "#FAFAFA"],
            LodLevels = [new LodLevel(0, 1.0)],
            PostProcessEffects = ["vignette", "chromatic_aberration"]
        }
    ];

    private static SessionState CreateDefaultSession()
    {
        return new SessionState
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
    }
}

// ── DTOs ────────────────────────────────────────────────────────────────

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
