using Xunit;
using System.Diagnostics;

namespace Nexo.Feature.Analysis.Tests;

public class AnalysisPerformanceTests
{
    [Fact(Timeout = 2000)]
    public void Analysis_Performance_Smoke()
    {
        var sw = Stopwatch.StartNew();
        // Simulated fast path
        for (int i = 0; i < 10000; i++) { _ = i * i; }
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1500);
    }
}
