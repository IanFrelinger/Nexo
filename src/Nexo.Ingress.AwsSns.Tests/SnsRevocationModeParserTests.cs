using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Xunit;

namespace Nexo.Ingress.AwsSns.Tests;

public sealed class SnsRevocationModeParserTests
{
    [Theory]
    [InlineData(null, X509RevocationMode.NoCheck)]
    [InlineData("", X509RevocationMode.NoCheck)]
    [InlineData("NoCheck", X509RevocationMode.NoCheck)]
    [InlineData("online", X509RevocationMode.Online)]
    [InlineData("OFFLINE", X509RevocationMode.Offline)]
    public void Parse_maps_expected_modes(string? input, X509RevocationMode expected) =>
        SnsRevocationModeParser.Parse(input).Should().Be(expected);
}
