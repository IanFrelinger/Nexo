using FluentAssertions;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Infrastructure.SelfContext;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.SelfContext;

public sealed class LiteDbExecutionTracerGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_blank_database_path()
    {
        var act = () => new LiteDbExecutionTracer("   ");

        act.Should().Throw<ArgumentNullException>().WithParameterName("pathOrConnectionString");
    }

    [Fact]
    public async Task TraceAsync_persists_entry_with_context_and_query_filters_by_window()
    {
        var path = CreateTempDbPath();
        try
        {
            var tracer = new LiteDbExecutionTracer(path);
            var before = DateTimeOffset.UtcNow.AddMinutes(-1);

            await tracer.TraceAsync(
                "build",
                context: new Dictionary<string, object> { ["project"] = "nexo" },
                path: "src/Foo.cs",
                outcome: "success");

            var results = await tracer.QueryAsync(since: before, until: DateTimeOffset.UtcNow.AddMinutes(1));

            results.Should().ContainSingle();
            results[0].Operation.Should().Be("build");
            results[0].Path.Should().Be("src/Foo.cs");
            results[0].Outcome.Should().Be("success");
            results[0].Context.Should().ContainKey("project");
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task QueryAsync_honors_cancellation()
    {
        var path = CreateTempDbPath();
        try
        {
            var tracer = new LiteDbExecutionTracer(path);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await tracer.QueryAsync(cancellationToken: cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void Accepts_filename_connection_string()
    {
        var path = CreateTempDbPath();
        try
        {
            var tracer = new LiteDbExecutionTracer($"Filename={path}");

            tracer.Should().NotBeNull();
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string CreateTempDbPath()
        => Path.Combine(Path.GetTempPath(), "nexo-trace-" + Guid.NewGuid().ToString("N") + ".db");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}

public sealed class LiteDbTestFailureStoreGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_blank_database_path()
    {
        var act = () => new LiteDbTestFailureStore("   ");

        act.Should().Throw<ArgumentNullException>().WithParameterName("pathOrConnectionString");
    }

    [Fact]
    public async Task RecordAsync_and_QueryAsync_round_trip_with_time_filter()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = new LiteDbTestFailureStore(path);
            var now = DateTimeOffset.UtcNow;
            await store.RecordAsync(new TestFailureRecord
            {
                Id = "fail-1",
                Timestamp = now,
                TestName = "MyTest",
                FilePath = "src/Test.cs",
                ErrorMessage = "assert failed",
                StackTrace = "at Test()",
            });

            var results = await store.QueryAsync(since: now.AddMinutes(-1), until: now.AddMinutes(1));

            results.Should().ContainSingle();
            results[0].TestName.Should().Be("MyTest");
            results[0].ErrorMessage.Should().Be("assert failed");
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task QueryAsync_honors_cancellation()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = new LiteDbTestFailureStore(path);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await store.QueryAsync(cancellationToken: cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string CreateTempDbPath()
        => Path.Combine(Path.GetTempPath(), "nexo-failures-" + Guid.NewGuid().ToString("N") + ".db");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
