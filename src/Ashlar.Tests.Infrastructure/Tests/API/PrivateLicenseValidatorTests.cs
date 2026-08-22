using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.API.Security;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.API;

/// <summary>Tests for private license validator.</summary>
public sealed class PrivateLicenseValidatorTests
{
    [Fact]
    public void GetStatus_WhenNoLicenseConfigured_ReturnsNotConfigured()
    {
        var validator = CreateValidator(new AshlarPrivateLicenseOptions(), contentRoot: Path.GetTempPath());

        var status = validator.GetStatus();

        status.State.Should().Be(PrivateLicenseState.NotConfigured);
    }

    [Fact]
    public void GetStatus_WhenLicenseValid_ReturnsValid()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"ashlar-lic-{Guid.NewGuid():N}")).FullName;
        var path = Path.Combine(dir, "license.json");
        var document = new PrivateLicenseDocument
        {
            CustomerId = "acme",
            TenantId = "acme-pilot",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            Seats = 5,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document));

        var validator = CreateValidator(
            new AshlarPrivateLicenseOptions { LicenseFilePath = path },
            contentRoot: dir);

        validator.GetStatus().State.Should().Be(PrivateLicenseState.Valid);
    }

    [Fact]
    public void GetStatus_WhenLicenseExpired_ReturnsExpired()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"ashlar-lic-{Guid.NewGuid():N}")).FullName;
        var path = Path.Combine(dir, "license.json");
        var document = new PrivateLicenseDocument
        {
            CustomerId = "acme",
            TenantId = "acme-pilot",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            Seats = 5,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document));

        var validator = CreateValidator(
            new AshlarPrivateLicenseOptions { LicenseFilePath = path },
            contentRoot: dir);

        validator.GetStatus().State.Should().Be(PrivateLicenseState.Expired);
    }

    /// <summary>Creates validator.</summary>
    /// <param name="options">Options.</param>
    /// <param name="contentRoot">Content root.</param>
    private static PrivateLicenseValidator CreateValidator(AshlarPrivateLicenseOptions options, string contentRoot) =>
        new(
            Options.Create(options),
            new StubHostEnvironment(contentRoot),
            NullLogger<PrivateLicenseValidator>.Instance);

    /// <summary>Tests for stub host environment.</summary>
    private sealed class StubHostEnvironment : IHostEnvironment
    {
        /// <summary>Stub host environment.</summary>
        /// <param name="contentRoot">Content root.</param>
        public StubHostEnvironment(string contentRoot) => ContentRootPath = contentRoot;

        /// <summary>Environment name.</summary>
        public string EnvironmentName { get; set; } = Microsoft.Extensions.Hosting.Environments.Development;
        /// <summary>Application name.</summary>
        public string ApplicationName { get; set; } = "Ashlar.Tests";
        /// <summary>Content root path.</summary>
        public string ContentRootPath { get; set; }
        /// <summary>Content root file provider.</summary>
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
