using FluentAssertions;
using Ashlar.Core.Application.SelfContext.Models;
using Ashlar.Infrastructure.SelfContext;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SelfContext;

/// <summary>Tests for lite db execution tracer gap coverage.</summary>
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
                context: new Dictionary<string, object> { ["project"] = "ashlar" },
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
            /// <summary>Attempts to delete; returns false on failure.</summary>
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
            /// <summary>Attempts to delete; returns false on failure.</summary>
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
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    private static string CreateTempDbPath()
        => Path.Combine(Path.GetTempPath(), "ashlar-trace-" + Guid.NewGuid().ToString("N") + ".db");

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
