using Xunit;
using System.Collections.Generic;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Tests;

public partial class AnalysisModelsTests
{
    [Fact]
    public void Analysis_Models_WorkCorrectly()
    {
        var request = new AnalysisRequest
        {
            TargetPath = "/test/path",
            AnalysisType = "code-quality",
            Options = new Dictionary<string, object> { ["option1"] = "value1" }
        };
        var issue = new AnalysisIssue
        {
            Severity = "High",
            Message = "Test issue",
            Location = "Line 42",
            Category = "Syntax"
        };
        var result = new AnalysisResult
        {
            Success = true,
            Issues = new List<AnalysisIssue> { issue },
            Metrics = new Dictionary<string, double> { ["metric1"] = 95.5 },
            Summary = "All checks passed"
        };

        Assert.Equal("/test/path", request.TargetPath);
        Assert.Equal("code-quality", request.AnalysisType);
        Assert.True(result.Success);
        Assert.Single(result.Issues);
        Assert.True(result.Metrics.ContainsKey("metric1"));
    }
}
