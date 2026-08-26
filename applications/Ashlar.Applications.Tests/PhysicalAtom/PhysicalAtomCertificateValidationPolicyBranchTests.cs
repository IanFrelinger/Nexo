using FluentAssertions;
using Ashlar.Certification.Physical;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Branch tests for the binding-scope validation policy. Every failure code the
/// policy can emit is pinned here — this is the certificate rejection surface, so
/// each branch is security-relevant.
/// </summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomCertificateValidationPolicyBranchTests
{
    private static readonly string ValidHash = new('a', 64);

    private static PhysicalAtomCertificate ValidDesignCert() => new()
    {
        AtomId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        BindingScope = BindingScope.Design,
        AssetHash = ValidHash,
        AssetVersion = "1.2.3"
    };

    private static (bool Ok, string? Code, string? Reason) Validate(PhysicalAtomCertificate certificate)
    {
        var ok = PhysicalAtomCertificateValidationPolicy.ValidateBindingScopeConsistency(
            certificate, out var code, out var reason);
        return (ok, code, reason);
    }

    [Fact]
    public void R1_EmptyAtomId_RejectedAsAtomIdInvalid()
    {
        var (ok, code, reason) = Validate(ValidDesignCert() with { AtomId = Guid.Empty });

        ok.Should().BeFalse();
        code.Should().Be("atom-id-invalid");
        reason.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("abc123")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void R2_InvalidAssetHash_RejectedAsAssetHashInvalid(string assetHash)
    {
        var (ok, code, _) = Validate(ValidDesignCert() with { AssetHash = assetHash });

        ok.Should().BeFalse();
        code.Should().Be("asset-hash-invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1.2")]
    [InlineData("v1.2.3")]
    [InlineData("01.2.3")]
    [InlineData("1.2.3.4")]
    public void R3_InvalidAssetVersion_RejectedAsAssetVersionInvalid(string assetVersion)
    {
        var (ok, code, _) = Validate(ValidDesignCert() with { AssetVersion = assetVersion });

        ok.Should().BeFalse();
        code.Should().Be("asset-version-invalid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void R4_UnsupportedSchemaVersion_Rejected(int schemaVersion)
    {
        var (ok, code, reason) = Validate(ValidDesignCert() with { SchemaVersion = schemaVersion });

        ok.Should().BeFalse();
        code.Should().Be("schema-version-unsupported");
        reason.Should().Contain(schemaVersion.ToString());
    }

    [Fact]
    public void R5_DesignScope_PopulatedManufactureMeta_Rejected()
    {
        var cert = ValidDesignCert() with
        {
            ManufactureMeta = new ManufactureMeta("batch-1", "serial-1", null)
        };

        var (ok, code, _) = Validate(cert);

        ok.Should().BeFalse();
        code.Should().Be("binding-scope-manufacture-meta-forbidden");
    }

    [Fact]
    public void R6_InstanceScope_NullManufactureMeta_Rejected()
    {
        var cert = ValidDesignCert() with { BindingScope = BindingScope.Instance, ManufactureMeta = null };

        var (ok, code, _) = Validate(cert);

        ok.Should().BeFalse();
        code.Should().Be("binding-scope-manufacture-meta-required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void R7_InstanceScope_BlankSerialNumber_Rejected(string? serialNumber)
    {
        var cert = ValidDesignCert() with
        {
            BindingScope = BindingScope.Instance,
            ManufactureMeta = new ManufactureMeta("batch-1", serialNumber, null)
        };

        var (ok, code, _) = Validate(cert);

        ok.Should().BeFalse();
        code.Should().Be("binding-scope-serial-required");
    }

    [Fact]
    public void R8_BatchScope_NullManufactureMeta_Rejected()
    {
        var cert = ValidDesignCert() with { BindingScope = BindingScope.Batch, ManufactureMeta = null };

        var (ok, code, _) = Validate(cert);

        ok.Should().BeFalse();
        code.Should().Be("binding-scope-manufacture-meta-required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void R9_BatchScope_BlankBatchId_Rejected(string? batchId)
    {
        var cert = ValidDesignCert() with
        {
            BindingScope = BindingScope.Batch,
            ManufactureMeta = new ManufactureMeta(batchId, "serial-1", null)
        };

        var (ok, code, _) = Validate(cert);

        ok.Should().BeFalse();
        code.Should().Be("binding-scope-batch-required");
    }

    [Fact]
    public void R10_UnknownBindingScope_RejectedNotDefaulted()
    {
        var cert = ValidDesignCert() with { BindingScope = (BindingScope)255 };

        var (ok, code, reason) = Validate(cert);

        ok.Should().BeFalse();
        code.Should().Be("binding-scope-unknown");
        reason.Should().Contain("255");
    }

    [Fact]
    public void R11_InconsistentGeoAnchor_RejectedAsGeoAnchorInconsistent()
    {
        var cert = ValidDesignCert() with
        {
            GeoAnchor = new GeoAnchor(37.7749, -122.4194, 9, "000000000000000")
        };

        var (ok, code, reason) = Validate(cert);

        ok.Should().BeFalse();
        code.Should().Be("geo-anchor-inconsistent");
        reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void A1_DesignScope_NoMeta_Accepted()
    {
        var (ok, code, reason) = Validate(ValidDesignCert());

        ok.Should().BeTrue();
        code.Should().BeNull();
        reason.Should().BeNull();
    }

    [Fact]
    public void A2_InstanceScope_WithSerial_Accepted()
    {
        var cert = ValidDesignCert() with
        {
            BindingScope = BindingScope.Instance,
            ManufactureMeta = new ManufactureMeta(null, "serial-1", DateTimeOffset.UtcNow)
        };

        Validate(cert).Ok.Should().BeTrue();
    }

    [Fact]
    public void A3_BatchScope_WithBatchId_Accepted()
    {
        var cert = ValidDesignCert() with
        {
            BindingScope = BindingScope.Batch,
            ManufactureMeta = new ManufactureMeta("batch-1", null, null)
        };

        Validate(cert).Ok.Should().BeTrue();
    }

    [Fact]
    public void A4_ConsistentGeoAnchor_Accepted()
    {
        var cert = ValidDesignCert() with
        {
            GeoAnchor = new GeoAnchor(37.7749, -122.4194, 9, "897016d01d3ffff")
        };

        Validate(cert).Ok.Should().BeTrue();
    }
}
