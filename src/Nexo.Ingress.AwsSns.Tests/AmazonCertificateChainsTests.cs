using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Xunit;

namespace Nexo.Ingress.AwsSns.Tests;

public sealed class AmazonCertificateChainsTests
{
    [Fact]
    public void Self_signed_leaf_is_rejected()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            "CN=unit-test.fake.local",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        AmazonCertificateChains.TryValidateSnsSigningCertificate(cert).Should().BeFalse();
    }
}
