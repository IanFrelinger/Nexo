using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Xunit;

namespace Nexo.Ingress.AwsSns.Tests;

/// <summary>Tests for sns revocation mode parser.</summary>
public sealed class SnsRevocationModeParserTests
{
    /// <summary>Parse_maps_expected_modes.</summary>
    /// <param name="input">Input.</param>
    /// <param name="expected">Expected.</param>
    [Theory]
    [InlineData(null, X509RevocationMode.NoCheck)]
    [InlineData("", X509RevocationMode.NoCheck)]
    [InlineData("NoCheck", X509RevocationMode.NoCheck)]
    [InlineData("online", X509RevocationMode.Online)]
    [InlineData("OFFLINE", X509RevocationMode.Offline)]
    public void Parse_maps_expected_modes(string? input, X509RevocationMode expected) =>
        SnsRevocationModeParser.Parse(input).Should().Be(expected);
}
