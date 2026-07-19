using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Abstractions;

namespace Nexo.BRPlaytestAgent;

internal abstract class PlaytestTool(PlaytestSession session) : ITool
{
    protected PlaytestSession Session { get; } = session;

    public abstract string Id { get; }
    public abstract ToolSchema Schema { get; }
    public abstract Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken);

    protected static T Parse<T>(ToolCall call)
        => JsonSerializer.Deserialize<T>(
            call.Arguments.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

    protected static ToolResult Result(
        WorldSnapshot snapshot,
        string log,
        object? payload)
    {
        var delta = new PlaytestDelta
        {
            TickFrom = snapshot.Tick,
            TickTo = snapshot.Tick + 1
        };
        delta.Add(log);
        return new ToolResult(delta, payload);
    }

    protected string ArtifactPath(string relative)
    {
        var root = Path.GetFullPath(Session.ArtifactRoot)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(root, relative));
        if (!full.StartsWith(root, StringComparison.Ordinal))
            throw new InvalidOperationException("Artifact path escaped playtest root.");
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        return full;
    }
}

internal sealed class GameLaunchTool(PlaytestSession session) : PlaytestTool(session)
{
    public override string Id => "game.launch";
    public override ToolSchema Schema => new(
        Id,
        "Launch the allowlisted Unity Weapon Lab build and wait until its FPS control bridge is ready.",
        """{"type":"object","properties":{}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (Session.Process is { HasExited: false } && Session.Client is not null)
            return Result(snapshot, "game already running", new { running = true });

        var existingClient = new WeaponLabClient(Session.Port, Session.Token);
        var attachedToEditor = false;
        try
        {
            await existingClient.ConnectAsync(
                TimeSpan.FromSeconds(2),
                cancellationToken).ConfigureAwait(false);
            Session.Client = existingClient;
            attachedToEditor = true;
        }
        catch
        {
            await existingClient.DisposeAsync().ConfigureAwait(false);
            Session.Client = null;
        }
        if (attachedToEditor)
        {
            return await WaitUntilReadyAsync(
                snapshot,
                attachedToEditor: true,
                cancellationToken).ConfigureAwait(false);
        }

        var executable = ResolveExecutable(Session.BuildPath);
        var start = Session.VirtualPlayerMode
            ? new ProcessStartInfo("/usr/bin/open")
            : new ProcessStartInfo(executable);
        start.UseShellExecute = false;
        start.WorkingDirectory = Path.GetDirectoryName(executable)!;
        if (Session.VirtualPlayerMode)
        {
            start.ArgumentList.Add("-na");
            start.ArgumentList.Add(Session.BuildPath);
            start.ArgumentList.Add("--args");
        }
        start.Environment["NEXO_BR_PLAYTEST_PORT"] = Session.Port.ToString();
        start.Environment["NEXO_BR_PLAYTEST_TOKEN"] = Session.Token;
        start.Environment["NEXO_BR_PLAYTEST_ARTIFACT_ROOT"] =
            Path.GetFullPath(Session.ArtifactRoot);
        start.ArgumentList.Add("-screen-width");
        start.ArgumentList.Add("1920");
        start.ArgumentList.Add("-screen-height");
        start.ArgumentList.Add("1080");
        start.ArgumentList.Add("-window-mode");
        start.ArgumentList.Add("windowed");
        if (Session.BuiltInCaptureMode)
        {
            start.ArgumentList.Add("--br-capture");
            start.ArgumentList.Add("--br-capture-duration");
            start.ArgumentList.Add("120");
            start.ArgumentList.Add("--br-capture-output");
            start.ArgumentList.Add(
                Session.BuiltInCaptureOutputPath ??
                throw new InvalidOperationException(
                    "Built-in capture output path is required."));
        }
        if (!Session.VirtualPlayerMode)
            start.ArgumentList.Add("-batchmode");

        var beforePids = Session.VirtualPlayerMode
            ? FindProcessesByExecutable(executable)
                .Select(process => process.Id)
                .ToHashSet()
            : null;
        var launched = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start Weapon Lab build.");
        if (Session.VirtualPlayerMode)
        {
            launched.WaitForExit(10_000);
            launched.Dispose();
            var processDeadline =
                DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
            while (DateTimeOffset.UtcNow < processDeadline)
            {
                Session.Process = FindProcessesByExecutable(executable)
                    .Where(process => !beforePids!.Contains(process.Id))
                    .OrderByDescending(process => process.StartTime)
                    .FirstOrDefault();
                if (Session.Process is not null)
                    break;
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
            if (Session.Process is null)
                throw new InvalidOperationException(
                    "Windowed Weapon Lab process was not discovered.");
        }
        else
        {
            Session.Process = launched;
        }
        Session.Client = new WeaponLabClient(Session.Port, Session.Token);
        await Session.Client.ConnectAsync(
            TimeSpan.FromSeconds(30),
            cancellationToken).ConfigureAwait(false);
        return await WaitUntilReadyAsync(
            snapshot,
            attachedToEditor: false,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ToolResult> WaitUntilReadyAsync(
        WorldSnapshot snapshot,
        bool attachedToEditor,
        CancellationToken cancellationToken)
    {
        var client = Session.Client
            ?? throw new InvalidOperationException("Weapon Lab client is not connected.");
        JsonElement status = default;
        var deadline = DateTimeOffset.UtcNow +
                       TimeSpan.FromSeconds(attachedToEditor ? 60 : 30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            status = await client.SendAsync(
                "status",
                new { },
                cancellationToken).ConfigureAwait(false);
            if (status.TryGetProperty("ready", out var ready) &&
                ready.GetBoolean())
            {
                Session.Actions.Add("launch");
                return Result(
                    snapshot,
                    attachedToEditor
                        ? "attached to Unity Editor Weapon Lab"
                        : "Weapon Lab launched and ready",
                    new
                    {
                        running = true,
                        attachedToEditor,
                        pid = Session.Process?.Id,
                        status
                    });
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Weapon Lab launched but did not become ready.");
    }

    private static string ResolveExecutable(string buildPath)
    {
        var full = Path.GetFullPath(buildPath);
        if (!Directory.Exists(full) ||
            !full.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException(
                $"Weapon Lab .app build not found: {full}");
        }

        var macOs = Path.Combine(full, "Contents", "MacOS");
        return Directory.GetFiles(macOs)
                   .FirstOrDefault(path => !path.EndsWith(".dylib"))
               ?? throw new FileNotFoundException(
                   $"No executable found under {macOs}");
    }

    private static IReadOnlyList<Process> FindProcessesByExecutable(
        string executable)
    {
        var expected = Path.GetFullPath(executable);
        var matches = new List<Process>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (path is not null &&
                    string.Equals(
                        Path.GetFullPath(path),
                        expected,
                        StringComparison.Ordinal))
                {
                    matches.Add(process);
                }
                else
                {
                    process.Dispose();
                }
            }
            catch
            {
                process.Dispose();
            }
        }

        return matches;
    }
}

internal sealed class GameStatusTool(PlaytestSession session) : PlaytestTool(session)
{
    private sealed record Args(string? Label);

    public override string Id => "game.status";
    public override ToolSchema Schema => new(
        Id,
        "Read structured Weapon Lab state: health, position, weapon, ammo, nearby pickups, and FPS camera telemetry.",
        """{"type":"object","properties":{"label":{"type":"string"}}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var status = await RequireClient().SendAsync(
            "status",
            new { },
            cancellationToken).ConfigureAwait(false);
        var args = Parse<Args>(toolCall);
        var label = string.IsNullOrWhiteSpace(args.Label)
            ? $"checkpoint-{Session.Checkpoints.Count + 1}"
            : args.Label;
        Session.Checkpoints.Add(
            new PlaytestSession.StatusCheckpoint(label, status));
        return Result(snapshot, $"read game status ({label})", status);
    }

    private WeaponLabClient RequireClient()
        => Session.Client
           ?? throw new InvalidOperationException("Game is not running.");
}

internal sealed class GameKeyboardTool(PlaytestSession session) : PlaytestTool(session)
{
    private sealed record Args(
        [property: JsonPropertyName("keys")] string[]? Keys,
        [property: JsonPropertyName("duration_ms")] int DurationMs,
        [property: JsonPropertyName("yaw")] float Yaw,
        [property: JsonPropertyName("pitch")] float Pitch,
        [property: JsonPropertyName("checkpoint")] string? Checkpoint,
        [property: JsonPropertyName("screenshot_name")] string? ScreenshotName,
        [property: JsonPropertyName("capture_delay_ms")] int CaptureDelayMs);

