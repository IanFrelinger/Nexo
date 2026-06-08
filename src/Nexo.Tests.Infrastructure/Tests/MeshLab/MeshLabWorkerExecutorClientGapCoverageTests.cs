using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Infrastructure.MeshLab;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.MeshLab;

public sealed class MeshLabWorkerExecutorClientGapCoverageTests
{
    [Fact]
    public void PickCandidate_returns_null_for_blank_prefix()
    {
        var tasks = new[]
        {
            new MeshLabTaskSnapshot("a", "mesh-lab-worker-exec-x", "Assigned", "http://peer:8080", "tok"),
        };

        MeshLabWorkerExecutorClient.PickCandidate(tasks, "").Should().BeNull();
        MeshLabWorkerExecutorClient.PickCandidate(tasks, "   ").Should().BeNull();
    }

    [Fact]
    public void PickCandidate_skips_tasks_without_lease_or_peer_url()
    {
        var tasks = new[]
        {
            new MeshLabTaskSnapshot("no-lease", "mesh-lab-worker-exec-a", "Assigned", "http://peer:8080", null),
            new MeshLabTaskSnapshot("no-url", "mesh-lab-worker-exec-b", "Assigned", null, "tok"),
            new MeshLabTaskSnapshot("wrong-status", "mesh-lab-worker-exec-c", "Running", "http://peer:8080", "tok"),
        };

        MeshLabWorkerExecutorClient.PickCandidate(tasks, "mesh-lab-worker-exec").Should().BeNull();
    }

    [Fact]
    public void PickCandidate_skips_tasks_with_non_matching_name_prefix()
    {
        var tasks = new[]
        {
            new MeshLabTaskSnapshot("a", "other-prefix-task", "Assigned", "http://peer:8080", "tok"),
        };

        MeshLabWorkerExecutorClient.PickCandidate(tasks, "mesh-lab-worker-exec").Should().BeNull();
    }

    [Theory]
    [InlineData(null, MeshLabTaskStatus.Assigned, false)]
    [InlineData("", MeshLabTaskStatus.Assigned, false)]
    [InlineData("assigned", MeshLabTaskStatus.Assigned, true)]
    [InlineData("1", MeshLabTaskStatus.Assigned, true)]
    public void IsStatus_treats_blank_or_case_insensitive_values(string? status, MeshLabTaskStatus expected, bool match)
    {
        MeshLabWorkerExecutorClient.IsStatus(status, expected).Should().Be(match);
    }

    [Fact]
    public void PickCandidate_accepts_numeric_assigned_status()
    {
        var tasks = new[]
        {
            new MeshLabTaskSnapshot("numeric", "mesh-lab-worker-exec-numeric", "1", "http://peer:8080", "tok"),
        };

        MeshLabWorkerExecutorClient.PickCandidate(tasks, "mesh-lab-worker-exec")!.TaskId.Should().Be("numeric");
    }

