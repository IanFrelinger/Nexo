using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Transport.Grpc.Server;
using Xunit;

namespace Nexo.Tests.Transport;

public sealed class AgentTransportServiceImplReflectionGapCoverageTests
{
    [Fact]
    public void DeserializeJson_returns_raw_string_when_json_invalid()
    {
        var result = InvokeStatic<object?>("DeserializeJson", "not-json{{");
        result.Should().Be("not-json{{");
    }

    [Fact]
    public void SerializeOutput_supports_readonly_object_dictionary()
    {
        IReadOnlyDictionary<string, object> output = new Dictionary<string, object>
        {
            ["count"] = 3,
        };

        var serialized = InvokeStatic<Dictionary<string, string>>("SerializeOutput", output)!;

        serialized.Should().ContainKey("count");
        serialized["count"].Should().Be("3");
    }

    [Fact]
    public void TryGetBearerToken_accepts_non_bearer_authorization_values()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["authorization"] = "ApiKey abc123",
        };

        var token = InvokeStatic<string?>("TryGetBearerToken", headers);
        token.Should().Be("ApiKey abc123");
    }

    [Fact]
    public void TryGetApiKey_reads_custom_header()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["x-nexo-api-key"] = "secret-key",
        };

        var apiKey = InvokeStatic<string?>("TryGetApiKey", headers);
        apiKey.Should().Be("secret-key");
    }

    [Fact]
    public void TryGetBearerToken_strips_bearer_prefix()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["authorization"] = "Bearer jwt-token-123",
        };

        var token = InvokeStatic<string?>("TryGetBearerToken", headers);
        token.Should().Be("jwt-token-123");
    }

    [Fact]
    public void ToHeaderDictionary_skips_binary_and_blank_keys()
    {
        var metadata = new Metadata
        {
            { "valid", "value" },
            { "blank-value", string.Empty },
            { "binary-bin", new byte[] { 1, 2, 3 } },
        };
        metadata.Add(new Metadata.Entry(string.Empty, "ignored"));
        var context = GrpcServerCallContextFactory.Create(metadata);

        var headers = InvokeStatic<Dictionary<string, string>>("ToHeaderDictionary", context)!;

        headers.Should().ContainKey("valid").WhoseValue.Should().Be("value");
        headers.Should().ContainKey("blank-value").WhoseValue.Should().BeEmpty();
        headers.Should().NotContainKey(string.Empty);
        headers.Should().NotContainKey("binary-bin");
    }

    [Fact]
    public void ExtractCertificateSubjects_returns_empty_when_certificate_missing()
    {
        var context = GrpcServerCallContextFactory.Create(new Metadata());

        var subjects = InvokeStatic<IReadOnlyList<string>>("ExtractCertificateSubjects", context)!;

        subjects.Should().BeEmpty();
    }

    [Fact]
    public void ExtractCertificateSubjects_returns_client_certificate_subject()
    {
        using var cert = CreateCertificateWithSan("localhost", "agent.local");
        var context = GrpcServerCallContextFactory.Create(new Metadata(), cert);

        var subjects = InvokeStatic<IReadOnlyList<string>>("ExtractCertificateSubjects", context)!;

        subjects.Should().ContainSingle(s => s.Contains("CN=localhost", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtractCertificateSans_parses_subject_alternative_names()
    {
        using var cert = CreateCertificateWithSan("localhost", "agent.local");
        var context = GrpcServerCallContextFactory.Create(new Metadata(), cert);

        var sans = InvokeStatic<IReadOnlyList<string>>("ExtractCertificateSans", context)!;

        sans.Should().HaveCountGreaterThanOrEqualTo(2);
        sans.Should().Contain(s => s.Contains("agent.local", StringComparison.Ordinal));
        sans.Should().Contain(s => s.Contains("localhost", StringComparison.Ordinal));
    }

    private static X509Certificate2 CreateCertificateWithSan(string commonName, string dnsName)
    {
        var dir = Path.Combine(Path.GetTempPath(), "nexo-agent-transport-cert-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var certPem = Path.Combine(dir, "cert.pem");
        var keyPem = Path.Combine(dir, "key.pem");

        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "openssl",
                Arguments =
                    $"req -x509 -newkey rsa:2048 -nodes -keyout \"{keyPem}\" -out \"{certPem}\" -days 1 " +
                    $"-subj /CN={commonName} -addext \"subjectAltName=DNS:{commonName},DNS:{dnsName}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            process.Should().NotBeNull();
            process!.WaitForExit(10_000).Should().BeTrue();
            process.ExitCode.Should().Be(0);

            return new X509Certificate2(certPem);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private static T? InvokeStatic<T>(string methodName, params object?[] args)
    {
        var method = typeof(AgentTransportServiceImpl).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return (T?)method!.Invoke(null, args);
    }
}