    public override string Id => "game.keyboard";
    public override ToolSchema Schema => new(
        Id,
        "Hold allowlisted gameplay keys through the Fusion input pipeline, optionally capture a status checkpoint while held, then release them.",
        """{"type":"object","required":["keys","duration_ms"],"properties":{"keys":{"type":"array","items":{"type":"string","enum":["W","A","S","D","SHIFT","SPACE","R","E","MOUSE1","MOUSE2"]}},"duration_ms":{"type":"integer","minimum":16,"maximum":5000},"yaw":{"type":"number"},"pitch":{"type":"number"},"checkpoint":{"type":"string"},"screenshot_name":{"type":"string"},"capture_delay_ms":{"type":"integer","minimum":0,"maximum":5000}}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var args = Parse<Args>(toolCall);
        var keys = new HashSet<string>(
            args.Keys ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        var duration = Math.Clamp(args.DurationMs, 16, 5000);
        var input = new
        {
            moveX = (keys.Contains("D") ? 127 : 0) -
                    (keys.Contains("A") ? 127 : 0),
            moveY = (keys.Contains("W") ? 127 : 0) -
                    (keys.Contains("S") ? 127 : 0),
            fire = keys.Contains("MOUSE1"),
            ads = keys.Contains("MOUSE2"),
            reload = keys.Contains("R"),
            sprint = keys.Contains("SHIFT"),
            jump = keys.Contains("SPACE"),
            interact = keys.Contains("E"),
            yaw = args.Yaw,
            pitch = args.Pitch
        };
        await RequireClient().SendAsync(
            "input",
            input,
            cancellationToken).ConfigureAwait(false);
        string? screenshotPath = null;
        if (!string.IsNullOrWhiteSpace(args.ScreenshotName))
        {
            var captureDelay = Math.Clamp(
                args.CaptureDelayMs,
                0,
                duration);
            await Task.Delay(captureDelay, cancellationToken)
                .ConfigureAwait(false);
            screenshotPath = ArtifactPath(Path.Combine(
                "screenshots",
                Path.GetFileName(args.ScreenshotName)));
            await RequireClient().SendAsync(
                "capture",
                new
                {
                    path = screenshotPath,
                    width = 1920,
                    height = 1080
                },
                cancellationToken).ConfigureAwait(false);
            Session.Screenshots.Add(screenshotPath);
            await Task.Delay(duration - captureDelay, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await Task.Delay(duration, cancellationToken).ConfigureAwait(false);
        }
        JsonElement heldStatus = default;
        if (!string.IsNullOrWhiteSpace(args.Checkpoint))
        {
            heldStatus = await RequireClient().SendAsync(
                "status",
                new { },
                cancellationToken).ConfigureAwait(false);
            Session.Checkpoints.Add(
                new PlaytestSession.StatusCheckpoint(
                    args.Checkpoint,
                    heldStatus));
        }
        await RequireClient().SendAsync(
            "clear_input",
            new { },
            cancellationToken).ConfigureAwait(false);

        var action = $"keys={string.Join("+", keys)} duration={duration}ms";
        Session.Actions.Add(action);
        return Result(
            snapshot,
            action,
            new
            {
                keys,
                duration,
                checkpoint = args.Checkpoint,
                heldStatus,
                screenshotPath
            });
    }

    private WeaponLabClient RequireClient()
        => Session.Client
           ?? throw new InvalidOperationException("Game is not running.");
}

internal sealed class GameApproachPickupTool(PlaytestSession session)
    : PlaytestTool(session)
{
    private sealed record Args(string Weapon);

    public override string Id => "game.approach_pickup";
    public override ToolSchema Schema => new(
        Id,
        "Move the authoritative playtest player into interaction range of a named available pickup; use game.keyboard E afterward to exercise the real pickup path.",
        """{"type":"object","required":["weapon"],"properties":{"weapon":{"type":"string","enum":["Pistol","AssaultRifle","SniperRifle"]}}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var args = Parse<Args>(toolCall);
        JsonElement response = default;
        Exception? lastError = null;
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                response = await RequireClient().SendAsync(
                    "approach_pickup",
                    new { weapon = args.Weapon },
                    cancellationToken).ConfigureAwait(false);
                lastError = null;
                break;
            }
            catch (InvalidOperationException exception)
            {
                lastError = exception;
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }
        }
        if (lastError is not null)
            throw lastError;

