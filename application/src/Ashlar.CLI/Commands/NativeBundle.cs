using System.Text.Json;
using Ashlar.Manifest;
using Ashlar.Manifest.Ledger;
using Ashlar.Manifest.Signing;

namespace Ashlar.CLI.Commands;

/// <summary>What a project is, for the purpose of shipping it: its name, whether it verifies,
/// and — if it was ever certified — the signer and how many signed entries its ledger holds.</summary>
public sealed record BundleInfo(
    string Name,
    string Rid,
    bool Verified,
    bool Certified,
    string? SignerFingerprint,
    int LedgerEntries,
    // What the verdict this bundle carries actually covers. A bundle that ships a CERTIFIED banner
    // over a project with no code in it is the same silent overclaim `ashlar verify` used to make;
    // the scope travels with the bundle so the person who unzips it reads the same honest line.
    CertificationScope? Scope = null);

/// <summary>
/// Stages a governed Ashlar application into a portable, self-proving bundle: the project (its
/// contract, its operator-owned policy, its signed ledger, its bricks) laid out beside a launcher
/// that <em>verifies before it runs</em>. Drop the self-contained runtime next to it (the command
/// publishes one) and the whole folder is a download-and-run agentic application — it proves its
/// own certification, offline, on first launch.
///
/// <para>The staging is deterministic and does no network or build work, so it is unit-tested on
/// its own; producing the native runtime binary is a separate <c>dotnet publish</c> step.</para>
/// </summary>
public static class NativeBundle
{
    /// <summary>The bundle format tag.</summary>
    public const string Format = "ashlar-native/v1";

    /// <summary>
    /// Reads what the project is without changing it: does it verify, was it certified (an intact
    /// signed ledger), and by whom. A corrupt ledger surfaces as not-certified here and fails loudly
    /// at <c>verify</c> time; this describe step never throws for it.
    /// </summary>
    public static BundleInfo Describe(string projectDir, string rid)
    {
        var manifestYaml = File.ReadAllText(Path.Combine(projectDir, "ashlar.yaml"));
        var policyYaml = File.ReadAllText(Path.Combine(projectDir, "ashlar.policy.yaml"));
        var verification = ProjectVerifier.Verify(manifestYaml, policyYaml, projectDir);
        var name = ManifestLoader.TryLoad(manifestYaml, out var manifest, out _)
            ? manifest!.Metadata.Name
            : "app";

        var certified = false;
        string? fingerprint = null;
        var ledgerEntries = 0;
        try
        {
            var chain = new InstanceLedger(Path.Combine(projectDir, ".ashlar")).VerifyChain();
            ledgerEntries = chain.Count;
            // Certified only when the signed head attests THESE exact documents — an intact chain
            // over a since-edited contract is not a certification of what we are about to ship.
            certified = chain.Count > 0
                && string.Equals(chain.Head?.Subject, InstanceLedger.Subject(manifestYaml, policyYaml), StringComparison.Ordinal);
            if (chain.Head?.Signer is { } signer)
            {
                fingerprint = OperatorKey.Fingerprint(Convert.FromBase64String(signer));
            }
        }
        catch (InvalidOperationException)
        {
            // A corrupt ledger is not certified; verify will refuse it at run time.
        }

        return new BundleInfo(name, rid, verification.Verified, certified, fingerprint, ledgerEntries, verification.Scope);
    }

