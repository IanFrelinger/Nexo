using System.Text.Json.Nodes;
using FluentAssertions;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;
using Ashlar.Manifest.Packaging;
using Ashlar.Manifest.Signing;
using Xunit;

namespace Ashlar.Tests.Kernel.Packaging;

/// <summary>
/// The certified extension package: a portable admission. The properties under test are the two
/// signatures and what each refuses — the SEAL binds files to the verdict (swap the payload, the
/// seal breaks), and the RECORD's own signature is the verdict's authority (re-seal a doctored
/// record with your own key and the record check still refuses). Verification needs no local
/// keys and no network; opening a package admits nothing.
/// </summary>
public sealed class ExtensionPackagingTests : IDisposable
{
    private readonly string _dir;
    private readonly SigningIdentity _origin;

    public ExtensionPackagingTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashpkg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _origin = OperatorKey.Generate(Path.Combine(_dir, "origin-keys"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private static readonly DateTimeOffset Now = new(2026, 8, 24, 6, 0, 0, TimeSpan.Zero);

    private static ExtensionProposal Proposal(string id) => new()
    {
        Id = id,
        Kind = "brick",
        Summary = "add brick shared.classify",
        ProposedBy = "night-agent",
        ProposedAt = Now,
        Courses =
        [
            new CourseResult { Name = "sandbox", Passed = true, Detail = "confined" },
            new CourseResult { Name = "tests", Passed = true, Detail = "14 passed" },
        ],
        ForgeProposalIds = ["forge-1"],
    };

    /// <summary>An Admitted record signed by the origin key — built through the real store so the
    /// signature is exactly what production writes.</summary>
    private async Task<GateRecord> AdmittedRecordAsync(string id = "ext-share")
    {
        var store = new GateStore(Path.Combine(_dir, ".ashlar-" + id), _origin);
        await store.RecordAsync(Proposal(id), new AdmissionOutcome { State = ProposalState.Held, Reason = "held" }, Now);
        return await store.DecideAsync(id, admit: true, "origin-operator", "reviewed", Now.AddMinutes(1));
    }

    private static IReadOnlyList<PackageFile> Files(string content = "// admitted code") =>
        [new PackageFile { Path = "src/Shared.cs", Content = content }];

    // ─────────────────────────── the happy path ───────────────────────────

    [Fact]
    public async Task Pack_then_open_round_trips_and_verifies_with_no_local_keys()
    {
        var record = await AdmittedRecordAsync();
        var json = ExtensionPackaging.Pack(record, Files(), _origin);

        // TryOpen is static and reads no key material from disk or env — intrinsic by construction.
        ExtensionPackaging.TryOpen(json, out var pkg, out var reason).Should().BeTrue(reason);
        pkg!.Record.Proposal.Id.Should().Be("ext-share");
        pkg.Record.State.Should().Be(ProposalState.Admitted);
        pkg.Files.Should().ContainSingle().Which.Content.Should().Be("// admitted code");
        pkg.SealSigner.Should().Be(_origin.PublicKeyBase64);
        pkg.Record.Signer.Should().Be(_origin.PublicKeyBase64);
    }

    // ─────────────────────────── pack refusals ───────────────────────────

    [Fact]
    public async Task Pack_refuses_a_record_that_is_not_admitted()
    {
        var store = new GateStore(Path.Combine(_dir, ".ashlar-held"), _origin);
        var held = await store.RecordAsync(Proposal("ext-held"), new AdmissionOutcome { State = ProposalState.Held, Reason = "held" }, Now);

        var act = () => ExtensionPackaging.Pack(held, Files(), _origin);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ADMITTED*Held*");
    }

    [Fact]
    public async Task Pack_refuses_an_unsigned_record()
    {
        // An unsigned admission proves nothing to a receiver — it must not travel.
        var store = new GateStore(Path.Combine(_dir, ".ashlar-unsigned"));   // no signer
        await store.RecordAsync(Proposal("ext-plain"), new AdmissionOutcome { State = ProposalState.Held, Reason = "held" }, Now);
        var unsigned = await store.DecideAsync("ext-plain", admit: true, "op", "ok", Now.AddMinutes(1));

        var act = () => ExtensionPackaging.Pack(unsigned, Files(), _origin);

        act.Should().Throw<InvalidOperationException>().WithMessage("*unsigned*proves nothing*");
    }

    [Fact]
    public async Task Pack_refuses_escaping_or_rooted_paths()
    {
        var record = await AdmittedRecordAsync("ext-paths");
        foreach (var bad in new[] { "../outside.cs", "src/../../evil.cs", "/etc/passwd", "C:\\windows\\evil", "src\\..\\..\\evil", "a::stream" })
        {
            var act = () => ExtensionPackaging.Pack(record, [new PackageFile { Path = bad, Content = "x" }], _origin);
            act.Should().Throw<InvalidOperationException>($"path '{bad}' must be refused").WithMessage("*Illegal package path*");
        }
    }

    [Fact]
    public async Task Pack_refuses_an_empty_file_list()
    {
        var record = await AdmittedRecordAsync("ext-empty");
        var act = () => ExtensionPackaging.Pack(record, [], _origin);
        act.Should().Throw<InvalidOperationException>().WithMessage("*no files*");
    }

    // ─────────────────────────── open refusals: the attacks ───────────────────────────

    [Fact]
    public async Task Open_refuses_a_tampered_payload()
    {
        var record = await AdmittedRecordAsync("ext-tamper");
        var json = ExtensionPackaging.Pack(record, Files("// original"), _origin);

        var doctored = json.Replace("// original", "// backdoored");

        ExtensionPackaging.TryOpen(doctored, out _, out var reason).Should().BeFalse();
        reason.Should().Contain("seal does not verify");
    }

    [Fact]
    public async Task Open_refuses_a_transplanted_verdict_re_sealed_by_an_attacker()
    {
        // THE key attack. An attacker takes a genuine package, doctors the RECORD (say, flips a
        // failing course to passing, or the summary to their code), and re-seals the whole thing
        // with their own key. Check 1 (the seal) now PASSES — the attacker sealed exactly these
        // bytes. Check 2 must still refuse: the record's embedded signature no longer covers the
        // doctored verdict, and the sealer's key is not the verdict's authority.
        var record = await AdmittedRecordAsync("ext-forge");
        var json = ExtensionPackaging.Pack(record, Files(), _origin);
        var attacker = OperatorKey.Generate(Path.Combine(_dir, "attacker-keys"));

        var node = JsonNode.Parse(json)!;
        node["Record"]!["Proposal"]!["Summary"] = "add brick attacker.payload";  // doctor the verdict's content
        var doctoredUnsealed = System.Text.Json.JsonSerializer.Deserialize<ExtensionPackage>(
            node.ToJsonString(), new System.Text.Json.JsonSerializerOptions
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
            })! with
        { Seal = null, SealSigner = null };
        var reSealed = doctoredUnsealed with
        {
            Seal = attacker.Sign(CanonicalJson.Bytes(doctoredUnsealed)),
            SealSigner = attacker.PublicKeyBase64,
        };
        var reSealedJson = System.Text.Json.JsonSerializer.Serialize(reSealed, new System.Text.Json.JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        });

        ExtensionPackaging.TryOpen(reSealedJson, out _, out var reason).Should().BeFalse(
            "a valid seal from the wrong authority must not carry a doctored verdict");
        reason.Should().Contain("gate record's signature does not verify");
    }