        JsonElement replicatedStatus = default;
        var replicationDeadline =
            DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTimeOffset.UtcNow < replicationDeadline)
        {
            replicatedStatus = await RequireClient().SendAsync(
                "status",
                new { },
                cancellationToken).ConfigureAwait(false);
            var replicatedWeapon =
                replicatedStatus.TryGetProperty(
                    "weapon",
                    out var weaponElement)
                    ? weaponElement.GetString()
                    : null;
            if (string.Equals(
                    replicatedWeapon,
                    args.Weapon,
                    StringComparison.Ordinal))
            {
                break;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
        if (!replicatedStatus.TryGetProperty("weapon", out var confirmed) ||
            !string.Equals(
                confirmed.GetString(),
                args.Weapon,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"weapon replication did not confirm {args.Weapon}");
        }

        Session.Actions.Add($"approach pickup {args.Weapon}");
        return Result(
            snapshot,
            $"approached and equipped {args.Weapon}",
            new { pickup = response, status = replicatedStatus });
    }

    private WeaponLabClient RequireClient()
        => Session.Client
           ?? throw new InvalidOperationException("Game is not running.");
}

internal sealed class GameLookTool(PlaytestSession session) : PlaytestTool(session)
{
    private sealed record Args(float Yaw, float Pitch);