    [Fact]
    public void PickCandidate_returns_first_matching_assigned_task()
    {
        var tasks = new[]
        {
            new MeshLabTaskSnapshot("first", "mesh-lab-worker-exec-a", "Assigned", "http://peer:8080", "tok1"),
            new MeshLabTaskSnapshot("second", "mesh-lab-worker-exec-b", "Assigned", "http://peer:8080", "tok2"),
        };

        MeshLabWorkerExecutorClient.PickCandidate(tasks, "mesh-lab-worker-exec")!.TaskId.Should().Be("first");
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_returns_zero_when_director_lists_no_tasks()
    {
        var client = CreateClient(
            new FakeFleetHandler((_, _) => Json(HttpStatusCode.OK, "[]")),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = "test-key",
                TaskNamePrefix = "mesh-lab-worker-exec",
            },
            new ConfigurationBuilder().Build());

        (await client.TryProcessOneAssignedTaskAsync(CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_returns_zero_when_status_patch_fails()
    {
        var client = CreateClient(
            new FakeFleetHandler((req, _) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    return Json(HttpStatusCode.OK, """
                        [{"taskId":"t1","name":"mesh-lab-worker-exec-job","status":"Assigned","assignedApiBaseUrl":"http://peer:8080","leaseToken":"tok"}]
                        """);
                }

                return Json(HttpStatusCode.BadRequest, """{"error":"patch failed"}""");
            }),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = "test-key",
                TaskNamePrefix = "mesh-lab-worker-exec",
                ExecuteBrickOnAssignedPeer = false,
            },
            new ConfigurationBuilder().Build());

        (await client.TryProcessOneAssignedTaskAsync(CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_uses_configuration_api_key_fallback()
    {
        var patchCount = 0;
        var client = CreateClient(
            new FakeFleetHandler((req, _) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    return Json(HttpStatusCode.OK, """
                        [{"taskId":"t2","name":"mesh-lab-worker-exec-fallback","status":"Assigned","assignedApiBaseUrl":"http://peer:8080","leaseToken":"tok"}]
                        """);
                }

                req.Headers.TryGetValues("X-Nexo-Api-Key", out var keys).Should().BeTrue();
                keys!.Single().Should().Be("config-key");
                patchCount++;
                return Json(HttpStatusCode.OK, "{}");
            }),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = null,
                TaskNamePrefix = "mesh-lab-worker-exec",
                ExecuteBrickOnAssignedPeer = false,
            },
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Nexo:Security:ApiKey"] = "config-key" })
                .Build());

        var processed = await client.TryProcessOneAssignedTaskAsync(CancellationToken.None);

        processed.Should().Be(1);
        patchCount.Should().Be(2);
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_returns_zero_when_no_api_key_configured()
    {
        var client = CreateClient(
            new FakeFleetHandler((_, _) => Json(HttpStatusCode.OK, """
                [{"taskId":"t3","name":"mesh-lab-worker-exec-no-key","status":"Assigned","assignedApiBaseUrl":"http://peer:8080","leaseToken":"tok"}]
                """)),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = null,
                TaskNamePrefix = "mesh-lab-worker-exec",
            },
            new ConfigurationBuilder().Build());

        (await client.TryProcessOneAssignedTaskAsync(CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_completes_task_without_peer_brick_execution()
    {
        var patchCount = 0;
        var client = CreateClient(
            new FakeFleetHandler((req, _) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    return Json(HttpStatusCode.OK, """
                        [{"taskId":"t4","name":"mesh-lab-worker-exec-complete","status":"Assigned","assignedApiBaseUrl":"http://peer:8080","leaseToken":"tok"}]
                        """);
                }

                patchCount++;
                return Json(HttpStatusCode.OK, "{}");
            }),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = "test-key",
                TaskNamePrefix = "mesh-lab-worker-exec",
                ExecuteBrickOnAssignedPeer = false,
            },
            new ConfigurationBuilder().Build());

        (await client.TryProcessOneAssignedTaskAsync(CancellationToken.None)).Should().Be(1);
        patchCount.Should().Be(2);
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_still_succeeds_when_peer_catalog_is_empty()
    {
        var patchCount = 0;
        var client = CreateClient(
            new FakeFleetHandler((req, _) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                if (path.EndsWith("/api/mesh/tasks", StringComparison.Ordinal))
                {
                    return Json(HttpStatusCode.OK, """
                        [{"taskId":"t5","name":"mesh-lab-worker-exec-empty-catalog","status":"Assigned","assignedApiBaseUrl":"http://peer:8080/","leaseToken":"tok"}]
                        """);
                }

                if (path.EndsWith("/api/bricks", StringComparison.Ordinal))
                    return Json(HttpStatusCode.OK, """{"bricks":[]}""");

                patchCount++;
                return Json(HttpStatusCode.OK, "{}");
            }),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = "test-key",
                TaskNamePrefix = "mesh-lab-worker-exec",
                ExecuteBrickOnAssignedPeer = true,
            },
            new ConfigurationBuilder().Build());

