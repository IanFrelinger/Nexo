using FluentAssertions;
using Ashlar.CLI.Commands;
using Ashlar.Manifest;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// The defect: <c>ashlar export</c> reported CERTIFIED and exited 0 over a bundle that cannot
/// verify itself.
///
/// <para><c>NativeBundle.StageApp</c> copied <c>ashlar.yaml</c>, <c>ashlar.policy.yaml</c>,
/// <c>.ashlar/</c> and <c>src/</c>. The composition course resolves declared bricks by scanning the
/// WHOLE project tree. So a brick whose source sits anywhere else — the layout the composition
/// refusal itself tells you to create — certified at the source project and was then dropped by the
/// export. The bundle still said <c>verified:true / certified:true</c> in bundle.json and README.md
/// and exited 0, and its own <c>run.sh</c> begins with <c>verify --path app</c>, which failed course
/// 2 and exited 65. The shipped application refused to launch, on every machine.</para>
///
/// <para>Two halves are pinned here. The brick travels, so the export that used to lie now
/// succeeds honestly. And where something the courses depend on genuinely cannot travel, the export
/// REFUSES by name rather than reporting success — because a bundle whose own launcher exits 65
/// must never be reported as a successful export.</para>
/// </summary>
public sealed class ExportBundleSelfVerifiesTests : IDisposable
{
    private readonly string _root;
    private readonly string _project;
    private readonly string _out;

    public ExportBundleSelfVerifiesTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ashlar-export-" + Guid.NewGuid().ToString("N"));
        _project = Path.Combine(_root, "project");
        _out = Path.Combine(_root, "out");
        Directory.CreateDirectory(_project);
        Directory.CreateDirectory(_out);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    /// <summary>Writes the scaffolded documents, optionally declaring one brick and/or moving the sandbox root.</summary>
    private void Scaffold(string? brickId = null, string? sandboxRoot = null)
    {
        ProjectScaffold.TryScaffold("exportdemo", out var manifest, out var policy, out var reason)
            .Should().BeTrue(reason);

        if (brickId is not null)
        {
            manifest = manifest.Replace(
                "bricks: []",
                $"bricks:\n  - id: {brickId}\n    version: 1.0.0",
                StringComparison.Ordinal);
        }
        if (sandboxRoot is not null)
        {
            policy = policy.Replace("  root: .", $"  root: {sandboxRoot}", StringComparison.Ordinal);
        }

        File.WriteAllText(Path.Combine(_project, "ashlar.yaml"), manifest);
        File.WriteAllText(Path.Combine(_project, "ashlar.policy.yaml"), policy);
    }

    private ProjectVerification VerifyProject() => ProjectVerifier.Verify(
        File.ReadAllText(Path.Combine(_project, "ashlar.yaml")),
        File.ReadAllText(Path.Combine(_project, "ashlar.policy.yaml")),
        _project);

    private string StageBundle()
    {
        var bundleDir = Path.Combine(_out, "bundle");
        Directory.CreateDirectory(bundleDir);
        var info = NativeBundle.Describe(_project, "linux-x64");
        NativeBundle.Stage(_project, bundleDir, info);
        return bundleDir;
    }

