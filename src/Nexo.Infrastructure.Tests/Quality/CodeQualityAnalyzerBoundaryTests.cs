using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.CodeQuality;
using Nexo.Infrastructure.Quality;
using Xunit;

namespace Nexo.Infrastructure.Tests.Quality
{
    /// <summary>
    /// Boundary tests for CodeQualityAnalyzer focusing on threshold behavior and edge cases
    /// </summary>
    public class CodeQualityAnalyzerBoundaryTests
    {
        private readonly CodeQualityAnalyzer _analyzer;
        private readonly ILogger<CodeQualityAnalyzer> _logger;

        public CodeQualityAnalyzerBoundaryTests()
        {
            _logger = new LoggerFactory().CreateLogger<CodeQualityAnalyzer>();
            _analyzer = new CodeQualityAnalyzer(_logger);
        }

        #region Quality Gate Threshold Tests

        [Theory]
        [InlineData(5.9, false, false)] // Below gate
        [InlineData(6.0, true, false)]  // Exactly at gate
        [InlineData(6.1, true, false)] // Just above gate
        [InlineData(7.9, true, false)] // High but below production
        [InlineData(8.0, true, true)]  // Production ready
        [InlineData(9.5, true, true)]  // Excellent
        public void QualityGates_ShouldRespectThresholds(double score, bool passesGate, bool isProductionReady)
        {
            // Arrange
            var qualityScore = new QualityScore
            {
                OverallScore = score,
                QualityLevel = DetermineQualityLevel(score),
                MaintainabilityIndex = 80,
                CyclomaticComplexity = 5,
                LinesOfCode = 100
            };

            // Act
            var gateResult = _analyzer.PassesQualityGate(qualityScore);
            var productionResult = qualityScore.IsProductionReady;

            // Assert
            Assert.Equal(passesGate, gateResult);
            Assert.Equal(isProductionReady, productionResult);
        }

        [Theory]
        [InlineData(3.9, true)]  // Below threshold, requires review
        [InlineData(4.0, true)]  // At threshold, requires review
        [InlineData(4.1, false)] // Above threshold, no review needed
        [InlineData(6.0, false)] // Good quality, no review needed
        public void HumanReviewThreshold_ShouldWorkCorrectly(double score, bool requiresReview)
        {
            // Arrange
            var qualityScore = new QualityScore
            {
                OverallScore = score,
                QualityLevel = DetermineQualityLevel(score),
                MaintainabilityIndex = 70,
                CyclomaticComplexity = 8,
                LinesOfCode = 150
            };

            // Act
            var result = _analyzer.RequiresHumanReview(qualityScore);

            // Assert
            Assert.Equal(requiresReview, result);
        }

        #endregion

        #region Language Feature Edge Cases

        [Theory]
        [InlineData("unsafe { int* ptr = &value; }", true, IssueType.CodeQuality, IssueSeverity.Medium)]
        [InlineData("fixed (byte* ptr = array) { }", true, IssueType.CodeQuality, IssueSeverity.Medium)]
        [InlineData("var span = stackalloc int[10];", false, IssueType.None, IssueSeverity.None)]
        [InlineData("using var resource = new FileStream();", false, IssueType.None, IssueSeverity.None)]
        public async Task UnsafeCode_ShouldBeDetectedAndFlagged(
            string code, 
            bool shouldFlag, 
            IssueType expectedType, 
            IssueSeverity expectedSeverity)
        {
            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code, "TestTool");

            // Assert
            if (shouldFlag)
            {
                Assert.Contains(result.Issues, i => i.Type == expectedType && i.Severity == expectedSeverity);
            }
            else
            {
                Assert.DoesNotContain(result.Issues, i => i.Type == expectedType);
            }
        }

