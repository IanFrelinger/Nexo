using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Text;
using FluentAssertions;
using Nexo.Infrastructure.Testing;
using Nexo.Infrastructure.Testing.Docker;
using Nexo.Infrastructure.Testing.ExecutionPlatform;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Testing;

public sealed class TarArchiveHelperGapCoverageTests
{
    [Fact]
    public void CreateBuildContextTar_includes_dockerfile_and_context_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tar-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var dockerfile = Path.Combine(root, "Dockerfile");
        File.WriteAllText(dockerfile, "FROM scratch");
        File.WriteAllText(Path.Combine(root, "global.json"), """{"sdk":{"version":"8.0.0"}}""");

        var srcDir = Path.Combine(root, "src");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "Program.cs"), "Console.WriteLine();");

        try
        {
            using var tarGz = TarArchiveHelper.CreateBuildContextTar(root, dockerfile);
            using var gzip = new GZipStream(tarGz, CompressionMode.Decompress, leaveOpen: true);
            using var reader = new TarReader(gzip);

            var entries = new List<string>();
            while (reader.GetNextEntry() is { } entry)
            {
                if (!string.IsNullOrEmpty(entry.Name))
                    entries.Add(entry.Name.Replace('\\', '/'));
            }

            entries.Should().Contain("Dockerfile");
            entries.Should().Contain("global.json");
            entries.Should().Contain(entry => entry.Contains("src/Program.cs", StringComparison.Ordinal));
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}

public sealed class UnitTestFrameworkBridgeGapCoverageTests
{
    [Fact]
    public void DiscoverUnitTestTypesFromAssembly_throws_for_null_assembly()
    {
        var act = () => UnitTestFrameworkBridge.DiscoverUnitTestTypesFromAssembly(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DiscoverUnitTestTypesFromAssembly_excludes_infrastructure_helper_types()
    {
        var assembly = typeof(UnitTestFrameworkBridgeGapCoverageTests).Assembly;
        var types = UnitTestFrameworkBridge.DiscoverUnitTestTypesFromAssembly(assembly);

        types.Should().NotContain(t => t.Name == "SimpleTestForRunner");
        types.Should().NotContain(t => t.Name == "DependencyWrappingArchitectureTests");
    }

    [Fact]
    public async Task ExecuteUnitTestAsync_throws_for_invalid_type()
    {
        var act = async () => await UnitTestFrameworkBridge.ExecuteUnitTestAsync(typeof(string));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("testType");
    }
}

public sealed class RemoteExecutionPlatformGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_http_client()
    {
        var act = () => new RemoteExecutionPlatform(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public async Task BuildImageAsync_returns_failure_details_from_remote_response()
    {
        var handler = new FakeTestingHandler((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("build", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """{"success":false,"errorMessage":"bad dockerfile","durationMs":25}""");
            }

            return Json(HttpStatusCode.NotFound, "{}");
        });

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://remote.example/") };
        var platform = new RemoteExecutionPlatform(client);

        var result = await platform.BuildImageAsync("Dockerfile", "tag:latest", ".");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("bad dockerfile");
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class FakeTestingHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
