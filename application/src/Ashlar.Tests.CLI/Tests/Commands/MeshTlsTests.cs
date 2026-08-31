using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// mTLS for a private fleet: the serve endpoint over TLS requiring a client cert the fleet CA signed.
/// Uses REAL certificates (generated in-test) and the real Kestrel server + HttpClient on loopback, so
/// it proves the whole chain: a fleet member gets in, a client with no cert or an outsider CA's cert is
/// rejected at the TLS handshake. The Ed25519 package trust is unchanged and orthogonal — this is the
/// transport access-control layer on top.
/// </summary>
[Xunit.Collection("MeshIntegration")]
public sealed class MeshTlsTests : IDisposable
{
    private readonly string _dir;
    private readonly string _published;

    public MeshTlsTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-mtls-" + Guid.NewGuid().ToString("N"));
        _published = Path.Combine(_dir, "published");
        Directory.CreateDirectory(_published);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    // PKCS#12 round-trip so the returned cert owns a PERSISTENT private key — the inner RSA can be
    // disposed and the CA can still sign leaves (a `using var key` would invalidate the key handle).
    private static X509Certificate2 Persist(X509Certificate2 cert) =>
        X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pkcs12), password: null);

    private static X509Certificate2 MakeCa(string cn)
    {
        using var key = RSA.Create(2048);
        var req = new CertificateRequest($"CN={cn}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        using var self = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
        return Persist(self);
    }

    private static X509Certificate2 MakeLeaf(string cn, X509Certificate2 ca, bool serverAuth)
    {
        using var key = RSA.Create(2048);
        var req = new CertificateRequest($"CN={cn}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        var eku = new OidCollection { new Oid(serverAuth ? "1.3.6.1.5.5.7.3.1" : "1.3.6.1.5.5.7.3.2") };
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, false));
        if (serverAuth)
        {
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(IPAddress.Loopback);
            req.CertificateExtensions.Add(san.Build());
        }
        var serial = new byte[16];
        RandomNumberGenerator.Fill(serial);
        // Clamp the leaf's validity to the CA's exact window — a leaf that outlives its issuer by even
        // a sub-second (both computed from UtcNow.AddHours(1) moments apart) is rejected.
        using var cert = req.Create(ca, ca.NotBefore, ca.NotAfter, serial);
        using var withKey = cert.CopyWithPrivateKey(key);
        return Persist(withKey);
    }

    private (string cert, string key) WritePem(X509Certificate2 c, string name)
    {
        var certPath = Path.Combine(_dir, name + ".crt");
        var keyPath = Path.Combine(_dir, name + ".key");
        File.WriteAllText(certPath, c.ExportCertificatePem());
        File.WriteAllText(keyPath, c.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem());
        return (certPath, keyPath);
    }

    private string WriteCaPem(X509Certificate2 ca, string name)
    {
        var caPath = Path.Combine(_dir, name + ".crt");
        File.WriteAllText(caPath, ca.ExportCertificatePem());
        return caPath;
    }

    private async Task<(MeshServeService svc, int port)> StartMtlsServeAsync(string serverCert, string serverKey, string caPath)
    {
        var port = FreePort();
        var svc = new MeshServeService(
            new MeshServeSettings(port, _published, "fleet-node", serverCert, serverKey, RequireClientCert: true, CaPath: caPath),
            NullLogger<MeshServeService>.Instance);
        await svc.StartAsync(CancellationToken.None);
        await WaitForPortAsync(port);
        return (svc, port);
    }

    // BackgroundService.StartAsync returns before Kestrel finishes binding; wait for the port.
    private static async Task WaitForPortAsync(int port)
    {
        for (var i = 0; i < 100; i++)
        {
            try
            {
                using var probe = new TcpClient();
                await probe.ConnectAsync(IPAddress.Loopback, port);
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(50);
            }
        }
    }

    private static HttpClient ClientTrusting(X509Certificate2Collection caBundle, X509Certificate2? clientCert)
    {
        var handler = new SocketsHttpHandler();
        handler.SslOptions.RemoteCertificateValidationCallback = (_, cert, _, _) => MeshTls.ChainsToCa(MeshTls.AsCert2(cert), caBundle);
        if (clientCert is not null)
        {
            handler.SslOptions.ClientCertificates = new X509CertificateCollection { clientCert };
        }
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
    }

    [Fact]
    public async Task Mtls_fleetMember_isAdmitted_stranger_andCertless_areRejected()
    {
        using var ca = MakeCa("fleet-ca");
        using var otherCa = MakeCa("outsider-ca");
        using var serverCert = MakeLeaf("fleet-node", ca, serverAuth: true);
        using var memberCert = MakeLeaf("member-1", ca, serverAuth: false);
        using var outsiderCert = MakeLeaf("outsider", otherCa, serverAuth: false);

        var (sc, sk) = WritePem(serverCert, "server");
        var caPath = WriteCaPem(ca, "ca");
        var caBundle = MeshTls.LoadCaBundle(caPath);

        var (svc, port) = await StartMtlsServeAsync(sc, sk, caPath);
        var url = $"https://localhost:{port}/mesh/v1/hello";
        try
        {
            // (1) fleet member with a CA-signed client cert → admitted
            using (var member = ClientTrusting(caBundle, memberCert))
            {
                var r = await member.GetAsync(url);
                r.IsSuccessStatusCode.Should().BeTrue("a client cert the fleet CA signed is admitted");
                (await r.Content.ReadAsStringAsync()).Should().Contain("fleet-node");
            }

            // (2) no client cert → rejected at the handshake
            using (var certless = ClientTrusting(caBundle, clientCert: null))
            {
                var act = async () => await certless.GetAsync(url);
                await act.Should().ThrowAsync<HttpRequestException>("mTLS requires a client cert");
            }

            // (3) outsider cert (signed by a different CA) → rejected
            using (var outsider = ClientTrusting(caBundle, outsiderCert))
            {
                var act = async () => await outsider.GetAsync(url);
                await act.Should().ThrowAsync<HttpRequestException>("a cert the fleet CA did not sign is not a fleet identity");
            }
        }
        finally { await svc.StopAsync(CancellationToken.None); }
    }

    [Theory]
    // The intended configs are coherent (no error):
    [InlineData(false, false, false, false, false)]   // no TLS — plaintext LAN default
    [InlineData(true, true, false, false, false)]     // TLS server-only
    [InlineData(true, true, true, true, false)]       // full mTLS
    // These must FAIL CLOSED rather than degrade to plaintext:
    [InlineData(false, false, true, true, true)]      // require client cert, but NO server cert/key
    [InlineData(true, false, false, false, true)]     // cert without key
    [InlineData(false, true, false, false, true)]     // key without cert
    [InlineData(true, true, true, false, true)]       // require client cert, but NO CA to validate it
    public void ConfigError_failsClosedOnHalfSpecifiedTls(bool cert, bool key, bool requireClient, bool ca, bool expectError)
    {
        var settings = new MeshServeSettings(
            7420, _published, "n",
            cert ? "/x/cert.pem" : null, key ? "/x/key.pem" : null, requireClient, ca ? "/x/ca.pem" : null);

        var error = MeshServeService.ConfigError(settings);

        (error is not null).Should().Be(expectError,
            "a private-fleet TLS setup must never silently fall back to plaintext");
    }

    [Fact]
    public async Task Tls_serverOnly_encryptsAndServes_withCaValidation()
    {
        using var ca = MakeCa("fleet-ca");
        using var serverCert = MakeLeaf("fleet-node", ca, serverAuth: true);
        await File.WriteAllTextAsync(Path.Combine(_published, "demo-abcdef.ashpkg"), "{\"ok\":true}");

        var (sc, sk) = WritePem(serverCert, "server");
        var caBundle = MeshTls.LoadCaBundle(WriteCaPem(ca, "ca"));

        var port = FreePort();
        var svc = new MeshServeService(
            new MeshServeSettings(port, _published, "fleet-node", sc, sk),   // TLS, no client-cert requirement
            NullLogger<MeshServeService>.Instance);
        await svc.StartAsync(CancellationToken.None);
        await WaitForPortAsync(port);
        try
        {
            using var client = ClientTrusting(caBundle, clientCert: null);
            var index = await client.GetStringAsync($"https://localhost:{port}/mesh/v1/index");
            index.Should().Contain("demo-abcdef.ashpkg");
        }
        finally { await svc.StopAsync(CancellationToken.None); }
    }
}
