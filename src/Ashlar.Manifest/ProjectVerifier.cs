namespace Ashlar.Manifest;

/// <summary>One course in a verification run.</summary>
public sealed record CourseResult
{
    /// <summary>Course name, e.g. <c>contract</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Whether the course passed.</summary>
    public required bool Passed { get; init; }

    /// <summary>What was checked (on pass) or exactly what failed (on fail).</summary>
    public required string Detail { get; init; }
}

/// <summary>The outcome of verifying a project.</summary>
public sealed record ProjectVerification
{
    /// <summary>Courses in the order they ran.</summary>
    public required IReadOnlyList<CourseResult> Courses { get; init; }

    /// <summary>True only when every course passed.</summary>
    public bool Verified => Courses.All(c => c.Passed);
}

/// <summary>
/// Verifies a project — the engine behind <c>ashlar verify</c>.
///
/// <para>Runs the courses that can be honestly checked from the two documents and the
/// filesystem: the contract loads, the composition is coherent, and the envelope is
/// enforceable. When the project has a signed instance ledger (once <c>ashlar keys init</c>
/// and a signed verify have run), a fourth <c>provenance</c> course joins and checks that
/// history's chain, fail-closed. A project with no ledger stays at three courses, and the
/// verdict a caller renders then says <em>unsigned</em>; with a valid signed head it says
/// <em>signed</em>.</para>
/// </summary>
public static class ProjectVerifier
{
    /// <summary>
    /// Verifies the project defined by the two documents.
    /// </summary>
    /// <param name="manifestYaml">Contents of <c>ashlar.yaml</c>.</param>
    /// <param name="policyYaml">Contents of <c>ashlar.policy.yaml</c>.</param>
    /// <param name="projectDirectory">Directory relative sandbox paths resolve against.</param>
    public static ProjectVerification Verify(string? manifestYaml, string? policyYaml, string projectDirectory)
    {
        var courses = new List<CourseResult>();

        // ── course 1 · contract ─────────────────────────────────────────────
        var manifestOk = ManifestLoader.TryLoad(manifestYaml, out var manifest, out var manifestReason);
        var policyOk = PolicyLoader.TryLoad(policyYaml, out var policy, out var policyReason);
        if (!manifestOk || !policyOk)
        {
            courses.Add(new CourseResult
            {
                Name = "contract",
                Passed = false,
                Detail = !manifestOk ? $"ashlar.yaml: {manifestReason}" : $"ashlar.policy.yaml: {policyReason}",
            });
            // Later courses depend on parsed documents; a broken contract is the whole verdict.
            return new ProjectVerification { Courses = courses };
        }
        courses.Add(new CourseResult
        {
            Name = "contract",
            Passed = true,
            Detail = $"both documents load · {manifest!.Agents.Count} agents · {manifest.Bricks.Count} bricks",
        });

        // ── course 2 · composition ──────────────────────────────────────────
        courses.Add(VerifyComposition(manifest));

        // ── course 3 · envelope ─────────────────────────────────────────────
        courses.Add(VerifyEnvelope(policy!, projectDirectory));

        // ── course 4 · provenance (only once a signed ledger exists) ────────
        var provenance = VerifyProvenance(projectDirectory, manifestYaml, policyYaml);
        if (provenance is not null)
        {
            courses.Add(provenance);
        }

        return new ProjectVerification { Courses = courses };
    }

