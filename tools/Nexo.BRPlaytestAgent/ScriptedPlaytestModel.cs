using System.Text.Json;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Models;

namespace Nexo.BRPlaytestAgent;

/// <summary>
/// BR Weapon Lab scenario factory over <see cref="DeterministicToolSequenceModel"/>.
/// </summary>
internal sealed class ScriptedPlaytestModel : IModel
{
    private readonly IModel _inner;

    public ScriptedPlaytestModel(
        bool virtualPlayer = false,
        bool recordVideo = false)
    {
        _inner = virtualPlayer
            ? new DeterministicToolSequenceModel(BuildVirtualPlayerTurns())
            : new DeterministicToolSequenceModel(BuildSemanticTurns(recordVideo));
    }

    public Task<ModelOutput> CompleteAsync(
        ModelInput input,
        CancellationToken cancellationToken)
        => _inner.CompleteAsync(input, cancellationToken);

    private static IReadOnlyList<ToolSequenceTurn> BuildSemanticTurns(bool recordVideo)
        =>
        [
            Turn("Launch the allowlisted Weapon Lab build.",
                Call("game.launch", new { })),
            Turn("Record the initial loadout and target state.",
                Call("game.status", new
                {
                    label = recordVideo
                        ? "camera-capture-active"
                        : "capture-not-requested"
                }),
                Call("game.status", new { label = "initial" })),
            Turn("Verify authoritative hit and damage using the starting rifle.",
                Call("game.look", new { yaw = 0, pitch = 0 }),
                Call("game.keyboard", new
                {
                    keys = new[] { "W" },
                    duration_ms = 250,
                    yaw = 0,
                    pitch = 0,
                    checkpoint = "walk-active"
                }),
                Call("game.keyboard", new
                {
                    keys = new[] { "W", "SHIFT" },
                    duration_ms = 250,
                    yaw = 0,
                    pitch = 0,
                    checkpoint = "sprint-active",
                    screenshot_name = "showcase-01-sprint.png",
                    capture_delay_ms = 180
                }),
                Call("game.keyboard", new
                {
                    keys = new[] { "S" },
                    duration_ms = 500,
                    yaw = 0,
                    pitch = 0
                }),
                Call("game.keyboard", new
                {
                    keys = new[] { "MOUSE2" },
                    duration_ms = 350,
                    yaw = 0,
                    pitch = 0,
                    checkpoint = "ads-active",
                    screenshot_name = "showcase-02-rifle-ads.png",
                    capture_delay_ms = 300
                }),
                Call("game.keyboard", new
                {
                    keys = new[] { "MOUSE1", "MOUSE2" },
                    duration_ms = 400,
                    yaw = 0,
                    pitch = 0,
                    screenshot_name = "showcase-03-rifle-ads-fire.png",
                    capture_delay_ms = 35
                }),
                Call("game.status", new { label = "target-damaged" })),
            Turn("Move into pistol pickup range and interact.",
                Call("game.look", new { yaw = 0, pitch = 0 }),
                Call("game.keyboard", new
                {
                    keys = new[] { "W" },
                    duration_ms = 300,
                    yaw = 0,
                    pitch = 0
                }),
                Call("game.approach_pickup", new { weapon = "Pistol" }),
                Call("game.status", new { label = "pistol-picked" })),
            Turn("Exercise pistol fire, reload start/completion, and capture.",
                Call("game.keyboard", new
                {
                    keys = Array.Empty<string>(),
                    duration_ms = 400,
                    yaw = 0,
                    pitch = 0
                }),
                Call("game.keyboard", new
                {
                    keys = new[] { "MOUSE1" },
                    duration_ms = 600,
                    yaw = 0,
                    pitch = 0,
                    screenshot_name = "showcase-04-pistol-fire.png",
                    capture_delay_ms = 35
                }),
                Call("game.status", new { label = "pistol-fired" }),
                Call("game.keyboard", new
                {
                    keys = new[] { "R" },
                    duration_ms = 400,
                    yaw = 0,
                    pitch = 0,
                    screenshot_name = "showcase-05-pistol-reload.png",
                    capture_delay_ms = 120
                }),
                Call("game.status", new { label = "pistol-reloading" }),
                Call("game.keyboard", new
                {
                    keys = Array.Empty<string>(),
                    duration_ms = 1200,
                    yaw = 0,
                    pitch = 0
                }),
                Call("game.status", new { label = "pistol-reloaded" }),
                Call("game.screenshot", new
                {
                    name = "pistol-cycle.png",
                    width = 1920,
                    height = 1080
                })),
            Turn("Pick up and identify the assault rifle.",
                Call("game.keyboard", new
                {
                    keys = new[] { "D", "W" },
                    duration_ms = 300,
                    yaw = 0,
                    pitch = 0
                }),
                Call("game.approach_pickup", new { weapon = "AssaultRifle" }),
                Call("game.status", new { label = "rifle-picked" })),
            Turn("Exercise assault-rifle fire and full reload cycle.",
                Call("game.keyboard", new
                {
                    keys = new[] { "MOUSE1" },
                    duration_ms = 400,
                    yaw = 0,
                    pitch = 0,
                    screenshot_name = "showcase-06-rifle-fire.png",
                    capture_delay_ms = 35
                }),
                Call("game.status", new { label = "rifle-fired" }),
                Call("game.keyboard", new
                {
                    keys = new[] { "R" },
                    duration_ms = 400,
                    yaw = 0,
                    pitch = 0,
                    screenshot_name = "showcase-07-rifle-reload.png",
                    capture_delay_ms = 120
                }),
                Call("game.status", new { label = "rifle-reloading" }),
                Call("game.keyboard", new
                {
                    keys = Array.Empty<string>(),
                    duration_ms = 1800,
                    yaw = 0,
                    pitch = 0
                }),
                Call("game.status", new { label = "rifle-reloaded" }),
                Call("game.screenshot", new
                {
                    name = "rifle-cycle.png",
                    width = 1920,
                    height = 1080
                })),
            Turn("Move to and pick up the sniper rifle.",
                Call("game.keyboard", new
                {
                    keys = new[] { "D", "W" },
                    duration_ms = 500,
                    yaw = 0,
                    pitch = 0
                }),
                Call("game.approach_pickup", new { weapon = "SniperRifle" }),
                Call("game.status", new { label = "sniper-picked" })),
            Turn("Exercise sniper fire/reload, inspect clipping, and capture.",
                Call("game.keyboard", new
                {
                    keys = new[] { "MOUSE1" },
                    duration_ms = 400,
                    yaw = 0,
                    pitch = 0,
                    screenshot_name = "showcase-08-sniper-fire.png",
                    capture_delay_ms = 35
                }),
                Call("game.status", new { label = "sniper-fired" }),
                Call("game.keyboard", new
                {
                    keys = new[] { "R" },
                    duration_ms = 400,
                    yaw = 0,
                    pitch = 0,
                    screenshot_name = "showcase-09-sniper-reload.png",
                    capture_delay_ms = 120
                }),
                Call("game.status", new { label = "sniper-reloading" }),
                Call("game.keyboard", new
                {
                    keys = Array.Empty<string>(),
                    duration_ms = 2400,
                    yaw = 0,
                    pitch = 0
                }),
                Call("game.status", new { label = "sniper-reloaded" }),
                Call("game.look", new { yaw = 35, pitch = 0 }),
                Call("game.screenshot", new
                {
                    name = "sniper-cycle.png",
                    width = 1920,
                    height = 1080
                }),
                Call("game.nearby", new { radius = 2 })),
            Turn("Write structured playtest evidence.",
                Call("game.status", new
                {
                    label = recordVideo
                        ? "camera-capture-finalizing"
                        : "video-not-requested"
                }),
                Call("playtest.report", new { })),
            Turn("Stop the game after evidence is durable.",
                Call("game.stop", new { }))
        ];