    [Fact]
    public void A_brick_outside_src_travels_into_the_bundle()
    {
        // The layout the composition refusal recommends: a brick is a project you build here, and
        // nothing says it has to live under src/.
        Scaffold(brickId: "invoice-classifier");
        var brickDir = Path.Combine(_project, "bricks", "invoice-classifier");
        Directory.CreateDirectory(brickDir);
        File.WriteAllText(Path.Combine(brickDir, "InvoiceClassifier.cs"), "namespace Demo; public sealed class InvoiceClassifier { }\n");
        File.WriteAllText(Path.Combine(brickDir, "InvoiceClassifier.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        // Build output must NOT travel, exactly as it does not from src/.
        Directory.CreateDirectory(Path.Combine(brickDir, "obj"));
        File.WriteAllText(Path.Combine(brickDir, "obj", "Stale.cs"), "// stale\n");

        VerifyProject().Verified.Should().BeTrue("the source project resolves the brick");

        var bundleDir = StageBundle();
        var app = Path.Combine(bundleDir, "app");

        File.Exists(Path.Combine(app, "bricks", "invoice-classifier", "InvoiceClassifier.cs"))
            .Should().BeTrue("the source the composition course resolved is what the bundle has to carry");
        File.Exists(Path.Combine(app, "bricks", "invoice-classifier", "InvoiceClassifier.csproj"))
            .Should().BeTrue("a brick is a project; carrying only its .cs ships something nobody can build");
        File.Exists(Path.Combine(app, "bricks", "invoice-classifier", "obj", "Stale.cs"))
            .Should().BeFalse("build output is not cargo");
    }

    [Fact]
    public void The_staged_bundle_verifies_itself()
    {
        Scaffold(brickId: "invoice-classifier");
        var brickDir = Path.Combine(_project, "bricks", "invoice-classifier");
        Directory.CreateDirectory(brickDir);
        File.WriteAllText(Path.Combine(brickDir, "InvoiceClassifier.cs"), "namespace Demo; public sealed class InvoiceClassifier { }\n");

        var bundleDir = StageBundle();

        NativeBundle.SelfVerificationRefusal(_project, bundleDir).Should().BeNull(
            "the bundle's own run.sh starts with `verify --path app`; if that fails the shipped app never launches");

        // The claim the export prints is only honest if this holds, so prove it directly.
        var staged = ProjectVerifier.Verify(
            File.ReadAllText(Path.Combine(bundleDir, "app", "ashlar.yaml")),
            File.ReadAllText(Path.Combine(bundleDir, "app", "ashlar.policy.yaml")),
            Path.Combine(bundleDir, "app"));
        staged.Verified.Should().BeTrue(string.Join(" | ", staged.Courses.Select(c => $"{c.Name}: {c.Detail}")));
    }

    [Fact]
    public void A_brick_at_the_project_root_travels_too()
    {
        Scaffold(brickId: "router");
        File.WriteAllText(Path.Combine(_project, "Router.cs"), "namespace Demo; public sealed class Router { }\n");

        VerifyProject().Verified.Should().BeTrue();
        var bundleDir = StageBundle();

        File.Exists(Path.Combine(bundleDir, "app", "Router.cs")).Should().BeTrue();
        NativeBundle.SelfVerificationRefusal(_project, bundleDir).Should().BeNull();
    }

    [Fact]
    public void A_brick_under_src_is_unaffected()
    {
        // The path that always worked must keep working: this is a staging fix, not a staging rewrite.
        Scaffold(brickId: "ledgerbrick");
        var dir = Path.Combine(_project, "src", "LedgerBrick");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "LedgerBrick.cs"), "namespace Demo; public sealed class LedgerBrick { }\n");

        var bundleDir = StageBundle();

        File.Exists(Path.Combine(bundleDir, "app", "src", "LedgerBrick", "LedgerBrick.cs")).Should().BeTrue();
        NativeBundle.SelfVerificationRefusal(_project, bundleDir).Should().BeNull();
    }

    [Fact]
    public void An_operator_key_inside_a_brick_directory_still_never_travels()
    {
        // SPEC-006's first rule does not get a hole cut in it by a new staging path.
        Scaffold(brickId: "invoice-classifier");
        var brickDir = Path.Combine(_project, "bricks", "invoice-classifier");
        Directory.CreateDirectory(Path.Combine(brickDir, "keys"));
        File.WriteAllText(Path.Combine(brickDir, "InvoiceClassifier.cs"), "namespace Demo; public sealed class InvoiceClassifier { }\n");
        File.WriteAllText(Path.Combine(brickDir, "keys", "operator.seed"), "SECRET");
        File.WriteAllText(Path.Combine(brickDir, "operator.key"), "SECRET");

        var app = Path.Combine(StageBundle(), "app");

        Directory.Exists(Path.Combine(app, "bricks", "invoice-classifier", "keys")).Should().BeFalse();
        File.Exists(Path.Combine(app, "bricks", "invoice-classifier", "operator.key")).Should().BeFalse();
    }

    [Fact]
    public void A_narrowed_sandbox_root_travels_so_the_export_is_not_an_unfixable_refusal()
    {
        // W3. Narrowing sandbox.root is the ordinary hardening move, and it made `ashlar export`
        // impossible to satisfy. StageApp carried ashlar.yaml, ashlar.policy.yaml, .ashlar/, src/
        // and declared-brick carriers; ./workspace is none of those, so the envelope course failed
        // on the COPY with "sandbox.root './workspace' does not exist" and the export refused —
        // forever. The refusal's own named fix ("create it, commit a .gitkeep, re-export") was run
        // verbatim and produced the byte-identical refusal, because the directory was never missing
        // at the source. There was no override flag, and the same project exported fine before the
        // self-verification check existed.
        //
        // The policy IS the declaration of where this application writes. Carrying the declaration
        // while dropping the thing it declares is what made the bundle unable to prove itself.
        Scaffold(sandboxRoot: "./workspace");
        Directory.CreateDirectory(Path.Combine(_project, "workspace"));

        VerifyProject().Verified.Should().BeTrue("the source project has the directory the policy names");

        var bundleDir = StageBundle();

        Directory.Exists(Path.Combine(bundleDir, "app", "workspace"))
            .Should().BeTrue("the directory the policy names has to exist in the copy the launcher verifies");
        NativeBundle.SelfVerificationRefusal(_project, bundleDir).Should().BeNull(
            "a project that verifies at the source and hardens its sandbox must still be exportable");
    }