    /// <summary>
    /// Verifies the project's instance ledger, when it has one. Returns null — no course — for a
    /// project that has never been certified, so a keyless, zero-setup project stays at three
    /// courses and reads <em>unsigned</em>. Once a signed history exists, this course checks two
    /// things and FAILS LOUD on either: the whole chain is intact (no signature, link, or sequence
    /// break), AND the latest entry attests THESE documents. The second half is what makes the
    /// certification bind to the contract: editing ashlar.yaml or ashlar.policy.yaml after signing
    /// leaves an intact chain whose head covers different bytes, and that is a provenance failure —
    /// a downloaded, tampered application is refused before it runs, and only re-certifying (a
    /// signed <c>verify</c>) makes the current documents the certified ones again.
    /// </summary>
    private static CourseResult? VerifyProvenance(string projectDirectory, string? manifestYaml, string? policyYaml)
    {
        var stateRoot = Path.Combine(projectDirectory, ".ashlar");
        var ledgerDir = Path.Combine(stateRoot, "ledger");
        var hasEntries = Directory.Exists(ledgerDir)
            && Directory.EnumerateFiles(ledgerDir, "*.json").Any(f => !f.EndsWith(".json.tmp", StringComparison.Ordinal));
        if (!hasEntries)
        {
            return null;
        }
        try
        {
            var chain = new Ledger.InstanceLedger(stateRoot).VerifyChain();
            var currentSubject = Ledger.InstanceLedger.Subject(manifestYaml, policyYaml);
            if (!string.Equals(chain.Head?.Subject, currentSubject, StringComparison.Ordinal))
            {
                return new CourseResult
                {
                    Name = "provenance",
                    Passed = false,
                    Detail = "the documents do not match the certification — ashlar.yaml or ashlar.policy.yaml "
                           + "changed since it was signed. re-certify with a signed `ashlar verify`.",
                };
            }
            return new CourseResult
            {
                Name = "provenance",
                Passed = true,
                Detail = $"{chain.Count} signed entr{(chain.Count == 1 ? "y" : "ies")} · chain intact · covers these documents",
            };
        }
        catch (InvalidOperationException ex)
        {
            return new CourseResult { Name = "provenance", Passed = false, Detail = ex.Message };
        }
    }

    private static CourseResult VerifyComposition(AshlarManifest manifest)
    {
        var failures = new List<string>();

        if (manifest.Agents.Count == 0)
        {
            failures.Add("no agents declared — an application with no agents does nothing");
        }
        foreach (var agent in manifest.Agents)
        {
            if (agent.Gates.Count == 0)
            {
                // Everything in this system is gated; an ungated agent is a hole, not a default.
                failures.Add($"agent '{agent.Id}' declares no gates");
            }
        }
        if (manifest.Targets.Count == 0)
        {
            failures.Add("no deployment targets declared");
        }
        foreach (var brick in manifest.Bricks)
        {
            if (string.IsNullOrWhiteSpace(brick.Id) || string.IsNullOrWhiteSpace(brick.Version))
            {
                failures.Add("a brick entry is missing id or version");
            }
        }

        return failures.Count == 0
            ? new CourseResult { Name = "composition", Passed = true, Detail = "agents gated · targets declared" }
            : new CourseResult { Name = "composition", Passed = false, Detail = string.Join("; ", failures) };
    }

    private static CourseResult VerifyEnvelope(AshlarPolicy policy, string projectDirectory)
    {
        var failures = new List<string>();

        // The sandbox root must actually exist — a policy naming a directory that is not
        // there confines nothing.
        var root = policy.Sandbox.Root;
        var resolvedRoot = Path.IsPathRooted(root)
            ? Path.GetFullPath(root)
            : Path.GetFullPath(Path.Combine(projectDirectory, root));
        if (!Directory.Exists(resolvedRoot))
        {
            failures.Add($"sandbox.root '{root}' does not exist (resolved: {resolvedRoot})");
        }

        // Writable paths must stay inside the root. Same containment discipline as the tool
        // sandbox: normalize, then require the root prefix.
        foreach (var writable in policy.Sandbox.Writable)
        {
            var resolved = Path.IsPathRooted(writable)
                ? Path.GetFullPath(writable)
                : Path.GetFullPath(Path.Combine(resolvedRoot, writable));
            var rootWithSep = resolvedRoot.EndsWith(Path.DirectorySeparatorChar)
                ? resolvedRoot
                : resolvedRoot + Path.DirectorySeparatorChar;
            if (!resolved.Equals(resolvedRoot, StringComparison.Ordinal)
                && !resolved.StartsWith(rootWithSep, StringComparison.Ordinal))
            {
                failures.Add($"writable path '{writable}' escapes sandbox.root");
            }
        }

        // A mode that can admit extensions with a zero budget is a contradiction the loader
        // does not catch (budget shape is valid); the verifier does.
        if (policy.SelfExtend.Mode != SelfExtendMode.Sealed && policy.SelfExtend.Budget.Extensions < 1)
        {
            failures.Add($"selfExtend.mode '{policy.SelfExtend.Mode}' with budget.extensions "
                + $"{policy.SelfExtend.Budget.Extensions} can never admit anything — seal it or fund it");
        }

        return failures.Count == 0
            ? new CourseResult
            {
                Name = "envelope",
                Passed = true,
                Detail = $"sandbox confined · mode: {policy.SelfExtend.Mode}",
            }
            : new CourseResult { Name = "envelope", Passed = false, Detail = string.Join("; ", failures) };
    }
}
