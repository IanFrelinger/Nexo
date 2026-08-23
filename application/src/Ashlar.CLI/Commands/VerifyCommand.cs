using System.CommandLine;
using System.CommandLine.Invocation;
using Ashlar.Manifest;

namespace Ashlar.CLI.Commands;

/// <summary>
/// <c>ashlar verify</c> — run the courses against the project in the current directory and
/// render the wall.
///
/// <para>Thin by design: every judgement lives kernel-side in <see cref="ProjectVerifier"/>;
/// this class reads the two documents, renders course results in the fixed vocabulary
/// (SPEC-009: gold marks verification only, green is a per-course pass, red is failure,
/// NO_COLOR honoured), and maps the outcome to exit codes — 0 verified, 65 verification
/// failed, 1 usage/environment.</para>
///
/// <para>The verdict word is VERIFIED, not CERTIFIED: certified means signed, and nothing
/// signs yet. When real keys land, the provenance course and the signature line appear here;
/// until then this command prints <c>unsigned</c> and nothing else.</para>
/// </summary>
public sealed class VerifyCommand : Command
{
    /// <summary>Creates a new VerifyCommand instance.</summary>
    public VerifyCommand() : base("verify", "Run the courses against this project and render the wall.")
    {
        var pathOpt = new Option<DirectoryInfo>(
            name: "--path",
            description: "Project directory containing ashlar.yaml and ashlar.policy.yaml (defaults to current).",
            getDefaultValue: () => new DirectoryInfo(Environment.CurrentDirectory));

        AddOption(pathOpt);

        this.SetHandler((InvocationContext ctx) =>
        {
            var path = ctx.ParseResult.GetValueForOption(pathOpt)!;
            ctx.ExitCode = Execute(path);
        });
    }

    private const int ExitVerified = 0;
    private const int ExitVerificationFailed = 65;
    private const int ExitUsage = 1;

    private static int Execute(DirectoryInfo directory)
    {
        var manifestPath = Path.Combine(directory.FullName, "ashlar.yaml");
        var policyPath = Path.Combine(directory.FullName, "ashlar.policy.yaml");

        var missing = new[] { manifestPath, policyPath }.Where(p => !File.Exists(p)).ToList();
        if (missing.Count > 0)
        {
            Console.Error.WriteLine(
                "not an ashlar project: missing "
                + string.Join(", ", missing.Select(Path.GetFileName))
                + $" in {directory.FullName}");
            Console.Error.WriteLine("start one with:  ashlar init <name>");
            return ExitUsage;
        }

        var result = ProjectVerifier.Verify(
            File.ReadAllText(manifestPath),
            File.ReadAllText(policyPath),
            directory.FullName);

        Render(result);
        return result.Verified ? ExitVerified : ExitVerificationFailed;
    }

    // ── rendering ───────────────────────────────────────────────────────────

    private static readonly bool Color =
        Environment.GetEnvironmentVariable("NO_COLOR") is null && !Console.IsOutputRedirected;

    private static string Paint(string ansi, string text) =>
        Color ? $"\x1b[{ansi}m{text}\x1b[0m" : text;

    private static string Ok(string t) => Paint("32", t);      // green — per-course pass
    private static string Bad(string t) => Paint("31", t);     // red — failure
    private static string Gold(string t) => Paint("33", t);    // gold — the verdict, nothing else
    private static string Dim(string t) => Paint("90", t);

    private static void Render(ProjectVerification result)
    {
        Console.WriteLine();
        var index = 1;
        foreach (var course in result.Courses)
        {
            var glyph = course.Passed ? Ok("✓") : Bad("×");
            var label = $"course {index} · {course.Name}";
            Console.WriteLine($"  {glyph} {label,-26} {Dim(course.Detail)}");
            index++;
        }

        Console.WriteLine();
        if (result.Verified)
        {
            // VERIFIED, not CERTIFIED: no signature exists to claim. The unsigned note is
            // load-bearing honesty, not a TODO.
            Console.WriteLine(
                $"  {Gold("✓ VERIFIED")}  {Dim($"{result.Courses.Count} courses · unsigned — signing arrives with the ledger")}");
        }
        else
        {
            var failed = result.Courses.First(c => !c.Passed);
            Console.WriteLine($"  {Bad("× FAILED")}  {Dim($"course '{failed.Name}': {failed.Detail}")}");
            Console.WriteLine($"  {Dim("exit 65")}");
        }
    }
}
