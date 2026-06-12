using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.API.Security;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

public sealed class PrivateLicenseValidatorTests
{
    [Fact]
    public void GetStatus_WhenNoLicenseConfigured_ReturnsNotConfigured()
    {
        var validator = CreateValidator(new NexoPrivateLicenseOptions(), contentRoot: Path.GetTempPath());

        var status = validator.GetStatus();

        status.State.Should().Be(PrivateLicenseState.NotConfigured);
    }

    [Fact]
    public void GetStatus_WhenLicenseValid_ReturnsValid()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"nexo-lic-{Guid.NewGuid():N}")).FullName;
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
            new NexoPrivateLicenseOptions { LicenseFilePath = path },
            contentRoot: dir);

        validator.GetStatus().State.Should().Be(PrivateLicenseState.Valid);
    }

    [Fact]
    public void GetStatus_WhenLicenseExpired_ReturnsExpired()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"nexo-lic-{Guid.NewGuid():N}")).FullName;
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
            new NexoPrivateLicenseOptions { LicenseFilePath = path },
            contentRoot: dir);

        validator.GetStatus().State.Should().Be(PrivateLicenseState.Expired);
    }

    private static PrivateLicenseValidator CreateValidator(NexoPrivateLicenseOptions options, string contentRoot) =>
        new(
            Options.Create(options),
            new StubHostEnvironment(contentRoot),
            NullLogger<PrivateLicenseValidator>.Instance);

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public StubHostEnvironment(string contentRoot) => ContentRootPath = contentRoot;

        public string EnvironmentName { get; set; } = Microsoft.Extensions.Hosting.Environments.Development;
        public string ApplicationName { get; set; } = "Nexo.Tests";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