    [Fact]
    public async Task Open_refuses_files_swapped_under_a_pristine_record_and_re_sealed_by_an_attacker()
    {
        // THE critical attack the review caught. The record signature does NOT cover file bytes —
        // only the seal does. So an attacker keeps a GENUINE origin-signed Admitted record exactly
        // as-is, swaps the Files for their own code, and mints a fresh seal with their OWN key.
        // Check 1 (seal) passes: they sealed exactly these bytes. Check 2 (record sig) passes: the
        // record is untouched. The bind — sealer must be the verdict's signer — is what refuses.
        var record = await AdmittedRecordAsync("ext-swap");
        var json = ExtensionPackaging.Pack(record, Files("// innocent"), _origin);
        var attacker = OperatorKey.Generate(Path.Combine(_dir, "swap-attacker"));

        var jsonOpts = new System.Text.Json.JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        };
        var genuine = System.Text.Json.JsonSerializer.Deserialize<ExtensionPackage>(json, jsonOpts)!;
        var swapped = genuine with
        {
            Files = [new PackageFile { Path = "src/Backdoor.cs", Content = "// malware" }],
            Seal = null,
            SealSigner = null,
        };
        var reSealed = swapped with
        {
            Seal = attacker.Sign(CanonicalJson.Bytes(swapped)),
            SealSigner = attacker.PublicKeyBase64,
        };
        var attackJson = System.Text.Json.JsonSerializer.Serialize(reSealed, jsonOpts);

        // Sanity: the record is still the genuine origin's, untouched.
        reSealed.Record.Signer.Should().Be(_origin.PublicKeyBase64);
        reSealed.Record.Sig.Should().Be(genuine.Record.Sig);

