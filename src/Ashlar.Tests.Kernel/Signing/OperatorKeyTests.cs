using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Ashlar.Manifest.Signing;
using Xunit;

namespace Ashlar.Tests.Kernel.Signing;

/// <summary>
/// The v1 operator key: generate, load, fingerprint, sign, verify. Every test uses an
/// explicit key directory under the temp path — never the real <c>~/.ashlar/keys</c> — so
/// the suite never touches the developer's actual identity and the tests do not depend on
/// process-global env state.
/// </summary>
public sealed class OperatorKeyTests : IDisposable
{
    private readonly string _keyDir;

    public OperatorKeyTests()
    {
        _keyDir = Path.Combine(Path.GetTempPath(), "ashlar-keys-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_keyDir))
        {
            Directory.Delete(_keyDir, recursive: true);
        }
    }

    private static byte[] Msg(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Generate_writes_both_halves_and_TryLoad_returns_the_same_identity()
    {
        var made = OperatorKey.Generate(_keyDir);

        File.Exists(Path.Combine(_keyDir, "operator.key")).Should().BeTrue("the seed is written");
        File.Exists(Path.Combine(_keyDir, "operator.pub")).Should().BeTrue("the public half is written");

        var loaded = OperatorKey.TryLoad(_keyDir);
        loaded.Should().NotBeNull();
        loaded!.PublicKeyBase64.Should().Be(made.PublicKeyBase64);
        loaded.Fingerprint.Should().Be(made.Fingerprint);
    }

    [Fact]
    public void TryLoad_returns_null_when_no_key_exists()
    {
        // S-2: absence is not an error. No keys means signing is simply off, and the system
        // degrades to today's honest unsigned behaviour.
        OperatorKey.TryLoad(_keyDir).Should().BeNull();
    }

    [Fact]
    public void Fingerprint_has_the_spec_shape_and_is_deterministic()
    {
        var id = OperatorKey.Generate(_keyDir);

        // ed25519: + exactly 16 lowercase hex chars.
        Regex.IsMatch(id.Fingerprint, "^ed25519:[0-9a-f]{16}$").Should().BeTrue(
            $"fingerprint '{id.Fingerprint}' must be ed25519: plus 16 lowercase hex chars");

        var raw = Convert.FromBase64String(id.PublicKeyBase64);
        OperatorKey.Fingerprint(raw).Should().Be(id.Fingerprint, "the fingerprint is a pure function of the key");
    }

    [Fact]
    public void Sign_then_verify_round_trips()
    {
        var id = OperatorKey.Generate(_keyDir);
        var data = Msg("the gate admitted proposal ext-42");

        var sig = id.Sign(data);

        OperatorKey.Verify(id.PublicKeyBase64, data, sig).Should().BeTrue();
    }

    [Fact]
    public void Verify_rejects_a_tampered_message()
    {
        var id = OperatorKey.Generate(_keyDir);
        var sig = id.Sign(Msg("admit"));

        OperatorKey.Verify(id.PublicKeyBase64, Msg("refuse"), sig).Should().BeFalse(
            "a signature over 'admit' must not verify over 'refuse'");
    }

    [Fact]
    public void Verify_rejects_a_signature_from_a_different_key()
    {
        var a = OperatorKey.Generate(_keyDir);
        var b = OperatorKey.Generate(Path.Combine(_keyDir, "other"));
        var data = Msg("same bytes, wrong signer");

        var sigFromB = b.Sign(data);

        OperatorKey.Verify(a.PublicKeyBase64, data, sigFromB).Should().BeFalse();
    }

    [Fact]
    public void Verify_returns_false_not_throws_on_malformed_input()
    {
        var id = OperatorKey.Generate(_keyDir);

        // Garbage that is not even base64, and base64 of the wrong length, must both come
        // back false — a verifier that throws on bad input is a denial-of-service on read.
        OperatorKey.Verify(id.PublicKeyBase64, Msg("x"), "!!!not base64!!!").Should().BeFalse();
        OperatorKey.Verify("!!!not base64!!!", Msg("x"), Convert.ToBase64String(new byte[64])).Should().BeFalse();
        OperatorKey.Verify(Convert.ToBase64String(new byte[7]), Msg("x"), Convert.ToBase64String(new byte[64]))
            .Should().BeFalse("a 7-byte 'public key' is not an Ed25519 key");
    }

    [Fact]
    public void Generate_refuses_to_overwrite_an_existing_key_without_rotate()
    {
        OperatorKey.Generate(_keyDir);

        var act = () => OperatorKey.Generate(_keyDir);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already exists*rotate*");
    }

    [Fact]
    public void TryLoad_refuses_a_mismatched_key_and_pub_pair()
    {
        // The crash-mid-rotation shape the review caught: operator.key holds one key's seed
        // while operator.pub holds a different key's public half. A blindly-trusting load would
        // sign with the seed but advertise the wrong pub, so every record it wrote would fail
        // its own signature and poison the store. TryLoad must refuse it loudly instead.
        var a = OperatorKey.Generate(_keyDir);
        var b = OperatorKey.Generate(Path.Combine(_keyDir, "other"));

        // Splice b's public key over a's, leaving a's seed in place — a new seed beside a
        // stale pub, exactly what a torn rotation leaves.
        File.WriteAllText(Path.Combine(_keyDir, "operator.pub"), b.PublicKeyBase64);
        a.PublicKeyBase64.Should().NotBe(b.PublicKeyBase64, "the two keys must actually differ for this to test anything");

        var act = () => OperatorKey.TryLoad(_keyDir);

        act.Should().Throw<InvalidOperationException>().WithMessage("*does not derive*rotation*");
    }

    [Fact]
    public void TryLoad_reports_an_unreadable_key_as_corrupt_not_absent()
    {
        // A present-but-mangled key must fail loud, never look like "no key" (which would
        // silently drop signing). Garbled base64 and a valid-base64-but-wrong-length seed are
        // the two shapes; both must surface as the one corrupt-key InvalidOperationException,
        // not a raw FormatException that a caller's guard would miss.
        OperatorKey.Generate(_keyDir);

        File.WriteAllText(Path.Combine(_keyDir, "operator.key"), "not-valid-base64!!!");
        var garbled = () => OperatorKey.TryLoad(_keyDir);
        garbled.Should().Throw<InvalidOperationException>().WithMessage("*Corrupt operator key*");

        // Valid base64, but far too short to be a 32-byte Ed25519 seed.
        File.WriteAllText(Path.Combine(_keyDir, "operator.key"), Convert.ToBase64String(new byte[8]));
        var wrongLength = () => OperatorKey.TryLoad(_keyDir);
        wrongLength.Should().Throw<InvalidOperationException>().WithMessage("*Corrupt operator key*");
    }

    [Fact]
    public void Rotate_keeps_the_old_public_key_so_old_records_still_verify()
    {
        var oldId = OperatorKey.Generate(_keyDir);
        var data = Msg("a verdict signed before rotation");
        var oldSig = oldId.Sign(data);

        var newId = OperatorKey.Generate(_keyDir, rotate: true);

        newId.PublicKeyBase64.Should().NotBe(oldId.PublicKeyBase64, "rotation makes a genuinely new key");

        var trusted = Path.Combine(_keyDir, "trusted", oldId.Fingerprint.Replace(':', '-') + ".pub");
        File.Exists(trusted).Should().BeTrue("the old public key is retained so its records still verify");
        File.ReadAllText(trusted).Trim().Should().Be(oldId.PublicKeyBase64);

        // The signature made before rotation still verifies against the retained public key —
        // rotation revokes nothing in v1, it just changes which key signs going forward.
        OperatorKey.Verify(oldId.PublicKeyBase64, data, oldSig).Should().BeTrue();
    }
}
