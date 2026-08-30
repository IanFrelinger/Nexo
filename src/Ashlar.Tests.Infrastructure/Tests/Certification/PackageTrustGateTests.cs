using Ashlar.Manifest.Signing;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The trust root (CLOSING-PLAN Phase 3): an imported package is admitted only if its sealer is
/// trusted — by the project policy's <c>trustedSigners</c> or the operator's local peers keychain.
/// These pin the decision itself; the end-to-end refusal on one box is proven by
/// scripts/e2e-loop.sh's <c>pkg-import-refuses-untrusted-signer</c>.
///
/// <para><b>The trap this guards.</b> The admission allowlist lives in <c>peers/</c>, deliberately
/// separate from <c>trusted/</c> — the directory key rotation drops the operator's OWN superseded
/// public keys into, for verifying old records. Wiring admission onto <c>trusted/</c> would make
/// rotating after a key theft silently re-authorize the stolen key. The last test asserts
/// <c>trusted/</c> is never an admission input.</para>
///
/// <para>In <c>...Tests.Certification</c> so it rides cert-gate. Hermetic: a temp key dir, no network.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class PackageTrustGateTests : IDisposable
{
    private readonly string _keyDir;

    public PackageTrustGateTests()
    {
        _keyDir = Path.Combine(Path.GetTempPath(), "ashlar-trust-" + Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(_keyDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_keyDir, recursive: true); } catch { /* best effort */ }
    }

    private const string A = "ed25519:aaaa1111bbbb2222";
    private const string B = "ed25519:cccc3333dddd4444";

    [Theory]
    [InlineData("ed25519:0123456789abcdef", true)]
    [InlineData("ed25519:aaaaaaaaaaaaaaaa", true)]
    [InlineData("ed25519:AAAA1111bbbb2222", false)] // uppercase hex is not the canonical form
    [InlineData("ed25519:short", false)]
    [InlineData("ed25519:0123456789abcdeg", false)] // 'g' not hex
    [InlineData("sha256:0123456789abcdef", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidFingerprint(string? fp, bool ok)
        => OperatorKey.IsValidFingerprint(fp).Should().Be(ok);

    [Fact]
    public void Trust_ThenList_RoundTripsTheFingerprint()
    {
        OperatorKey.Trust(A, _keyDir);
        OperatorKey.Trust(B, _keyDir);

        OperatorKey.ListTrusted(_keyDir).Should().BeEquivalentTo(new[] { A, B });
    }

    [Fact]
    public void Trust_IsIdempotent()
    {
        OperatorKey.Trust(A, _keyDir);
        OperatorKey.Trust(A, _keyDir);

        OperatorKey.ListTrusted(_keyDir).Should().ContainSingle().Which.Should().Be(A);
    }

    [Fact]
    public void Untrust_Removes_AndCannotShrinkWhatIsNotThere()
    {
        OperatorKey.Trust(A, _keyDir);
        OperatorKey.Untrust(A, _keyDir).Should().BeTrue();
        OperatorKey.ListTrusted(_keyDir).Should().BeEmpty();
        OperatorKey.Untrust(A, _keyDir).Should().BeFalse("removing an absent fingerprint is a no-op, not a lie");
    }

    [Fact]
    public void Trust_RejectsAMalformedFingerprint()
    {
        var act = () => OperatorKey.Trust("not-a-fingerprint", _keyDir);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsSignerTrusted_HonoursPolicyAndKeychain_ButNeverTheUnsigned()
    {
        // policy declares A; keychain adds B; C is a stranger; unsigned is never trusted.
        OperatorKey.Trust(B, _keyDir);
        var policy = new[] { A };

        OperatorKey.IsSignerTrusted(A, policy, _keyDir).Should().BeTrue("policy trustedSigners");
        OperatorKey.IsSignerTrusted(B, policy, _keyDir).Should().BeTrue("operator keychain");
        OperatorKey.IsSignerTrusted("ed25519:eeee5555ffff6666", policy, _keyDir).Should().BeFalse("a stranger");
        OperatorKey.IsSignerTrusted("(unsigned)", policy, _keyDir).Should().BeFalse("unsigned is fail-closed");
    }

    [Fact]
    public void IsSignerTrusted_AlwaysTrustsTheNodesOwnOperatorKey()
    {
        // A node publishing and re-importing its own package needs no ceremony: its own operator
        // fingerprint is trusted even with an empty policy and empty keychain. A different key is not.
        var self = OperatorKey.Generate(_keyDir);

        OperatorKey.IsSignerTrusted(self.Fingerprint, System.Array.Empty<string>(), _keyDir)
            .Should().BeTrue("a node trusts the key it signs with");
        OperatorKey.IsSignerTrusted(A, System.Array.Empty<string>(), _keyDir)
            .Should().BeFalse("another key is still a stranger until trusted");
    }

    [Fact]
    public void TrustSetDigest_IsOrderIndependent_AndChangesWithMembership()
    {
        var d1 = OperatorKey.TrustSetDigest(new[] { A, B });
        var d2 = OperatorKey.TrustSetDigest(new[] { B, A });
        var d3 = OperatorKey.TrustSetDigest(new[] { A });

        d1.Should().Be(d2, "the digest is over the sorted set, so order does not matter");
        d1.Should().NotBe(d3, "a box that dropped a signer must show a different digest");
    }

    [Fact]
    public void TheRotationDirectory_IsNeverAnAdmissionInput()
    {
        // Simulate a rotation: the operator's OWN old public key lands under trusted/. That must
        // NOT make its fingerprint trusted for admitting imports — only peers/ (keys trust) does.
        var trustedDir = Path.Combine(_keyDir, "trusted");
        Directory.CreateDirectory(trustedDir);
        File.WriteAllText(Path.Combine(trustedDir, A.Replace(':', '-') + ".pub"), "some-old-public-key");

        OperatorKey.ListTrusted(_keyDir).Should().BeEmpty("trusted/ is rotation retention, not an admission allowlist");
        OperatorKey.IsSignerTrusted(A, Array.Empty<string>(), _keyDir).Should().BeFalse(
            "a rotated-away key in trusted/ must never re-authorize itself for admission");
    }
}
