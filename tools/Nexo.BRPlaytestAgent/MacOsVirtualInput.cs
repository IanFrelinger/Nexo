using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Nexo.Abstractions;

namespace Nexo.BRPlaytestAgent;

internal static class MacOsVirtualInput
{
    private const string ApplicationServices =
        "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    private const string CoreGraphics =
        "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundation =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct CGPoint(double x, double y)
    {
        public readonly double X = x;
        public readonly double Y = y;
    }

    [DllImport(ApplicationServices)]
    private static extern bool AXIsProcessTrusted();

    [DllImport(CoreGraphics)]
    private static extern nint CGEventCreateKeyboardEvent(
        nint source,
        ushort virtualKey,
        bool keyDown);

    [DllImport(CoreGraphics)]
    private static extern nint CGEventCreateMouseEvent(
        nint source,
        uint mouseType,
        CGPoint mouseCursorPosition,
        uint mouseButton);

    [DllImport(CoreGraphics)]
    private static extern nint CGEventCreate(nint source);

    [DllImport(CoreGraphics)]
    private static extern CGPoint CGEventGetLocation(nint eventRef);

    [DllImport(CoreGraphics)]
    private static extern void CGEventPost(uint tap, nint eventRef);

    [DllImport(CoreGraphics)]
    private static extern bool CGPreflightScreenCaptureAccess();

    [DllImport(CoreGraphics)]
    private static extern bool CGRequestScreenCaptureAccess();

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(nint value);

    public static bool AccessibilityTrusted
        => OperatingSystem.IsMacOS() && AXIsProcessTrusted();

    public static bool ScreenRecordingTrusted
        => OperatingSystem.IsMacOS() && CGPreflightScreenCaptureAccess();

    public static bool RequestScreenRecording()
        => OperatingSystem.IsMacOS() && CGRequestScreenCaptureAccess();

    public static async Task FocusProcessAsync(
        int pid,
        CancellationToken cancellationToken)
    {
        var script =
            $"tell application \"System Events\" to set frontmost of first process whose unix id is {pid} to true";
        var process = Process.Start(new ProcessStartInfo("/usr/bin/osascript")
        {
            UseShellExecute = false,
            ArgumentList = { "-e", script }
        }) ?? throw new InvalidOperationException("Could not start osascript.");
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                "Could not focus Weapon Lab window. Grant Accessibility permission.");
    }

    public static async Task HoldKeysAsync(
        IEnumerable<string> keyNames,
        int durationMilliseconds,
        CancellationToken cancellationToken)
    {
        EnsureTrusted();
        var keyCodes = keyNames.Select(KeyCode).Distinct().ToArray();
        foreach (var code in keyCodes)
            PostKeyboard(code, keyDown: true);
        try
        {
            await Task.Delay(
                Math.Clamp(durationMilliseconds, 16, 5000),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            foreach (var code in keyCodes.Reverse())
                PostKeyboard(code, keyDown: false);
        }
    }

    public static void MoveMouse(double deltaX, double deltaY)
    {
        EnsureTrusted();
        var currentEvent = CGEventCreate(0);
        if (currentEvent == 0)
            throw new InvalidOperationException("Could not read mouse location.");
        var current = CGEventGetLocation(currentEvent);
        CFRelease(currentEvent);
        PostMouse(
            type: 5,
            new CGPoint(current.X + deltaX, current.Y + deltaY));
    }

    public static async Task LeftClickAsync(
        int durationMilliseconds,
        CancellationToken cancellationToken)
    {
        EnsureTrusted();
        var currentEvent = CGEventCreate(0);
        if (currentEvent == 0)
            throw new InvalidOperationException("Could not read mouse location.");
        var current = CGEventGetLocation(currentEvent);
        CFRelease(currentEvent);
        PostMouse(type: 1, current);
        try
        {
            await Task.Delay(
                Math.Clamp(durationMilliseconds, 16, 2000),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            PostMouse(type: 2, current);
        }
    }

    private static void EnsureTrusted()
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("OS input is macOS-only.");
        if (!AccessibilityTrusted)
        {
            throw new UnauthorizedAccessException(
                "Accessibility permission is required for the Nexo virtual player.");
        }
    }

    private static void PostKeyboard(ushort keyCode, bool keyDown)
    {
        var value = CGEventCreateKeyboardEvent(0, keyCode, keyDown);
        if (value == 0)
            throw new InvalidOperationException("Could not create keyboard event.");
        CGEventPost(0, value);
        CFRelease(value);
    }

    private static void PostMouse(uint type, CGPoint point)
    {
        var value = CGEventCreateMouseEvent(0, type, point, 0);
        if (value == 0)
            throw new InvalidOperationException("Could not create mouse event.");
        CGEventPost(0, value);
        CFRelease(value);
    }

    private static ushort KeyCode(string name)
        => name.ToUpperInvariant() switch
        {
            "A" => 0,
            "S" => 1,
            "D" => 2,
            "W" => 13,
            "E" => 14,
            "R" => 15,
            "SPACE" => 49,
            _ => throw new InvalidOperationException(
                $"OS key is not allowlisted: {name}")
        };
}