        [Theory]
        [InlineData("async Task<string> GetDataAsync() { return await httpClient.GetStringAsync(url); }", false)]
        [InlineData("public async Task ProcessAsync() { await Task.Delay(1000); }", false)]
        [InlineData("async void BadMethod() { }", true)]
        [InlineData("async Task Main() { }", true)]
        public async Task AsyncPatterns_ShouldBeEvaluatedCorrectly(string code, bool shouldFlag)
        {
            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code, "TestTool");

            // Assert
            if (shouldFlag)
            {
                Assert.Contains(result.Issues, i => i.Type == IssueType.CodeQuality);
            }
            else
            {
                Assert.DoesNotContain(result.Issues, i => i.Type == IssueType.CodeQuality && i.Title.Contains("async"));
            }
        }

        [Theory]
        [InlineData("public class MyClass { }", false)]
        [InlineData("public partial class MyClass { }", false)]
        [InlineData("public static class MyClass { }", false)]
        [InlineData("public sealed class MyClass { }", false)]
        [InlineData("public abstract class MyClass { }", false)]
        [InlineData("internal class MyClass { }", true)]
        public async Task ClassVisibility_ShouldBeEvaluated(string code, bool shouldFlag)
        {
            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code, "TestTool");

            // Assert
            if (shouldFlag)
            {
                Assert.Contains(result.Issues, i => i.Type == IssueType.CodeQuality && i.Title.Contains("visibility"));
            }
            else
            {
                Assert.DoesNotContain(result.Issues, i => i.Type == IssueType.CodeQuality && i.Title.Contains("visibility"));
            }
        }

        #endregion

        #region Complexity Boundary Tests

        [Theory]
        [InlineData(1, false)]  // Simple
        [InlineData(5, false)]  // Low complexity
        [InlineData(10, true)]  // Medium complexity
        [InlineData(15, true)]  // High complexity
        [InlineData(25, true)]  // Very high complexity
        public async Task CyclomaticComplexity_ShouldTriggerWarningsAtThresholds(int complexity, bool shouldFlag)
        {
            // Arrange
            var code = GenerateComplexCode(complexity);

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code, "TestTool");

            // Assert
            if (shouldFlag)
            {
                Assert.Contains(result.Issues, i => i.Type == IssueType.CodeQuality && i.Title.Contains("complexity"));
            }
            else
            {
                Assert.DoesNotContain(result.Issues, i => i.Type == IssueType.CodeQuality && i.Title.Contains("complexity"));
            }
        }

        [Theory]
        [InlineData(50, false)]   // Small file
        [InlineData(200, false)]  // Medium file
        [InlineData(500, true)]   // Large file
        [InlineData(1000, true)]  // Very large file
        public async Task FileSize_ShouldTriggerWarningsAtThresholds(int linesOfCode, bool shouldFlag)
        {
            // Arrange
            var code = string.Join("\n", Enumerable.Range(1, linesOfCode).Select(i => 
                $"var variable{i} = \"Line {i}\";"));

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code, "TestTool");

            // Assert
            if (shouldFlag)
            {
                Assert.Contains(result.Issues, i => i.Type == IssueType.CodeQuality && i.Title.Contains("size"));
            }
            else
            {
                Assert.DoesNotContain(result.Issues, i => i.Type == IssueType.CodeQuality && i.Title.Contains("size"));
            }
        }

        #endregion

        #region Security Boundary Tests

        [Theory]
        [InlineData("var password = \"hardcoded123\";", true, IssueType.Security, IssueSeverity.Critical)]
        [InlineData("var apiKey = \"sk-1234567890abcdef\";", true, IssueType.Security, IssueSeverity.Critical)]
        [InlineData("var connectionString = \"Server=localhost;Database=test;\";", true, IssueType.Security, IssueSeverity.High)]
        [InlineData("var config = \"appsettings.json\";", false, IssueType.None, IssueSeverity.None)]
        [InlineData("var message = \"Password is required\";", false, IssueType.None, IssueSeverity.None)]
        public async Task SecurityPatterns_ShouldBeDetectedAtBoundaries(
            string code, 
            bool shouldFlag, 
            IssueType expectedType, 
            IssueSeverity expectedSeverity)
        {
            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code, "TestTool");

            // Assert
            if (shouldFlag)
            {
                Assert.Contains(result.Issues, i => i.Type == expectedType && i.Severity == expectedSeverity);
            }
            else
            {
                Assert.DoesNotContain(result.Issues, i => i.Type == expectedType);
            }
        }

        #endregion

        #region Performance Boundary Tests

        [Theory]
        [InlineData("for (int i = 0; i < 1000000; i++) { }", true, IssueType.Performance, IssueSeverity.Medium)]
        [InlineData("foreach (var item in largeCollection) { }", true, IssueType.Performance, IssueSeverity.Low)]
        [InlineData("var result = items.Where(x => x.IsValid).ToList();", false, IssueType.None, IssueSeverity.None)]
        [InlineData("var count = items.Count();", true, IssueType.Performance, IssueSeverity.Low)]
        public async Task PerformancePatterns_ShouldBeDetectedAtBoundaries(
            string code, 
            bool shouldFlag, 
            IssueType expectedType, 
            IssueSeverity expectedSeverity)
        {
            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code, "TestTool");

            // Assert
            if (shouldFlag)
            {
                Assert.Contains(result.Issues, i => i.Type == expectedType && i.Severity == expectedSeverity);
            }
            else
            {
                Assert.DoesNotContain(result.Issues, i => i.Type == expectedType);
            }
        }

        #endregion

        #region Edge Case Combinations

        [Fact]
        public async Task MultipleIssues_ShouldAccumulateCorrectly()
        {
            // Arrange
            var problematicCode = @"
                public class BadClass
                {
                    private string password = ""hardcoded123"";
                    
                    public async void BadMethod()
                    {
                        for (int i = 0; i < 1000000; i++)
                        {
                            if (i % 2 == 0)
                            {
                                if (i % 3 == 0)
                                {
                                    if (i % 5 == 0)
                                    {
                                        Console.WriteLine(i);
                                    }
                                }
                            }
                        }
                    }
                }
            ";

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(problematicCode, "TestTool");

            // Assert
            Assert.True(result.Issues.Count >= 3);
            Assert.Contains(result.Issues, i => i.Type == IssueType.Security);
            Assert.Contains(result.Issues, i => i.Type == IssueType.CodeQuality);
            Assert.Contains(result.Issues, i => i.Type == IssueType.Performance);
        }

        [Fact]
        public async Task EmptyCode_ShouldHandleGracefully()
        {
            // Act
            var result = await _analyzer.AnalyzeCodeAsync("", "TestTool");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.OverallScore >= 0);
            Assert.True(result.OverallScore <= 10);
        }

        [Fact]
        public async Task NullCode_ShouldHandleGracefully()
        {
            // Act
            var result = await _analyzer.AnalyzeCodeAsync(null, "TestTool");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.OverallScore >= 0);
            Assert.True(result.OverallScore <= 10);
        }

        #endregion

        #region Helper Methods

        private QualityLevel DetermineQualityLevel(double score)
        {
            return score switch
            {
                < 2.0 => QualityLevel.Unacceptable,
                < 4.0 => QualityLevel.Poor,
                < 6.0 => QualityLevel.Acceptable,
                < 8.0 => QualityLevel.Good,
                _ => QualityLevel.Excellent
            };
        }

        private string GenerateComplexCode(int complexity)
        {
            var code = "public int ComplexMethod(int input) {\n";
            
            for (int i = 0; i < complexity - 1; i++)
            {
                code += $"    if (input > {i}) {{\n";
            }
            
            code += "        return input * 2;\n";
            
            for (int i = 0; i < complexity - 1; i++)
            {
                code += "    }\n";
            }
            
            code += "}\n";
            return code;
        }

        #endregion
    }
}