        ExtensionPackaging.TryOpen(attackJson, out _, out var reason).Should().BeFalse(
            "a genuine verdict must not vouch for files a different key sealed");
        reason.Should().Contain("sealed by a different key");
    }

    [Fact]
    public async Task Open_refuses_a_stripped_seal()
    {
        var record = await AdmittedRecordAsync("ext-strip");
        var json = ExtensionPackaging.Pack(record, Files(), _origin);
        var node = JsonNode.Parse(json)!;
        node["Seal"] = null;
        node["SealSigner"] = null;

        ExtensionPackaging.TryOpen(node.ToJsonString(), out _, out var reason).Should().BeFalse();
        reason.Should().Contain("no seal");
    }

    [Fact]
    public async Task Open_refuses_a_non_admitted_verdict_even_when_correctly_sealed()
    {
        // A held verdict sealed by its own origin is internally consistent — and still refused:
        // only admissions travel. (Built by hand because Pack correctly refuses to build it.)
        var store = new GateStore(Path.Combine(_dir, ".ashlar-heldseal"), _origin);
        var held = await store.RecordAsync(Proposal("ext-heldpkg"), new AdmissionOutcome { State = ProposalState.Held, Reason = "held" }, Now);
        var unsealed = new ExtensionPackage
        {
            FormatVersion = ExtensionPackage.ExpectedFormatVersion,
            Record = held,
            Files = Files(),
        };
        var sealed_ = unsealed with { Seal = _origin.Sign(CanonicalJson.Bytes(unsealed)), SealSigner = _origin.PublicKeyBase64 };
        var json = System.Text.Json.JsonSerializer.Serialize(sealed_, new System.Text.Json.JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        });

        ExtensionPackaging.TryOpen(json, out _, out var reason).Should().BeFalse();
        reason.Should().Contain("not Admitted");
    }

    [Fact]
    public void Open_refuses_garbage_and_wrong_versions()
    {
        ExtensionPackaging.TryOpen("not json at all", out _, out var r1).Should().BeFalse();
        r1.Should().Contain("not valid ashpkg JSON");

        ExtensionPackaging.TryOpen("", out _, out var r2).Should().BeFalse();
        r2.Should().Contain("empty");

        ExtensionPackaging.TryOpen("""{"FormatVersion":"ashpkg/v99"}""", out _, out var r3).Should().BeFalse();
        // An unknown version may fail on shape before version — either way it is refused loudly.
    }

    [Fact]
    public async Task Open_refuses_an_escaping_path_in_a_hand_crafted_package()
    {
        // Pack refuses to create one; an attacker hand-crafts it and seals it themselves. The
        // receiving side must independently refuse the path — safety is checked at BOTH ends.
        var record = await AdmittedRecordAsync("ext-crafted");
        var unsealed = new ExtensionPackage
        {
            FormatVersion = ExtensionPackage.ExpectedFormatVersion,
            Record = record,
            Files = [new PackageFile { Path = "../../outside.cs", Content = "// escape" }],
        };
        var sealed_ = unsealed with { Seal = _origin.Sign(CanonicalJson.Bytes(unsealed)), SealSigner = _origin.PublicKeyBase64 };
        var json = System.Text.Json.JsonSerializer.Serialize(sealed_, new System.Text.Json.JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        });

        ExtensionPackaging.TryOpen(json, out _, out var reason).Should().BeFalse();
        reason.Should().Contain("not a safe project-relative path");
    }

    // ─────────────────────────── path shape ───────────────────────────

    [Theory]
    [InlineData("src/Ok.cs", true)]
    [InlineData("docs\\note.md", true)]
    [InlineData("a/b/c.txt", true)]
    [InlineData("../x", false)]
    [InlineData("a/../../x", false)]
    [InlineData("/rooted", false)]
    [InlineData("\\rooted", false)]
    [InlineData("C:/win", false)]
    [InlineData("a::ads", false)]
    [InlineData("", false)]
    [InlineData("a//b", false)]
    [InlineData(".", false)]
    [InlineData("con", false)]
    [InlineData("src/nul.cs", false)]
    [InlineData("aux", false)]
    [InlineData("COM1", false)]
    [InlineData("src/lpt9.txt", false)]
    [InlineData("trailing.", false)]
    [InlineData("trailingspace ", false)]
    [InlineData("console.cs", true)]
    public void Path_safety_is_an_allowlist(string path, bool ok) =>
        ExtensionPackaging.IsSafeRelativePath(path).Should().Be(ok, $"'{path}'");
}
