using FluentAssertions;
using Nexo.Transport.Grpc;
using Xunit;

namespace Nexo.Tests.Transport;

public sealed class GrpcTransportOptionsTests
{
    [Fact]
    public void Validate_WhenAllowInsecureInNonDevelopment_Throws()
    {
        var options = new GrpcTransportOptions
        {
            AllowInsecure = true
        };

        var act = () => options.Validate(isDevelopment: false);

        act.Should().Throw<InvalidOperationException>();
    }
}
