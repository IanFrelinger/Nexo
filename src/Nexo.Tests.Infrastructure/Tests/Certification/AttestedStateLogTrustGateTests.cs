using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Certification.State;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Certification.Sdk.Extensions;
using Nexo.Tests.Infrastructure.Certification.Reuse;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class AttestedStateLogTrustGateTests
{
    private const string HmacKey = "attested-state-log-test-hmac";

    [Fact]
    public void TrustGate_FromDi_TrustsBundledSampleLog()
    {
        var artifactRoot = PhaseWitnessPaths.ArtifactRoot();
        var schema = AttestedStateLogWireFormat.DeserializeSchema(File.ReadAllText(PhaseWitnessPaths.SchemaPath(artifactRoot)));
        var log = AttestedStateLogWireFormat.DeserializeLog(File.ReadAllText(PhaseWitnessPaths.LogPath(artifactRoot)));
        var catalog = new FileCertifiedBehaviorCatalog(PhaseWitnessPaths.BehaviorRoot(artifactRoot));

        var services = new ServiceCollection();
        services.AddAttestedStateLogTrust(catalog);
        var gate = services.BuildServiceProvider().GetRequiredService<IAttestedStateLogTrustGate>();

        var result = gate.Verify(log, schema, HmacKey);

        result.Trusted.Should().BeTrue($"expected TRUSTED, got {result.FailureCode}: {result.Reason}");
        result.VerifiedTransitions.Should().Be(3);
    }
}
