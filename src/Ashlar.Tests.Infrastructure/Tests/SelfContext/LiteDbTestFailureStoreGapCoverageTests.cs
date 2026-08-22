using FluentAssertions;
using Ashlar.Core.Application.SelfContext.Models;
using Ashlar.Infrastructure.SelfContext;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SelfContext;

/// <summary>Tests for lite db test failure store gap coverage.</summary>
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
            var store = new LiteDbTestFailureStore(path);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await store.QueryAsync(cancellationToken: cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    private static string CreateTempDbPath()
        => Path.Combine(Path.GetTempPath(), "ashlar-failures-" + Guid.NewGuid().ToString("N") + ".db");

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