        (await client.TryProcessOneAssignedTaskAsync(CancellationToken.None)).Should().Be(1);
        patchCount.Should().Be(2);
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_reads_brick_id_from_lowercase_id_property()
    {
        var executed = false;
        var client = CreateClient(
            new FakeFleetHandler((req, _) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                if (path.EndsWith("/api/mesh/tasks", StringComparison.Ordinal))
                {
                    return Json(HttpStatusCode.OK, """
                        [{"taskId":"t6b","name":"mesh-lab-worker-exec-lowercase-id","status":"Assigned","assignedApiBaseUrl":"http://peer:8080/","leaseToken":"tok"}]
                        """);
                }

                if (path.EndsWith("/api/bricks", StringComparison.Ordinal))
                    return Json(HttpStatusCode.OK, """{"bricks":[{"id":"lowercase-brick"}]}""");

                if (path.Contains("/execute", StringComparison.Ordinal))
                {
                    executed = true;
                    return Json(HttpStatusCode.OK, """{"success":true}""");
                }

                return Json(HttpStatusCode.OK, "{}");
            }),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = "test-key",
                TaskNamePrefix = "mesh-lab-worker-exec",
                ExecuteBrickOnAssignedPeer = true,
            },
            new ConfigurationBuilder().Build());

        (await client.TryProcessOneAssignedTaskAsync(CancellationToken.None)).Should().Be(1);
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_skips_brick_when_catalog_request_fails()
    {
        var patchCount = 0;
        var client = CreateClient(
            new FakeFleetHandler((req, _) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                if (path.EndsWith("/api/mesh/tasks", StringComparison.Ordinal) && req.Method == HttpMethod.Get)
                {
                    return Json(HttpStatusCode.OK, """
                        [{"taskId":"t7","name":"mesh-lab-worker-exec-catalog-fail","status":"Assigned","assignedApiBaseUrl":"http://peer:8080/","leaseToken":"tok"}]
                        """);
                }

                if (path.EndsWith("/api/bricks", StringComparison.Ordinal))
                    return Json(HttpStatusCode.ServiceUnavailable, """{"error":"catalog down"}""");

                patchCount++;
                return Json(HttpStatusCode.OK, "{}");
            }),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = "test-key",
                TaskNamePrefix = "mesh-lab-worker-exec",
                ExecuteBrickOnAssignedPeer = true,
            },
            new ConfigurationBuilder().Build());

        (await client.TryProcessOneAssignedTaskAsync(CancellationToken.None)).Should().Be(1);
        patchCount.Should().Be(2);
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_tolerates_peer_http_errors_during_brick_execute()
    {
        var client = CreateClient(
            new FakeFleetHandler((req, _) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                if (path.EndsWith("/api/mesh/tasks", StringComparison.Ordinal))
                    return Json(HttpStatusCode.OK, req.Method == HttpMethod.Get
                        ? """
                          [{"taskId":"t8","name":"mesh-lab-worker-exec-peer-error","status":"Assigned","assignedApiBaseUrl":"http://peer:8080/","leaseToken":"tok"}]
                          """
                        : "{}");

                if (path.EndsWith("/api/bricks", StringComparison.Ordinal))
                    throw new HttpRequestException("peer unreachable");

                return Json(HttpStatusCode.OK, "{}");
            }),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = "test-key",
                TaskNamePrefix = "mesh-lab-worker-exec",
                ExecuteBrickOnAssignedPeer = true,
            },
            new ConfigurationBuilder().Build());

        (await client.TryProcessOneAssignedTaskAsync(CancellationToken.None)).Should().Be(1);
    }

    [Fact]
    public async Task TryProcessOneAssignedTaskAsync_reads_brick_id_from_pascal_case_property()
    {
        var executed = false;
        var client = CreateClient(
            new FakeFleetHandler((req, _) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                if (path.EndsWith("/api/mesh/tasks", StringComparison.Ordinal))
                {
                    return Json(HttpStatusCode.OK, """
                        [{"taskId":"t6","name":"mesh-lab-worker-exec-brick-id","status":"Assigned","assignedApiBaseUrl":"http://peer:8080/","leaseToken":"tok"}]
                        """);
                }

                if (path.EndsWith("/api/bricks", StringComparison.Ordinal))
                    return Json(HttpStatusCode.OK, """{"Bricks":[{"BrickId":"pascal-brick"}]}""");

                if (path.Contains("/execute", StringComparison.Ordinal))
                {
                    executed = true;
                    return Json(HttpStatusCode.BadRequest, """{"error":"ignored"}""");
                }

                return Json(HttpStatusCode.OK, "{}");
            }),
            new MeshLabWorkerExecutorOptions
            {
                ApiKey = "test-key",
                TaskNamePrefix = "mesh-lab-worker-exec",
                ExecuteBrickOnAssignedPeer = true,
            },
            new ConfigurationBuilder().Build());

        (await client.TryProcessOneAssignedTaskAsync(CancellationToken.None)).Should().Be(1);
        executed.Should().BeTrue();
    }

    private static MeshLabWorkerExecutorClient CreateClient(
        HttpMessageHandler handler,
        MeshLabWorkerExecutorOptions options,
        IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(MeshLabWorkerExecutorClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        return new MeshLabWorkerExecutorClient(
            factory,
            new StaticOptionsMonitor<MeshLabWorkerExecutorOptions>(options),
            configuration,
            NullLogger<MeshLabWorkerExecutorClient>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class FakeFleetHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }

    private sealed class StaticOptionsMonitor<T>(T value) : Microsoft.Extensions.Options.IOptionsMonitor<T> where T : class
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}

public sealed class MeshLabWorkerExecutorOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_mesh_lab_worker_configuration()
    {
        var options = new MeshLabWorkerExecutorOptions();

        MeshLabWorkerExecutorOptions.SectionPath.Should().Be("Nexo:MeshLab:WorkerExecutor");
        options.DirectorBaseUrl.Should().Be("http://127.0.0.1:18081");
        options.TaskNamePrefix.Should().Be("mesh-lab-worker-exec");
        options.PollIntervalMs.Should().Be(500);
        options.ExecuteBrickOnAssignedPeer.Should().BeTrue();
        options.ResultSummary.Should().Be("mesh-lab-worker-executor-complete");
    }
}
