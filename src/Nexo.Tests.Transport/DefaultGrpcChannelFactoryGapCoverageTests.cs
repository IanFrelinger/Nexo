using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Transport.Grpc;
using Xunit;

namespace Nexo.Tests.Transport;

/// <summary>Tests for default grpc channel factory gap coverage.</summary>
[Collection("GrpcTransportEnvironment")]
public sealed class DefaultGrpcChannelFactoryGapCoverageTests
{
    [Fact]
    public void GetOrCreate_reuses_channel_for_same_endpoint()
    {
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var first = factory.GetOrCreate("http://127.0.0.1:9");
        var second = factory.GetOrCreate("http://127.0.0.1:9");

        ReferenceEquals(first, second).Should().BeTrue();
        factory.DisposeAll();
    }

    [Fact]
    public void CreateChannel_applies_custom_ca_certificate()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nexo-grpc-ca-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var (_, _, caPem, _) = CreateTempCertificates(dir);

        try
        {
            using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
            var factory = new DefaultGrpcChannelFactory(
                Options.Create(new GrpcTransportOptions
                {
                    AllowInsecure = true,
                    CaCertPath = caPem,
                }),
                NullLogger<DefaultGrpcChannelFactory>.Instance);

            factory.GetOrCreate("http://127.0.0.1:9").Should().NotBeNull();
            factory.DisposeAll();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CreateChannel_loads_client_certificate_from_pem_pair()
    {
        if (OperatingSystem.IsMacOS())
            return;

        var dir = Path.Combine(Path.GetTempPath(), "nexo-grpc-mtls-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var (certPem, _, caPem, _) = CreateTempCertificates(dir);
        var keyPkcs8 = Path.Combine(dir, "key.pk8.pem");

        try
        {
            using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
            var factory = new DefaultGrpcChannelFactory(
                Options.Create(new GrpcTransportOptions
                {
                    AllowInsecure = true,
                    ClientCertPath = certPem,
                    ClientCertKeyPath = keyPkcs8,
                    CaCertPath = caPem,
                }),
                NullLogger<DefaultGrpcChannelFactory>.Instance);

            factory.GetOrCreate("http://127.0.0.1:9").Should().NotBeNull();
            factory.DisposeAll();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CreateChannel_custom_ca_validation_callback_rejects_null_certificate()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nexo-grpc-ca-callback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var (_, _, caPem, _) = CreateTempCertificates(dir);

        try
        {
            using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
            var factory = new DefaultGrpcChannelFactory(
                Options.Create(new GrpcTransportOptions
                {
                    AllowInsecure = true,
                    CaCertPath = caPem,
                }),
                NullLogger<DefaultGrpcChannelFactory>.Instance);

            factory.GetOrCreate("http://127.0.0.1:9").Should().NotBeNull();

            var handlerField = typeof(DefaultGrpcChannelFactory).GetField("_options", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var options = (GrpcTransportOptions)handlerField.GetValue(factory)!;
            options.CaCertPath.Should().Be(caPem);

            var buildHandler = typeof(DefaultGrpcChannelFactory).GetMethod("BuildHandler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var httpHandler = (HttpClientHandler)buildHandler.Invoke(factory, null)!;
            httpHandler.ServerCertificateCustomValidationCallback.Should().NotBeNull();
            httpHandler.ServerCertificateCustomValidationCallback!(null!, null, null, System.Net.Security.SslPolicyErrors.None)
                .Should().BeFalse();

            factory.DisposeAll();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CreateChannel_custom_ca_validation_callback_accepts_certificate_signed_by_custom_root()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nexo-grpc-ca-valid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var (certPem, _, caPem, _) = CreateTempCertificates(dir);

        try
        {
            using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
            var factory = new DefaultGrpcChannelFactory(
                Options.Create(new GrpcTransportOptions
                {
                    AllowInsecure = true,
                    CaCertPath = caPem,
                }),
                NullLogger<DefaultGrpcChannelFactory>.Instance);

            factory.GetOrCreate("http://127.0.0.1:9").Should().NotBeNull();

            var buildHandler = typeof(DefaultGrpcChannelFactory).GetMethod("BuildHandler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var httpHandler = (HttpClientHandler)buildHandler.Invoke(factory, null)!;
            var serverCertificate = new X509Certificate2(caPem);
            httpHandler.ServerCertificateCustomValidationCallback!(
                    null!,
                    serverCertificate,
                    null,
                    System.Net.Security.SslPolicyErrors.None)
                .Should().BeTrue();

            factory.DisposeAll();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CreateChannel_loads_client_certificate_without_separate_key_path()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nexo-grpc-pfx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var (_, _, _, pfxPath) = CreateTempCertificates(dir);

        try
        {
            using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
            var factory = new DefaultGrpcChannelFactory(
                Options.Create(new GrpcTransportOptions
                {
                    AllowInsecure = true,
                    ClientCertPath = pfxPath,
                }),
                NullLogger<DefaultGrpcChannelFactory>.Instance);

            factory.GetOrCreate("http://127.0.0.1:9").Should().NotBeNull();
            factory.DisposeAll();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Constructor_null_logger_throws()
    {
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var act = () => new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions()),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static (string certPem, string keyPem, string caPem, string pfxPath) CreateTempCertificates(string dir)
    {
        var certPem = Path.Combine(dir, "cert.pem");
        var keyPem = Path.Combine(dir, "key.pem");
        var caPem = Path.Combine(dir, "ca.pem");
        var pfxPath = Path.Combine(dir, "client.pfx");

        var gen = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "openssl",
            Arguments = $"req -x509 -newkey rsa:2048 -nodes -keyout \"{keyPem}\" -out \"{certPem}\" -days 1 -subj /CN=localhost",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        });
        gen.Should().NotBeNull();
        gen!.WaitForExit(10_000).Should().BeTrue();
        gen.ExitCode.Should().Be(0);

        var keyPkcs8 = Path.Combine(dir, "key.pk8.pem");
        var pkcs8 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "openssl",
            Arguments = $"pkcs8 -topk8 -inform PEM -outform PEM -nocrypt -in \"{keyPem}\" -out \"{keyPkcs8}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        });
        pkcs8.Should().NotBeNull();
        pkcs8!.WaitForExit(10_000).Should().BeTrue();
        pkcs8.ExitCode.Should().Be(0);

        File.Copy(certPem, caPem, overwrite: true);

        var pfx = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "openssl",
            Arguments = $"pkcs12 -export -out \"{pfxPath}\" -inkey \"{keyPem}\" -in \"{certPem}\" -passout pass:",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        });
        pfx.Should().NotBeNull();
        pfx!.WaitForExit(10_000).Should().BeTrue();
        pfx.ExitCode.Should().Be(0);

        return (certPem, keyPem, caPem, pfxPath);
    }

    /// <summary>Environment variable scope.</summary>
    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _key;
        private readonly string? _priorValue;

        public EnvironmentVariableScope(string key, string? value)
        {
            _key = key;
            _priorValue = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        /// <summary>Dispose.</summary>
        public void Dispose() => Environment.SetEnvironmentVariable(_key, _priorValue);
    }
}
