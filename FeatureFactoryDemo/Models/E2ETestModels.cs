using System;
using System.Collections.Generic;
using Nexo.Feature.Analysis.Models;

namespace FeatureFactoryDemo.Models
{
    public class E2ETestSuite
    {
        public string Platform { get; set; } = string.Empty;
        public string FeatureDescription { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public int QualityScore { get; set; }
        public List<E2ETest> UnitTests { get; set; } = new();
        public List<E2ETest> IntegrationTests { get; set; } = new();
        public List<E2ETest> APITests { get; set; } = new();
        public List<E2ETest> UITests { get; set; } = new();
        public List<E2ETest> PerformanceTests { get; set; } = new();
        public List<E2ETest> SecurityTests { get; set; } = new();
        public List<E2ETest> LoadTests { get; set; } = new();
    }

    public class E2ETest
    {
        public string TestName { get; set; } = string.Empty;
        public string TestType { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string TestResult { get; set; } = "Not Executed";
        public DateTime ExecutedAt { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

    public class E2ETestResult
    {
        public string Platform { get; set; } = string.Empty;
        public E2ETestSuite TestSuite { get; set; } = new();
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
    }

    public class E2ETestHistory
    {
        public int Id { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string FeatureDescription { get; set; } = string.Empty;
        public string GeneratedCode { get; set; } = string.Empty;
        public int QualityScore { get; set; }
        public string TestSuite { get; set; } = string.Empty;
        public string TestResult { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime ExecutedAt { get; set; }
        public bool IsSuccessful { get; set; }
        public string Tags { get; set; } = string.Empty;
    }

    public class FeatureGenerationResult
    {
        public string Description { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public int TargetScore { get; set; }
        public string GeneratedCode { get; set; } = string.Empty;
        public int FinalQualityScore { get; set; }
        public int TotalIterations { get; set; }
        public bool Success { get; set; }
        public List<CodingStandardValidationResult> IterationHistory { get; set; } = new();
    }
}