    public override string Id => "game.look";
    public override ToolSchema Schema => new(
        Id,
        "Set deterministic first-person yaw and pitch.",
        """{"type":"object","required":["yaw","pitch"],"properties":{"yaw":{"type":"number"},"pitch":{"type":"number","minimum":-89,"maximum":89}}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var args = Parse<Args>(toolCall);
        var response = await RequireClient().SendAsync(
            "look",
            new { yaw = args.Yaw, pitch = args.Pitch },
            cancellationToken).ConfigureAwait(false);
        Session.Actions.Add($"look yaw={args.Yaw} pitch={args.Pitch}");
        return Result(snapshot, "set FPS look", response);
    }

    private WeaponLabClient RequireClient()
        => Session.Client
           ?? throw new InvalidOperationException("Game is not running.");
}

internal sealed class GameScreenshotTool(PlaytestSession session) : PlaytestTool(session)
{
    private sealed record Args(string? Name, int Width, int Height);

    public override string Id => "game.screenshot";
    public override ToolSchema Schema => new(
        Id,
        "Capture the controlled first-person camera to an allowlisted PNG artifact.",
        """{"type":"object","required":["name"],"properties":{"name":{"type":"string"},"width":{"type":"integer"},"height":{"type":"integer"}}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var args = Parse<Args>(toolCall);
        var name = string.IsNullOrWhiteSpace(args.Name)
            ? $"capture-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.png"
            : Path.GetFileName(args.Name);
        var path = ArtifactPath(Path.Combine("screenshots", name));
        var response = await RequireClient().SendAsync(
            "capture",
            new
            {
                path,
                width = args.Width > 0 ? args.Width : 1920,
                height = args.Height > 0 ? args.Height : 1080
            },
            cancellationToken).ConfigureAwait(false);
        Session.Screenshots.Add(path);
        return Result(snapshot, $"captured {path}", response);
    }

    private WeaponLabClient RequireClient()
        => Session.Client
           ?? throw new InvalidOperationException("Game is not running.");
}

internal sealed class GameNearbyTool(PlaytestSession session) : PlaytestTool(session)
{
    public override string Id => "game.nearby";
    public override ToolSchema Schema => new(
        Id,
        "Inspect enabled renderers near the FPS camera to detect clipping/occlusion.",
        """{"type":"object","properties":{"radius":{"type":"number"}}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var radius = toolCall.Arguments.TryGetProperty("radius", out var value)
            ? value.GetSingle()
            : 2f;
        var response = await RequireClient().SendAsync(
            "nearby",
            new { radius },
            cancellationToken).ConfigureAwait(false);
        return Result(snapshot, "inspected nearby renderers", response);
    }

    private WeaponLabClient RequireClient()
        => Session.Client
           ?? throw new InvalidOperationException("Game is not running.");
}

internal sealed class DesktopScreenshotTool(PlaytestSession session) : PlaytestTool(session)
{
    private sealed record Args(string? Name);

    public override string Id => "game.screen_capture";
    public override ToolSchema Schema => new(
        Id,
        "Capture the macOS desktop as an opt-in fallback. Requires Screen Recording permission; camera-native game.screenshot is preferred.",
        """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Desktop capture is macOS-only.");
        if (!Session.ScreenRecordingVerified)
        {
            throw new UnauthorizedAccessException(
                "Screen Recording permission is required for desktop capture.");
        }

        var args = Parse<Args>(toolCall);
        var path = ArtifactPath(Path.Combine(
            "screenshots",
            Path.GetFileName(args.Name ?? "desktop.png")));
        var process = Process.Start(new ProcessStartInfo("/usr/sbin/screencapture")
        {
            UseShellExecute = false,
            ArgumentList = { "-x", path }
        }) ?? throw new InvalidOperationException("Could not start screencapture.");
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"screencapture exited {process.ExitCode}");
        Session.Screenshots.Add(path);
        return Result(snapshot, $"desktop capture {path}", new { path });
    }
}

internal sealed class PlaytestReportTool(PlaytestSession session) : PlaytestTool(session)
{
    public override string Id => "playtest.report";
    public override ToolSchema Schema => new(
        Id,
        "Write structured JSON and Markdown reports from the current game state and captured artifacts.",
        """{"type":"object","properties":{}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var status = await RequireClient().SendAsync(
            "status",
            new { },
            cancellationToken).ConfigureAwait(false);
        var nearby = await RequireClient().SendAsync(
            "nearby",
            new { radius = 2f },
            cancellationToken).ConfigureAwait(false);
        var jsonPath = ArtifactPath("report.json");
        var markdownPath = ArtifactPath("report.md");
        var ready = status.TryGetProperty("ready", out var readyElement) &&
                    readyElement.GetBoolean();
        var cameraTelemetry = status.TryGetProperty("camera", out var cameraElement)
            ? cameraElement.GetString() ?? ""
            : "";
        var checkpoints = Session.Checkpoints.ToDictionary(
            checkpoint => checkpoint.Label,
            checkpoint => checkpoint.Status,
            StringComparer.Ordinal);
        var targetDamageApplied =
            checkpoints.TryGetValue("initial", out var initialStatus) &&
            checkpoints.TryGetValue("target-damaged", out var damagedStatus) &&
            TotalPlayerHealth(damagedStatus) <
            TotalPlayerHealth(initialStatus);
        var pistolCycle = ValidateWeaponCycle(
            checkpoints,
            "pistol",
            "Pistol");
        var rifleCycle = ValidateWeaponCycle(
            checkpoints,
            "rifle",
            "AssaultRifle");
        var sniperCycle = ValidateWeaponCycle(
            checkpoints,
            "sniper",
            "SniperRifle");
        var adsTransition =
            checkpoints.TryGetValue("ads-active", out var adsStatus) &&
            NestedSingle(adsStatus, "ads", "cameraFov") < 65f &&
            NestedFlag(adsStatus, "ads", "active");
        var sprintMovement =
            checkpoints.TryGetValue("walk-active", out var walkStatus) &&
            checkpoints.TryGetValue("sprint-active", out var sprintStatus) &&
            NestedFlag(sprintStatus, "movement", "isSprinting") &&
            NestedSingle(sprintStatus, "movement", "speedMetersPerSecond") >
            NestedSingle(walkStatus, "movement", "speedMetersPerSecond") * 1.2f;
        var hitMarkerConfirmed =
            NestedInt(status, "feedback", "hitMarkerCount") > 0;
        var presentation =
            status.TryGetProperty("presentation", out var presentationElement)
                ? presentationElement
                : default;
        var animatorControllerAssigned = PresentationFlag(
            presentation,
            "animatorControllerAssigned");
        var fireAnimationConfigured = PresentationFlag(
            presentation,
            "fireAnimationParameter");
        var reloadAnimationConfigured = PresentationFlag(
            presentation,
            "reloadAnimationParameter");
        var equipAnimationConfigured = PresentationFlag(
            presentation,
            "equipAnimationParameter");
        var firstPersonArmsPresent = PresentationFlag(
            presentation,
            "firstPersonArmsPresent");
        var equippedWeaponVisualPresent = PresentationFlag(
            presentation,
            "equippedWeaponVisualPresent");
        var muzzleVfxPresent = PresentationFlag(
            presentation,
            "muzzleVfxPresent");
        var weaponAudioPresent = PresentationFlag(
            presentation,
            "weaponAudioPresent");
        var highResolution =
            !Session.VirtualPlayerMode ||
            NestedInt(status, "presentation", "renderWidth") >= 1600 &&
            NestedInt(status, "presentation", "renderHeight") >= 900;
        var highQualityAntiAliasing =
            NestedInt(status, "presentation", "antiAliasing") >= 4;
        var hdrEnabled = PresentationFlag(presentation, "hdrEnabled");
        var checks = new
        {
            gameReady = ready,
            singleFpsCamera =
                cameraTelemetry.Contains("enabledCameras=1", StringComparison.Ordinal),
            localBodyHidden =
                cameraTelemetry.Contains("bodyHidden=True", StringComparison.Ordinal),
            targetDamageApplied,
            pistolCycle,
            rifleCycle,
            sniperCycle,
            adsTransition,
            sprintMovement,
            hitMarkerConfirmed,
            animatorControllerAssigned,
            fireAnimationConfigured,
            reloadAnimationConfigured,
            equipAnimationConfigured,
            firstPersonArmsPresent,
            equippedWeaponVisualPresent,
            muzzleVfxPresent,
            weaponAudioPresent,
            highResolution,
            highQualityAntiAliasing,
            hdrEnabled,
            screenshotsCaptured =
                Session.Screenshots.Count >= 3 &&
                Session.Screenshots.All(File.Exists)
        };
        var mechanicsPassed =
            checks.gameReady &&
            checks.singleFpsCamera &&
            checks.localBodyHidden &&
            checks.targetDamageApplied &&
            checks.pistolCycle &&
            checks.rifleCycle &&
            checks.sniperCycle &&
            checks.adsTransition &&
            checks.sprintMovement &&
            checks.hitMarkerConfirmed &&
            checks.screenshotsCaptured;
        var presentationPassed =
            checks.animatorControllerAssigned &&
            checks.fireAnimationConfigured &&
            checks.reloadAnimationConfigured &&
            checks.equipAnimationConfigured &&
            checks.firstPersonArmsPresent &&
            checks.equippedWeaponVisualPresent &&
            checks.muzzleVfxPresent &&
            checks.weaponAudioPresent;
        presentationPassed =
            presentationPassed &&
            checks.highResolution &&
            checks.highQualityAntiAliasing &&
            checks.hdrEnabled;
        var passed = mechanicsPassed && presentationPassed;
        var report = new
        {
            generatedAt = DateTimeOffset.UtcNow,
            agent = "nexo-br-playtester",
            verdict = passed ? "passed" : "failed",
            mechanicsVerdict = mechanicsPassed ? "passed" : "failed",
            presentationVerdict =
                presentationPassed ? "passed" : "failed",
            checks,
            status,
            nearby,
            checkpoints = Session.Checkpoints,
            actions = Session.Actions,
            screenshots = Session.Screenshots,
            videos = Session.Videos
        };
        await File.WriteAllTextAsync(
            jsonPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            }),
            cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            markdownPath,
            $"""
            # Nexo BR Weapon Lab Playtest

            - Generated: {DateTimeOffset.UtcNow:O}
            - Verdict: **{(passed ? "PASSED" : "FAILED")}**
            - Mechanics: **{(mechanicsPassed ? "PASSED" : "FAILED")}**
            - Presentation polish: **{(presentationPassed ? "PASSED" : "FAILED")}**
            - Actions: {string.Join(", ", Session.Actions)}
            - Screenshots: {string.Join(", ", Session.Screenshots)}
            - Videos: {string.Join(", ", Session.Videos)}

            ## Final state

            ```json
            {status.GetRawText()}
            ```

            ## Nearby renderer diagnostics

            ```json
            {nearby.GetRawText()}
            ```
            """,
            cancellationToken).ConfigureAwait(false);
        return Result(
            snapshot,
            $"wrote reports {jsonPath} and {markdownPath}",
            new { jsonPath, markdownPath, passed, checks });
    }

    private WeaponLabClient RequireClient()
        => Session.Client
           ?? throw new InvalidOperationException("Game is not running.");

    private static bool ValidateWeaponCycle(
        IReadOnlyDictionary<string, JsonElement> checkpoints,
        string prefix,
        string expectedWeapon)
    {
        if (!checkpoints.TryGetValue($"{prefix}-picked", out var picked) ||
            !checkpoints.TryGetValue($"{prefix}-fired", out var fired) ||
            !checkpoints.TryGetValue($"{prefix}-reloading", out var reloading) ||
            !checkpoints.TryGetValue($"{prefix}-reloaded", out var reloaded))
        {
            return false;
        }

        return WeaponName(picked) == expectedWeapon &&
               WeaponName(fired) == expectedWeapon &&
               WeaponName(reloaded) == expectedWeapon &&
               Magazine(fired) < Magazine(picked) &&
               ShotSequence(fired) > ShotSequence(picked) &&
               RecoilMagnitude(fired) > 0 &&
               NestedString(picked, "equip", "viewmodelKind") ==
               expectedWeapon &&
               NestedSingle(
                   picked,
                   "presentation",
                   "weaponForwardAlignment") > 0.85f &&
               NestedSingle(
                   picked,
                   "presentation",
                   "muzzleForwardDistance") > 0.05f &&
               NestedInt(fired, "feedback", "fireFeedbackCount") >
               NestedInt(picked, "feedback", "fireFeedbackCount") &&
               NestedInt(fired, "feedback", "tracerSpawnCount") >
               NestedInt(picked, "feedback", "tracerSpawnCount") &&
               NestedInt(fired, "feedback", "shellEjectCount") >
               NestedInt(picked, "feedback", "shellEjectCount") &&
               NestedInt(reloading, "feedback", "reloadFeedbackCount") >
               NestedInt(fired, "feedback", "reloadFeedbackCount") &&
               IsReloading(reloading) &&
               !IsReloading(reloaded) &&
               Magazine(reloaded) > Magazine(fired);
    }

    private static bool PresentationFlag(
        JsonElement presentation,
        string name)
        => presentation.ValueKind == JsonValueKind.Object &&
           presentation.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.True;

    private static string WeaponName(JsonElement status)
        => status.TryGetProperty("weapon", out var value)
            ? value.GetString() ?? ""
            : "";

    private static int Magazine(JsonElement status)
        => status.TryGetProperty("magazine", out var value)
            ? value.GetInt32()
            : -1;

    private static bool IsReloading(JsonElement status)
        => status.TryGetProperty("reloading", out var value) &&
           value.GetBoolean();

    private static uint ShotSequence(JsonElement status)
        => status.TryGetProperty("shotSequence", out var value) &&
           value.ValueKind == JsonValueKind.Number
            ? value.GetUInt32()
            : 0;

    private static int RecoilMagnitude(JsonElement status)
    {
        var yaw =
            status.TryGetProperty("recoilYawMilliDegrees", out var yawValue) &&
            yawValue.ValueKind == JsonValueKind.Number
                ? Math.Abs(yawValue.GetInt32())
                : 0;
        var pitch =
            status.TryGetProperty(
                "recoilPitchMilliDegrees",
                out var pitchValue) &&
            pitchValue.ValueKind == JsonValueKind.Number
                ? Math.Abs(pitchValue.GetInt32())
                : 0;
        return yaw + pitch;
    }

    private static int TotalPlayerHealth(JsonElement status)
    {
        if (!status.TryGetProperty("players", out var players) ||
            players.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var total = 0;
        foreach (var player in players.EnumerateArray())
        {
            if (player.TryGetProperty("health", out var health))
                total += health.GetInt32();
        }

        return total;
    }

    private static bool NestedFlag(
        JsonElement root,
        string container,
        string name)
        => root.TryGetProperty(container, out var nested) &&
           nested.ValueKind == JsonValueKind.Object &&
           nested.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.True;

    private static int NestedInt(
        JsonElement root,
        string container,
        string name)
        => root.TryGetProperty(container, out var nested) &&
           nested.ValueKind == JsonValueKind.Object &&
           nested.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : 0;

    private static float NestedSingle(
        JsonElement root,
        string container,
        string name)
        => root.TryGetProperty(container, out var nested) &&
           nested.ValueKind == JsonValueKind.Object &&
           nested.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.Number
            ? value.GetSingle()
            : 0f;

    private static string NestedString(
        JsonElement root,
        string container,
        string name)
        => root.TryGetProperty(container, out var nested) &&
           nested.ValueKind == JsonValueKind.Object &&
           nested.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}

internal sealed class VirtualPlaytestReportTool(PlaytestSession session)
    : PlaytestTool(session)
{
    public override string Id => "playtest.virtual_report";
    public override ToolSchema Schema => new(
        Id,
        "Write the first-pass OS virtual-player report from keyboard/mouse/screen evidence.",
        """{"type":"object","properties":{}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var status = await RequireClient().SendAsync(
            "status",
            new { },
            cancellationToken).ConfigureAwait(false);
        var checkpoints = Session.Checkpoints.ToDictionary(
            checkpoint => checkpoint.Label,
            checkpoint => checkpoint.Status,
            StringComparer.Ordinal);
        var moved =
            checkpoints.TryGetValue("virtual-initial", out var initial) &&
            checkpoints.TryGetValue("virtual-after-input", out var after) &&
            PositionDistance(initial, after) > 0.2;
        var camera = status.TryGetProperty("camera", out var cameraElement)
            ? cameraElement.GetString() ?? ""
            : "";
        var presentation =
            status.TryGetProperty("presentation", out var presentationElement)
                ? presentationElement
                : default;
        var checks = new
        {
            accessibility = Session.AccessibilityVerified,
            screenRecording = Session.ScreenRecordingVerified,
            keyboardDelivered =
                Session.Actions.Any(action =>
                    action.StartsWith("os-keys=", StringComparison.Ordinal)),
            mouseDelivered =
                Session.Actions.Any(action =>
                    action.StartsWith("os-mouse", StringComparison.Ordinal)),
            mouseProducedShot =
                status.TryGetProperty("shotSequence", out var shotSequence) &&
                shotSequence.ValueKind == JsonValueKind.Number &&
                shotSequence.GetUInt32() > 0,
            playerMoved = moved,
            singleFpsCamera =
                camera.Contains("enabledCameras=1", StringComparison.Ordinal),
            localBodyHidden =
                camera.Contains("bodyHidden=True", StringComparison.Ordinal),
            animatorControllerAssigned = PresentationFlag(
                presentation,
                "animatorControllerAssigned"),
            fireAnimationConfigured = PresentationFlag(
                presentation,
                "fireAnimationParameter"),
            reloadAnimationConfigured = PresentationFlag(
                presentation,
                "reloadAnimationParameter"),
            equipAnimationConfigured = PresentationFlag(
                presentation,
                "equipAnimationParameter"),
            firstPersonArmsPresent = PresentationFlag(
                presentation,
                "firstPersonArmsPresent"),
            equippedWeaponVisualPresent = PresentationFlag(
                presentation,
                "equippedWeaponVisualPresent"),
            muzzleVfxPresent = PresentationFlag(
                presentation,
                "muzzleVfxPresent"),
            weaponAudioPresent = PresentationFlag(
                presentation,
                "weaponAudioPresent"),
            weaponForwardAligned =
                PresentationSingle(
                    presentation,
                    "weaponForwardAlignment") > 0.85f,
            muzzleAhead =
                PresentationSingle(
                    presentation,
                    "muzzleForwardDistance") > 0.05f,
            highResolution =
                PresentationSingle(presentation, "renderWidth") >= 1600f &&
                PresentationSingle(presentation, "renderHeight") >= 900f,
            highQualityAntiAliasing =
                PresentationSingle(presentation, "antiAliasing") >= 4f,
            hdrEnabled = PresentationFlag(presentation, "hdrEnabled"),
            tracerProduced = FeedbackCount(status, "tracerSpawnCount") > 0,
            shellEjected = FeedbackCount(status, "shellEjectCount") > 0,
            screenCaptures =
                Session.Screenshots.Count >= 2 &&
                Session.Screenshots.All(File.Exists)
        };
        var passed =
            checks.accessibility &&
            checks.screenRecording &&
            checks.keyboardDelivered &&
            checks.mouseDelivered &&
            checks.mouseProducedShot &&
            checks.playerMoved &&
            checks.singleFpsCamera &&
            checks.localBodyHidden &&
            checks.animatorControllerAssigned &&
            checks.fireAnimationConfigured &&
            checks.reloadAnimationConfigured &&
            checks.equipAnimationConfigured &&
            checks.firstPersonArmsPresent &&
            checks.equippedWeaponVisualPresent &&
            checks.muzzleVfxPresent &&
            checks.weaponAudioPresent &&
            checks.weaponForwardAligned &&
            checks.muzzleAhead &&
            checks.highResolution &&
            checks.highQualityAntiAliasing &&
            checks.hdrEnabled &&
            checks.tracerProduced &&
            checks.shellEjected &&
            checks.screenCaptures;
        var reportPath = ArtifactPath("report.json");
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(new
            {
                generatedAt = DateTimeOffset.UtcNow,
                agent = "nexo-br-virtual-player",
                mode = "os-input-and-screen",
                verdict = passed ? "passed" : "failed",
                checks,
                status,
                checkpoints = Session.Checkpoints,
                actions = Session.Actions,
                screenshots = Session.Screenshots
            }, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken).ConfigureAwait(false);
        return Result(
            snapshot,
            $"wrote virtual-player report {reportPath}",
            new { reportPath, passed, checks });
    }

    private WeaponLabClient RequireClient()
        => Session.Client
           ?? throw new InvalidOperationException("Game is not running.");

    private static double PositionDistance(
        JsonElement first,
        JsonElement second)
    {
        if (!first.TryGetProperty("position", out var a) ||
            !second.TryGetProperty("position", out var b))
            return 0;
        var dx = b.GetProperty("x").GetDouble() - a.GetProperty("x").GetDouble();
        var dy = b.GetProperty("y").GetDouble() - a.GetProperty("y").GetDouble();
        var dz = b.GetProperty("z").GetDouble() - a.GetProperty("z").GetDouble();
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static bool PresentationFlag(
        JsonElement presentation,
        string name)
        => presentation.ValueKind == JsonValueKind.Object &&
           presentation.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.True;

    private static int FeedbackCount(JsonElement status, string name)
        => status.TryGetProperty("feedback", out var feedback) &&
           feedback.ValueKind == JsonValueKind.Object &&
           feedback.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : 0;

    private static float PresentationSingle(
        JsonElement presentation,
        string name)
        => presentation.ValueKind == JsonValueKind.Object &&
           presentation.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.Number
            ? value.GetSingle()
            : -1f;
}

internal sealed class GameStopTool(PlaytestSession session) : PlaytestTool(session)
{
    public override string Id => "game.stop";
    public override ToolSchema Schema => new(
        Id,
        "Stop the allowlisted Weapon Lab process.",
        """{"type":"object","properties":{}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (Session.Process is null && Session.Client is not null)
        {
            await Session.Client.SendAsync(
                "clear_input",
                new { },
                cancellationToken).ConfigureAwait(false);
            await Session.Client.SendAsync(
                "release_look",
                new { },
                cancellationToken).ConfigureAwait(false);
            Session.Actions.Add("detach-editor");
            return Result(
                snapshot,
                "detached from Unity Editor Weapon Lab",
                new { detached = true });
        }

        if (Session.Client is not null)
        {
            try
            {
                await Session.Client.SendAsync(
                    "quit",
                    new { },
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Process fallback below.
            }
        }
        if (Session.Process is { HasExited: false })
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                deadline.Token);
            try
            {
                await Session.Process.WaitForExitAsync(linked.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Session.Process.Kill(entireProcessTree: true);
            }
        }
        if (Session.BuiltInCaptureMode &&
            Session.BuiltInCaptureOutputPath is { } capturePath &&
            File.Exists(capturePath) &&
            new FileInfo(capturePath).Length > 0)
        {
            Session.Videos.Add(capturePath);
        }

        Session.Actions.Add("stop");
        return Result(snapshot, "stopped Weapon Lab", new { stopped = true });
    }
}