    /// <summary>
    /// Stages the governed app itself into <paramref name="bundleDir"/>/app — its contract, its
    /// operator-owned policy, its signed ledger (the proof it carries), and its bricks. Copies only
    /// these, never bin/obj/.git or the output itself. Shared by every exporter (native, cloud):
    /// what travels is the same regardless of where it lands.
    ///
    /// <para>What is deliberately EXCLUDED from <c>.ashlar/</c>: <c>keys/</c> (an operator who
    /// pointed <c>ASHLAR_KEY_DIR</c> inside the project must never find their PRIVATE key inside a
    /// shipped bundle — SPEC-006's first rule), <c>forge/</c> (raw held/rejected proposal content
    /// is working state, not something to distribute), and lock/temp files. The gates records and
    /// the signed ledger — the governance history the bundle exists to carry — stay.</para>
    /// </summary>
    public static List<string> StageApp(string projectDir, string bundleDir)
    {
        var appDir = Path.Combine(bundleDir, "app");
        Directory.CreateDirectory(appDir);
        var written = new List<string>();
        CopyFile(Path.Combine(projectDir, "ashlar.yaml"), Path.Combine(appDir, "ashlar.yaml"), written, bundleDir);
        CopyFile(Path.Combine(projectDir, "ashlar.policy.yaml"), Path.Combine(appDir, "ashlar.policy.yaml"), written, bundleDir);
        CopyTreeIfPresent(Path.Combine(projectDir, ".ashlar"), Path.Combine(appDir, ".ashlar"), written, bundleDir,
            exclude: rel =>
            {
                var top = rel.Split('/', '\\')[0];
                if (string.Equals(top, "keys", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(top, "forge", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                var name = Path.GetFileName(rel);
                return name is ".lock" || name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".key", StringComparison.OrdinalIgnoreCase);
            });
        // src/ is inside the project too: the same operator who pointed ASHLAR_KEY_DIR at
        // .ashlar/keys can point it at src/keys, and build output is not cargo. Same rule,
        // same filter shape as the .ashlar tree above.
        CopyTreeIfPresent(Path.Combine(projectDir, "src"), Path.Combine(appDir, "src"), written, bundleDir,
            exclude: rel =>
            {
                var parts = rel.Split('/', '\\');
                if (parts[0] is "bin" or "obj" or ".git")
                {
                    return true;
                }
                if (parts.Any(p => string.Equals(p, "keys", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                var name = Path.GetFileName(rel);
                return name is ".lock" || name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".key", StringComparison.OrdinalIgnoreCase);
            });
        StageDeclaredBrickSources(projectDir, appDir, written, bundleDir);
        return written;
    }

    /// <summary>
    /// Stages the source of every brick the manifest DECLARES, wherever in the project it lives.
    ///
    /// <para>The composition course resolves declared bricks by scanning the whole project tree;
    /// this staging used to copy <c>src/</c> and nothing else. A brick implemented anywhere else —
    /// <c>bricks/</c>, <c>libs/</c>, a file at the project root — therefore certified at the source
    /// and was then DROPPED from the bundle, and the bundle's own launcher, which begins with
    /// <c>verify --path app</c>, failed course 2 and exited 65 on every machine. An export that
    /// reports CERTIFIED over an application that cannot start is worse than one that reports
    /// nothing, so what the courses resolve is what travels.</para>
    ///
    /// <para>The unit staged is the resolved file's TOP-LEVEL directory under the project, not the
    /// file: a brick is a project (a csproj, a README, nested folders), and carrying its .cs files
    /// out of the layout that resolved them would satisfy the course while shipping something no
    /// one can build. A resolved file sitting directly in the project root is carried alone,
    /// because its "directory" is the project.</para>
    /// </summary>
    private static void StageDeclaredBrickSources(string projectDir, string appDir, List<string> written, string bundleRoot)
    {
        string manifestYaml;
        try
        {
            manifestYaml = File.ReadAllText(Path.Combine(projectDir, "ashlar.yaml"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return;   // Describe() already read it; a race here is not this method's to report.
        }

        if (!ManifestLoader.TryLoad(manifestYaml, out var manifest, out _) || manifest!.Bricks.Count == 0)
        {
            return;
        }

        var inventory = BrickSourceResolver.Scan(projectDir);
        // Already staged above, by their own rules. Restaging them would duplicate work and, for
        // .ashlar/, would defeat the keys/forge exclusions that protect the operator's private key.
        var alreadyStaged = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".ashlar", "src" };
        var carrierDirectories = new SortedSet<string>(StringComparer.Ordinal);
        var looseFiles = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var brick in manifest.Bricks)
        {
            foreach (var file in BrickSourceResolver.Resolve(inventory, brick.Id))
            {
                var rel = Path.GetRelativePath(projectDir, file);
                if (rel.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(rel))
                {
                    continue;   // Scan never leaves the project; belt and braces.
                }
                var segments = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (segments.Length == 1)
                {
                    looseFiles.Add(rel);
                    continue;
                }
                if (alreadyStaged.Contains(segments[0]))
                {
                    continue;
                }
                carrierDirectories.Add(segments[0]);
            }
        }

        foreach (var carrier in carrierDirectories)
        {
            var source = Path.Combine(projectDir, carrier);
            // An --out directory inside the project would otherwise be copied into itself.
            if (IsSameOrUnder(bundleRoot, source))
            {
                continue;
            }
            CopyTreeIfPresent(source, Path.Combine(appDir, carrier), written, bundleRoot,
                exclude: rel =>
                {
                    var parts = rel.Split('/', '\\');
                    if (parts[0] is "bin" or "obj" or ".git")
                    {
                        return true;
                    }
                    if (parts.Any(p => string.Equals(p, "bin", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(p, "obj", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(p, "keys", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                    var name = Path.GetFileName(rel);
                    return name is ".lock" || name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                        || name.EndsWith(".key", StringComparison.OrdinalIgnoreCase);
                });
        }

        foreach (var loose in looseFiles)
        {
            var source = Path.Combine(projectDir, loose);
            if (!File.Exists(source))
            {
                continue;
            }
            CopyFile(source, Path.Combine(appDir, loose), written, bundleRoot);
        }
    }

    /// <summary>True when <paramref name="candidate"/> is <paramref name="ancestor"/> or sits inside it.</summary>
    private static bool IsSameOrUnder(string candidate, string ancestor)
    {
        var a = Path.GetFullPath(ancestor).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var c = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return c.Equals(a, StringComparison.OrdinalIgnoreCase)
            || c.StartsWith(a + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Runs the project's own courses against the STAGED copy and returns a refusal naming what did
    /// not travel, or null when the bundle proves itself.
    ///
    /// <para>This is the guarantee, not the optimism: the launcher every export writes begins with
    /// <c>verify --path app</c>, so a staged copy that fails a course is an application that exits
    /// 65 on first launch, on every machine, while its bundle.json and README say
    /// <c>certified: true</c>. The source project verifying is not evidence that the COPY does —
    /// they are different directories, and only one of them is what the user receives. So the copy
    /// is verified before the export is allowed to call itself successful.</para>
    /// </summary>
    /// <param name="projectDir">The source project, used to name what is present there and absent here.</param>
    /// <param name="bundleDir">The staged bundle root (the one holding <c>app/</c>).</param>
    public static string? SelfVerificationRefusal(string projectDir, string bundleDir)
    {
        var appDir = Path.Combine(bundleDir, "app");
        var manifestPath = Path.Combine(appDir, "ashlar.yaml");
        var policyPath = Path.Combine(appDir, "ashlar.policy.yaml");
        if (!File.Exists(manifestPath) || !File.Exists(policyPath))
        {
            return "refusing to report this export as successful: the staged bundle has no "
                 + $"ashlar.yaml/ashlar.policy.yaml under {appDir}, so its launcher cannot verify it. "
                 + "This is an export bug, not a project one — re-run the export, and report it if it repeats.";
        }

        var staged = ProjectVerifier.Verify(
            File.ReadAllText(manifestPath), File.ReadAllText(policyPath), appDir);
        if (staged.Verified)
        {
            return null;
        }

        var lines = new List<string>
        {
            "refusing to report this export as successful: THE BUNDLE DOES NOT VERIFY ITSELF.",
            string.Empty,
            "the launcher this export writes starts with `verify --path app`, so a course that fails on the",
            "staged copy is an application that exits 65 on first launch, on every machine, while bundle.json",
            "and README.md say it is certified. the source project verifies; the copy does not, which means",
            "something the courses depend on did not travel:",
            string.Empty,
        };
        foreach (var course in staged.Courses.Where(c => !c.Passed))
        {
            lines.Add($"  course '{course.Name}' failed — {course.Detail}");
        }

        var stranded = StrandedBrickSources(projectDir, appDir);
        if (stranded.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("brick source that is in the project but NOT in the bundle:");
            foreach (var rel in stranded.Take(20))
            {
                lines.Add($"  {rel}");
            }
            if (stranded.Count > 20)
            {
                lines.Add($"  … and {stranded.Count - 20} more");
            }
        }

        lines.Add(string.Empty);
        lines.Add("fix, in the order to try them:");
        lines.Add("  - move that source under the project directory: everything under it travels except bin/,");
        lines.Add("    obj/, .git/, .ashlar/keys/ and .ashlar/forge/. a brick outside the project cannot ship.");
        lines.Add("  - a directory the policy needs (sandbox.root, a writable path) must EXIST in the project,");
        lines.Add("    or `verify` in the bundle refuses it. create it, commit a .gitkeep, and re-export.");
        lines.Add("  - or delete the entry from bricks: in ashlar.yaml, if the project no longer uses it.");
        lines.Add("then run `ashlar verify` at the source and export again.");
        lines.Add($"the incomplete bundle was left at {bundleDir} so you can see what did and did not arrive.");
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Declared-brick source files present under the project and absent from the staged app —
    /// the concrete list a person needs to see, rather than "something did not travel".
    /// </summary>
    private static List<string> StrandedBrickSources(string projectDir, string appDir)
    {
        var stranded = new List<string>();
        try
        {
            var manifestYaml = File.ReadAllText(Path.Combine(projectDir, "ashlar.yaml"));
            if (!ManifestLoader.TryLoad(manifestYaml, out var manifest, out _))
            {
                return stranded;
            }
            var inventory = BrickSourceResolver.Scan(projectDir);
            foreach (var brick in manifest!.Bricks)
            {
                foreach (var file in BrickSourceResolver.Resolve(inventory, brick.Id))
                {
                    var rel = Path.GetRelativePath(projectDir, file);
                    if (!File.Exists(Path.Combine(appDir, rel)))
                    {
                        stranded.Add(rel);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The refusal still stands on the failed courses above; this list is detail, not proof.
        }
        return stranded.Distinct(StringComparer.Ordinal).OrderBy(r => r, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Stages the project into <paramref name="bundleDir"/>/app and writes the launchers, the
    /// bundle descriptor, and a README. Returns the relative paths written. The runtime binary is
    /// added separately (see the export command's publish step).
    /// </summary>
    public static IReadOnlyList<string> Stage(string projectDir, string bundleDir, BundleInfo info)
    {
        var written = StageApp(projectDir, bundleDir);

        var exe = "ashlar" + (info.Rid.StartsWith("win", StringComparison.Ordinal) ? ".exe" : string.Empty);
        // The launcher must not overclaim: "certified" is only true when a signed ledger attests
        // these exact documents — bundle.json and the README already say it honestly.
        var readiness = info.Certified ? "certified and ready." : "verified and ready (unsigned).";

        // run.sh — verify (self-prove), then run whatever the user asked for.
        var runSh =
            "#!/usr/bin/env sh\n"
            + "set -e\n"
            + "DIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"\n"
            + "\"$DIR/" + exe + "\" verify --path \"$DIR/app\"\n"
            + "if [ \"$#\" -gt 0 ]; then\n"
            + "  exec \"$DIR/" + exe + "\" run \"$@\" --path \"$DIR/app\"\n"
            + "else\n"
            + "  echo\n"
            + "  echo \"" + readiness + " run a request with:  ./run.sh \\\"classify the invoices in ./inbox\\\"\"\n"
            + "fi\n";
        WriteText(Path.Combine(bundleDir, "run.sh"), runSh, written, bundleDir);

        // run.cmd — the Windows launcher.
        var runCmd =
            "@echo off\r\n"
            + "setlocal\r\n"
            + "set \"DIR=%~dp0\"\r\n"
            + "\"%DIR%" + exe + "\" verify --path \"%DIR%app\"\r\n"
            + "if errorlevel 1 exit /b %errorlevel%\r\n"
            + "if \"%~1\"==\"\" (\r\n"
            + "  echo.\r\n"
            + "  echo " + readiness + " run a request with:  run.cmd \"classify the invoices in .\\inbox\"\r\n"
            + ") else (\r\n"
            + "  \"%DIR%" + exe + "\" run %* --path \"%DIR%app\"\r\n"
            + ")\r\n";
        WriteText(Path.Combine(bundleDir, "run.cmd"), runCmd, written, bundleDir);

        // bundle.json — what is inside and what certifies it.
        var descriptor = new
        {
            format = Format,
            name = info.Name,
            rid = info.Rid,
            verified = info.Verified,
            certified = info.Certified,
            signer = info.SignerFingerprint,
            ledgerEntries = info.LedgerEntries,
            scope = info.Scope is null ? null : new
            {
                summary = info.Scope.Summary,
                coversCode = info.Scope.CoversCode,
                sourceFiles = info.Scope.SourceFiles,
                declaredBricks = info.Scope.DeclaredBricks,
                resolvedBricks = info.Scope.ResolvedBricks,
            },
            runtime = exe,
            run = info.Rid.StartsWith("win", StringComparison.Ordinal) ? "run.cmd \"<request>\"" : "./run.sh \"<request>\"",
        };
        WriteText(Path.Combine(bundleDir, "bundle.json"),
            JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true }), written, bundleDir);

        // README — the human's five-second orientation.
        var certLine = info.Certified
            ? $"It is **certified**: signed {info.SignerFingerprint}, with {info.LedgerEntries} entr{(info.LedgerEntries == 1 ? "y" : "ies")} in its tamper-evident ledger."
            : "It is **unsigned** — it verifies, but was not certified with an operator key. Run `ashlar keys init` at the source and re-export to certify.";
        var readme =
            $"# {info.Name} — a portable Ashlar application\n\n"
            + "This folder is a governed AI application you can run offline, with no install.\n\n"
            + $"{certLine}\n\n"
            + (info.Scope is { } scope
                ? $"Scope of that verdict: {scope.Summary}.\n\n"
                  + (scope.CoversCode
                      ? string.Empty
                      : "**There is no application code in this bundle.** The verdict above covers the two\n"
                        + "documents and nothing else — it does not attest an implementation.\n\n")
                : string.Empty)
            + "## Run it\n\n"
            + (info.Rid.StartsWith("win", StringComparison.Ordinal)
                ? "```\nrun.cmd \"<your request>\"\n```\n\n"
                : "```\n./run.sh \"<your request>\"\n```\n\n")
            + "The launcher **verifies the application against its own contract and ledger before it runs** —\n"
            + "if the app or its history were altered, it refuses. That is the whole point: the download\n"
            + "proves its own certification.\n\n"
            + "- `app/` — the project: its contract, its operator-owned policy, its signed ledger.\n"
            + $"- `{exe}` — the self-contained runtime (no .NET install required).\n"
            + "- `bundle.json` — what is inside and what certifies it.\n";
        WriteText(Path.Combine(bundleDir, "README.md"), readme, written, bundleDir);

        return written;
    }

    private static void CopyFile(string src, string dest, List<string> written, string bundleRoot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(src, dest, overwrite: true);
        written.Add(Path.GetRelativePath(bundleRoot, dest));
    }

    private static void WriteText(string dest, string content, List<string> written, string bundleRoot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllText(dest, content);
        written.Add(Path.GetRelativePath(bundleRoot, dest));
        if (dest.EndsWith(".sh", StringComparison.Ordinal) && !OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(dest,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }

    private static void CopyTreeIfPresent(string srcDir, string destDir, List<string> written, string bundleRoot, Func<string, bool>? exclude = null)
    {
        if (!Directory.Exists(srcDir))
        {
            return;
        }
        CopyTreeCore(srcDir, srcDir, destDir, written, bundleRoot, exclude);
    }

    private static void CopyTreeCore(string root, string dir, string destDir, List<string> written, string bundleRoot, Func<string, bool>? exclude)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
        {
            var rel = Path.GetRelativePath(root, entry);
            if (exclude?.Invoke(rel) == true)
            {
                continue;
            }
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                // Same rule ForgeApplier enforces on the write side: lexical containment is not
                // enough when a link is in the way. A link can pull content from outside the
                // project — including a private key — into the bundle, past every name filter.
                throw new InvalidOperationException(
                    $"refusing to stage '{rel}': it is a symlink or junction, and a link can pull content from outside the project into the bundle. Replace it with the real file, or remove it, and re-export.");
            }
            if ((attributes & FileAttributes.Directory) != 0)
            {
                CopyTreeCore(root, entry, destDir, written, bundleRoot, exclude);
            }
            else
            {
                CopyFile(entry, Path.Combine(destDir, rel), written, bundleRoot);
            }
        }
    }
}
