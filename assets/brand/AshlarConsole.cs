// AshlarConsole.cs — terminal styling for the ashlar CLI
// Palette: ink #2B2420 · cream #F7F2E5 · sage #7E8F6E · gold #D1A23C · clay #C96F4A · olive #8A8069
//
// Rules of the road (see ashlar-terminal-style.md):
//   gold  = certification truth only (never decoration)
//   clay  = needs human attention (warn / fail-closed)
//   sage  = structure: rules, chain, info glyphs
//   olive = dim context (timings, hashes, annotations)
//
// Respects NO_COLOR and output redirection automatically.

using System;
using System.Runtime.InteropServices;

namespace Ashlar.Cli;

public static class AshlarConsole
{
    // ---- palette -----------------------------------------------------------
    public const string Reset = "\x1b[0m";
    public const string Italic = "\x1b[3m";

    public static readonly string Cream = Fg(0xF7, 0xF2, 0xE5);
    public static readonly string Sage  = Fg(0x7E, 0x8F, 0x6E);
    public static readonly string Gold  = Fg(0xD1, 0xA2, 0x3C);
    public static readonly string Clay  = Fg(0xC9, 0x6F, 0x4A);
    public static readonly string Olive = Fg(0x8A, 0x80, 0x69);

    static string Fg(byte r, byte g, byte b) => $"\x1b[38;2;{r};{g};{b}m";

    /// <summary>Color on iff we're interactive and NO_COLOR is unset.</summary>
    public static bool UseColor { get; set; } =
        !Console.IsOutputRedirected &&
        Environment.GetEnvironmentVariable("NO_COLOR") is null;

    static string C(string style) => UseColor ? style : string.Empty;
    static string R() => UseColor ? Reset : string.Empty;

    // ---- banner ------------------------------------------------------------
    // N E X in cream; the O ring in gold with a cream node-dot center —
    // the terminal translation of the node-"o" wordmark.
    public static void Banner(string? version = null)
    {
        string cr = C(Cream), gd = C(Gold), cl = C(Clay), ol = C(Olive), r = R();

        Console.WriteLine();
        Console.WriteLine($"                                {gd}~{r}   {gd}+{r}                             {cl}*{r}");
        Console.WriteLine($"  {cr}███╗   ██╗███████╗██╗  ██╗ {r}{gd} ██████╗ {r}");
        Console.WriteLine($"  {cr}████╗  ██║██╔════╝╚██╗██╔╝ {r}{gd}██╔═══██╗{r}");
        Console.WriteLine($"  {cr}██╔██╗ ██║█████╗   ╚███╔╝  {r}{gd}██║ {r}{cr}█{r}{gd} ██║{r}");
        Console.WriteLine($"  {cr}██║╚██╗██║██╔══╝   ██╔██╗  {r}{gd}██║ {r}{cr}█{r}{gd} ██║{r}");
        Console.WriteLine($"  {cr}██║ ╚████║███████╗██╔╝ ██╗ {r}{gd}╚██████╔╝{r}");
        Console.WriteLine($"  {cr}╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝ {r}{gd} ╚═════╝ {r}");
        Console.WriteLine($"                              {C(Clay + Italic)}↳ certified!{r}");
        Console.WriteLine();
        var v = version is null ? "" : $"{C(Olive)} · v{version}{r}";
        Console.WriteLine($"  {cr}adaptive orchestration{r}{v}");
        Console.WriteLine();
    }

    /// <summary>The layer chain: ●────●────◉  brick → behavior → agent</summary>
    public static void Chain()
    {
        Console.WriteLine(
            $"  {C(Sage)}●────────●────────{R()}{C(Gold)}◉{R()}   " +
            $"{C(Olive)}brick → behavior → agent{R()}");
    }

    // ---- status lines ------------------------------------------------------
    const int Width = 68;

    static void Line(string glyph, string glyphStyle, string msg, string msgStyle,
                     string ctx = "", string right = "", string rightStyle = "")
    {
        int pad = Math.Max(Width - (2 + glyph.Length + 1 + msg.Length + ctx.Length + right.Length), 1);
        Console.WriteLine(
            $"  {C(glyphStyle)}{glyph}{R()} {C(msgStyle)}{msg}{R()}" +
            (ctx.Length > 0 ? $"{C(Olive)}{ctx}{R()}" : "") +
            new string(' ', pad) +
            (right.Length > 0 ? $"{C(rightStyle)}{right}{R()}" : ""));
    }

    public static void Info(string msg, string ctx = "", string right = "")
        => Line("●", Sage, msg, Cream, ctx, right, Olive);

    public static void Ok(string msg, string ctx = "")
        => Line("✓", Gold, msg, Cream, ctx);

    /// <summary>Reserved for certification results only.</summary>
    public static void Certified(string msg, string right = "")
        => Line("◉", Gold, msg, Gold, "", right, Olive);

    public static void Warn(string msg, string ctx = "")
        => Line("▲", Clay, msg, Cream, ctx);

    /// <summary>Rejections / fail-closed outcomes.</summary>
    public static void Error(string msg, string ctx = "")
        => Line("×", Clay, msg, Clay, ctx);

    public static void Pending(string msg, string ctx = "")
        => Line("○", Olive, msg, Olive, ctx);

    public static void Rule(string title)
    {
        string bar = new string('─', Math.Max(Width - title.Length - 6, 4));
        Console.WriteLine($"  {C(Sage)}── {title} {bar}{R()}");
    }

    // ---- spinner frames (braille, sage) ------------------------------------
    public static readonly string[] SpinnerFrames =
        { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };

    // ---- windows legacy console --------------------------------------------
    // Windows Terminal and modern conhost handle ANSI already; call this once
    // at startup to cover older hosts.
    public static void EnableVirtualTerminal()
    {
        if (!OperatingSystem.IsWindows()) return;
        var handle = GetStdHandle(-11); // STD_OUTPUT_HANDLE
        if (GetConsoleMode(handle, out uint mode))
            SetConsoleMode(handle, mode | 0x0004); // ENABLE_VIRTUAL_TERMINAL_PROCESSING
    }

    [DllImport("kernel32.dll")] static extern IntPtr GetStdHandle(int nStdHandle);
    [DllImport("kernel32.dll")] static extern bool GetConsoleMode(IntPtr h, out uint mode);
    [DllImport("kernel32.dll")] static extern bool SetConsoleMode(IntPtr h, uint mode);
}

/* usage:

AshlarConsole.EnableVirtualTerminal();
AshlarConsole.Banner("0.7.0");
AshlarConsole.Chain();

AshlarConsole.Rule("boot");
AshlarConsole.Info("loaded 42 bricks", right: "12ms");
AshlarConsole.Info("composed 7 behaviors", right: "4ms");
AshlarConsole.Certified("certification chain verified", right: "ed25519 ✓");
AshlarConsole.Warn("skills approval pending", " · human gate");
AshlarConsole.Error("rejected unsigned brick", " · fail-closed");
AshlarConsole.Rule("ready");
AshlarConsole.Certified("ashlar is certified & listening");

*/