internal sealed class OsAccessTool(PlaytestSession session) : PlaytestTool(session)
{
    public override string Id => "game.os_access";
    public override ToolSchema Schema => new(
        Id,
        "Check whether macOS Accessibility input permission is available.",
        """{"type":"object","properties":{}}""");

    public override Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        Session.AccessibilityVerified =
            MacOsVirtualInput.AccessibilityTrusted;
        Session.ScreenRecordingVerified =
            MacOsVirtualInput.ScreenRecordingTrusted;
        if (!Session.ScreenRecordingVerified)
        {
            _ = MacOsVirtualInput.RequestScreenRecording();
            Session.ScreenRecordingVerified =
                MacOsVirtualInput.ScreenRecordingTrusted;
        }
        return Task.FromResult(Result(
            snapshot,
            "checked macOS accessibility",
            new
            {
                platform = OperatingSystem.IsMacOS() ? "macOS" : "other",
                accessibility = Session.AccessibilityVerified,
                screenRecording = Session.ScreenRecordingVerified
            }));
    }
}

internal sealed class OsKeyboardTool(PlaytestSession session) : PlaytestTool(session)
{
    private sealed record Args(
        [property: JsonPropertyName("keys")] string[]? Keys,
        [property: JsonPropertyName("duration_ms")] int DurationMs);

    public override string Id => "game.os_keyboard";
    public override ToolSchema Schema => new(
        Id,
        "Focus the allowlisted Weapon Lab process and hold real macOS keys W/A/S/D/E/R/SPACE.",
        """{"type":"object","required":["keys","duration_ms"],"properties":{"keys":{"type":"array","items":{"type":"string","enum":["W","A","S","D","E","R","SPACE"]}},"duration_ms":{"type":"integer","minimum":16,"maximum":5000}}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var args = Parse<Args>(toolCall);
        var process = Session.Process is { HasExited: false }
            ? Session.Process
            : throw new InvalidOperationException("Windowed game is not running.");
        await MacOsVirtualInput.FocusProcessAsync(
            process.Id,
            cancellationToken).ConfigureAwait(false);
        await MacOsVirtualInput.HoldKeysAsync(
            args.Keys ?? Array.Empty<string>(),
            args.DurationMs,
            cancellationToken).ConfigureAwait(false);
        var action =
            $"os-keys={string.Join("+", args.Keys ?? Array.Empty<string>())} " +
            $"duration={args.DurationMs}ms";
        Session.Actions.Add(action);
        return Result(snapshot, action, new { delivered = true });
    }
}

internal sealed class OsMouseTool(PlaytestSession session) : PlaytestTool(session)
{
    private sealed record Args(
        [property: JsonPropertyName("delta_x")] double DeltaX,
        [property: JsonPropertyName("delta_y")] double DeltaY,
        bool Click,
        [property: JsonPropertyName("duration_ms")] int DurationMs);

    public override string Id => "game.os_mouse";
    public override ToolSchema Schema => new(
        Id,
        "Focus the Weapon Lab and emit real macOS mouse movement and/or a left click.",
        """{"type":"object","properties":{"delta_x":{"type":"number"},"delta_y":{"type":"number"},"click":{"type":"boolean"},"duration_ms":{"type":"integer","minimum":16,"maximum":2000}}}""");

    public override async Task<ToolResult> InvokeAsync(
        ToolCall toolCall,
        WorldSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var args = Parse<Args>(toolCall);
        var process = Session.Process is { HasExited: false }
            ? Session.Process
            : throw new InvalidOperationException("Windowed game is not running.");
        await MacOsVirtualInput.FocusProcessAsync(
            process.Id,
            cancellationToken).ConfigureAwait(false);
        if (args.DeltaX != 0 || args.DeltaY != 0)
            MacOsVirtualInput.MoveMouse(args.DeltaX, args.DeltaY);
        if (args.Click)
            await MacOsVirtualInput.LeftClickAsync(
                args.DurationMs > 0 ? args.DurationMs : 250,
                cancellationToken).ConfigureAwait(false);
        Session.Actions.Add(
            $"os-mouse dx={args.DeltaX} dy={args.DeltaY} click={args.Click}");
        return Result(snapshot, "emitted OS mouse input", new { delivered = true });
    }
}