    private static IReadOnlyList<ToolSequenceTurn> BuildVirtualPlayerTurns()
        =>
        [
            Turn("Launch the real windowed Weapon Lab.",
                Call("game.launch", new { })),
            Turn("Check OS access and capture the initial desktop/state.",
                Call("game.os_access", new { }),
                Call("game.status", new { label = "virtual-initial" }),
                Call("game.screen_capture", new { name = "virtual-initial.png" })),
            Turn("Move forward using real keyboard input.",
                Call("game.os_keyboard", new
                {
                    keys = new[] { "W" },
                    duration_ms = 800
                })),
            Turn("Turn and fire using real mouse events.",
                Call("game.os_mouse", new
                {
                    delta_x = 140,
                    delta_y = 0,
                    click = true,
                    duration_ms = 300
                })),
            Turn("Exercise physical reload and interaction keys.",
                Call("game.os_keyboard", new
                {
                    keys = new[] { "R" },
                    duration_ms = 250
                }),
                Call("game.os_keyboard", new
                {
                    keys = new[] { "E" },
                    duration_ms = 250
                })),
            Turn("Capture post-input desktop and structured state.",
                Call("game.status", new { label = "virtual-after-input" }),
                Call("game.screen_capture", new { name = "virtual-after-input.png" }),
                Call("game.screenshot", new
                {
                    name = "virtual-camera.png",
                    width = 1920,
                    height = 1080
                })),
            Turn("Write the OS virtual-player evidence report.",
                Call("playtest.virtual_report", new { })),
            Turn("Stop the virtual-player game.",
                Call("game.stop", new { }))
        ];

    private static ToolSequenceTurn Turn(
        string rationale,
        params ToolSequenceStep[] steps)
        => new(rationale, steps);

    private static ToolSequenceStep Call(string id, object arguments)
        => ToolSequenceStep.FromObject(id, arguments);
}