    [Fact]
    public void A_writable_path_under_a_narrowed_root_travels_too()
    {
        // The same fact for the rest of the envelope: writable paths are resolved beneath the root,
        // and a bundle missing them is a bundle whose app cannot write where its policy says it may.
        Scaffold(sandboxRoot: "./workspace");
        Directory.CreateDirectory(Path.Combine(_project, "workspace"));
        var policyPath = Path.Combine(_project, "ashlar.policy.yaml");
        var policy = File.ReadAllText(policyPath).Replace(
            "  writable: []", "  writable:\n    - ./out", StringComparison.Ordinal);
        policy.Should().Contain("- ./out", "the test has to actually change the policy it is about");
        File.WriteAllText(policyPath, policy);

        var app = Path.Combine(StageBundle(), "app");

        Directory.Exists(Path.Combine(app, "workspace", "out")).Should().BeTrue();
    }

    [Fact]
    public void The_refusal_no_longer_teaches_a_step_that_cannot_work()
    {
        // The old fix text told the operator to create the directory and re-export, which was the
        // exact thing that could not help — the directory already existed at the source. A refusal
        // naming an unrunnable fix is the defect this whole pass is about.
        Scaffold(brickId: "invoice-classifier");
        var brickDir = Path.Combine(_project, "bricks", "invoice-classifier");
        Directory.CreateDirectory(brickDir);
        File.WriteAllText(Path.Combine(brickDir, "InvoiceClassifier.cs"), "namespace Demo; public sealed class InvoiceClassifier { }\n");
        var bundleDir = StageBundle();
        Directory.Delete(Path.Combine(bundleDir, "app", "bricks"), recursive: true);

        var refusal = NativeBundle.SelfVerificationRefusal(_project, bundleDir);

        refusal.Should().NotBeNull();
        refusal!.Should().Contain("DOES NOT VERIFY ITSELF");
        refusal.Should().Contain("verify --path app", "why it matters: it is what the launcher runs first");
        refusal.Should().Contain(bundleDir, "and where to look at what did and did not arrive");
        refusal.Should().NotContain("commit a .gitkeep",
            "that step was run verbatim and produced the identical refusal");
        refusal.Should().Contain("is created inside the bundle",
            "the fix list has to say what the export now does on the operator's behalf");
    }

    [Fact]
    public void The_refusal_lists_the_brick_source_that_did_not_arrive()
    {
        // Force the stranding directly: stage, then delete the brick from the bundle. This is the
        // pre-fix bundle, byte for byte, and it must be refused rather than reported as certified.
        Scaffold(brickId: "invoice-classifier");
        var brickDir = Path.Combine(_project, "bricks", "invoice-classifier");
        Directory.CreateDirectory(brickDir);
        File.WriteAllText(Path.Combine(brickDir, "InvoiceClassifier.cs"), "namespace Demo; public sealed class InvoiceClassifier { }\n");

        var bundleDir = StageBundle();
        Directory.Delete(Path.Combine(bundleDir, "app", "bricks"), recursive: true);

        var refusal = NativeBundle.SelfVerificationRefusal(_project, bundleDir);

        refusal.Should().NotBeNull();
        refusal!.Should().Contain("course 'composition' failed");
        refusal.Should().Contain("brick source that is in the project but NOT in the bundle");
        refusal.Should().Contain(Path.Combine("bricks", "invoice-classifier", "InvoiceClassifier.cs"));
    }

    [Fact]
    public void An_out_directory_inside_the_project_is_not_copied_into_itself()
    {
        // `ashlar export native --path . --out ./dist` is an obvious thing to type. The new brick
        // staging walks top-level directories, so it must not walk the bundle it is writing.
        Scaffold(brickId: "dist");
        var brickDir = Path.Combine(_project, "dist");
        Directory.CreateDirectory(brickDir);
        File.WriteAllText(Path.Combine(brickDir, "Dist.cs"), "namespace Demo; public sealed class Dist { }\n");

        var bundleDir = Path.Combine(brickDir, "bundle");
        Directory.CreateDirectory(bundleDir);
        var info = NativeBundle.Describe(_project, "linux-x64");

        var act = () => NativeBundle.Stage(_project, bundleDir, info);

        act.Should().NotThrow("a bundle written inside a staged directory must not recurse into itself");
        Directory.Exists(Path.Combine(bundleDir, "app", "dist")).Should().BeFalse(
            "the output directory is not the application's source");
    }
}
