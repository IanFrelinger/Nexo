using System.CommandLine;
using System.CommandLine.Invocation;
using Ashlar.Manifest.Signing;

namespace Ashlar.CLI.Commands;

/// <summary>
/// <c>ashlar keys</c> — the operator's local signing identity (SPEC-006 v1).
///
/// <para><c>keys init</c> generates the single Ed25519 operator keypair; <c>keys show</c>
/// prints its fingerprint, or says plainly there is none. Signing is <strong>presence-
/// activated</strong>: once a key exists, the gate signs every verdict it records; with no
/// key the system stays honestly unsigned and zero-setup still works. The private seed lives
/// only under the key directory (<c>ASHLAR_KEY_DIR</c> or <c>~/.ashlar/keys</c>) and never
/// enters a project, a bundle, or any command output — this verb prints the PUBLIC fingerprint
/// and nothing more.</para>
/// </summary>
public sealed class KeysCommand : Command
{
    /// <summary>Creates a new KeysCommand instance.</summary>
    public KeysCommand() : base("keys", "Manage the local operator signing key (SPEC-006).")
    {
        AddCommand(BuildInit());
        AddCommand(BuildShow());
    }

    private static Option<string?> KeyDirOption() => new(
        name: "--key-dir",
        description: "Key directory (defaults to $ASHLAR_KEY_DIR, else ~/.ashlar/keys).");

    private static Command BuildInit()
    {
        var rotateOpt = new Option<bool>(
            name: "--rotate",
            description: "Replace an existing key. The old PUBLIC key is retained under trusted/ so records it "
                       + "already signed keep verifying.");
        var keyDirOpt = KeyDirOption();
        var cmd = new Command("init", "Generate the operator signing keypair.") { rotateOpt, keyDirOpt };
        cmd.SetHandler((InvocationContext ctx) =>
        {
            ctx.ExitCode = Init(
                ctx.ParseResult.GetValueForOption(rotateOpt),
                ctx.ParseResult.GetValueForOption(keyDirOpt));
        });
        return cmd;
    }

    private static int Init(bool rotate, string? keyDir)
    {
        var dir = string.IsNullOrWhiteSpace(keyDir) ? OperatorKey.ResolveKeyDir() : Path.GetFullPath(keyDir);
        try
        {
            var id = OperatorKey.Generate(dir, rotate);
            Console.WriteLine($"  {Gold(rotate ? "✓ operator key rotated" : "✓ operator key ready")}");
            Console.WriteLine($"  {Dim($"fingerprint {id.Fingerprint}")}");
            Console.WriteLine($"  {Dim($"stored in {dir}")}");
            Console.WriteLine();
            Console.WriteLine($"  {Dim("gate decisions on this machine are now signed. keep the private key here — never commit it.")}");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            // The key already exists and --rotate was not given: the kernel's refusal, verbatim.
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static Command BuildShow()
    {
        var keyDirOpt = KeyDirOption();
        var cmd = new Command("show", "Print the operator key fingerprint, or note that none exists.") { keyDirOpt };
        cmd.SetHandler((InvocationContext ctx) =>
        {
            ctx.ExitCode = Show(ctx.ParseResult.GetValueForOption(keyDirOpt));
        });
        return cmd;
    }

    private static int Show(string? keyDir)
    {
        var dir = string.IsNullOrWhiteSpace(keyDir) ? OperatorKey.ResolveKeyDir() : Path.GetFullPath(keyDir);
        SigningIdentity? id;
        try
        {
            id = OperatorKey.TryLoad(dir);
        }
        catch (InvalidOperationException ex)
        {
            // A corrupt key pair (e.g. a crash mid-rotation) must not be papered over: signing
            // with a mismatched identity is exactly what poisons the store. Surface it and stop.
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        if (id is null)
        {
            // S-3: never print a fingerprint for a key that is not there. Say plainly there is none.
            Console.WriteLine($"  {Dim("no operator key — gate decisions are recorded unsigned.")}");
            Console.WriteLine($"  {Dim("create one with:  ashlar keys init")}");
            return 0;
        }

        Console.WriteLine($"  {Gold("operator key")}  {Dim(id.Fingerprint)}");
        Console.WriteLine($"  {Dim($"stored in {dir}")}");
        return 0;
    }

    // Same colour discipline as the other verbs: gold marks the identity, dim is detail.
    private static readonly bool Color =
        Environment.GetEnvironmentVariable("NO_COLOR") is null && !Console.IsOutputRedirected;
    private static string Paint(string ansi, string t) => Color ? $"\x1b[{ansi}m{t}\x1b[0m" : t;
    private static string Gold(string t) => Paint("33", t);
    private static string Dim(string t) => Paint("90", t);
}
