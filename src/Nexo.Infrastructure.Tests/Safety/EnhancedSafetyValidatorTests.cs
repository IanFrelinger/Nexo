using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Safety;
using Nexo.Infrastructure.Safety;
using Xunit;

namespace Nexo.Infrastructure.Tests.Safety
{
    /// <summary>
    /// Comprehensive tests for EnhancedSafetyValidator including rule hits and false-positive regression tests
    /// </summary>
    public class EnhancedSafetyValidatorTests
    {
        private readonly EnhancedSafetyValidator _validator;
        private readonly ILogger<EnhancedSafetyValidator> _logger;

        public EnhancedSafetyValidatorTests()
        {
            _logger = new LoggerFactory().CreateLogger<EnhancedSafetyValidator>();
            _validator = new EnhancedSafetyValidator(_logger);
        }

        #region File System Safety Tests

        [Theory]
        [InlineData("File.Delete(\"C:\\Windows\\System32\\*\");", true, SafetyIssueType.MaliciousCode, SafetySeverity.Critical)]
        [InlineData("File.Delete(userInput);", true, SafetyIssueType.SecurityVulnerability, SafetySeverity.High)]
        [InlineData("Directory.Delete(path, true);", true, SafetyIssueType.SecurityVulnerability, SafetySeverity.High)]
        [InlineData("File.Move(source, dest);", false, SafetyIssueType.None, SafetySeverity.None)]
        [InlineData("File.ReadAllText(\"config.json\");", false, SafetyIssueType.None, SafetySeverity.None)]
        public async Task FileSystemOperations_ShouldDetectDangerousPatterns(
            string code, 
            bool shouldBlock, 
            SafetyIssueType expectedType, 
            SafetySeverity expectedSeverity)
        {
            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(code);

            // Assert
            if (shouldBlock)
            {
                Assert.True(result.IsBlocked || result.RequiresHumanReview);
                Assert.Contains(result.Issues, i => i.Type == expectedType && i.Severity == expectedSeverity);
            }
            else
            {
                Assert.False(result.IsBlocked);
                Assert.DoesNotContain(result.Issues, i => i.Type == expectedType);
            }
        }

        [Fact]
        public async Task FileSystemOperations_ShouldAllowSafeOperations()
        {
            // Arrange
            var safeCode = @"
                var config = File.ReadAllText(""appsettings.json"");
                var tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, ""safe content"");
                File.Delete(tempFile);
            ";

            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(safeCode);

            // Assert
            Assert.True(result.IsValid);
            Assert.False(result.IsBlocked);
            Assert.False(result.RequiresHumanReview);
            Assert.Empty(result.Issues);
        }

        #endregion

        #region Network Safety Tests

        [Theory]
        [InlineData("HttpClient().GetAsync(\"http://example.com\");", true, SafetyIssueType.SecurityVulnerability, SafetySeverity.Medium)]
        [InlineData("WebRequest.Create(\"http://malicious.com\");", true, SafetyIssueType.SecurityVulnerability, SafetySeverity.Medium)]
        [InlineData("HttpClient().GetAsync(\"https://api.trusted.com\");", false, SafetyIssueType.None, SafetySeverity.None)]
        [InlineData("new HttpClient().GetStringAsync(\"https://secure.example.com\");", false, SafetyIssueType.None, SafetySeverity.None)]
        public async Task NetworkOperations_ShouldDetectInsecureConnections(
            string code, 
            bool shouldFlag, 
            SafetyIssueType expectedType, 
            SafetySeverity expectedSeverity)
        {
            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(code);

            // Assert
            if (shouldFlag)
            {
                Assert.True(result.RequiresHumanReview);
                Assert.Contains(result.Issues, i => i.Type == expectedType && i.Severity == expectedSeverity);
            }
            else
            {
                Assert.DoesNotContain(result.Issues, i => i.Type == expectedType);
            }
        }

        [Fact]
        public async Task NetworkOperations_ShouldAllowSecureConnections()
        {
            // Arrange
            var secureCode = @"
                using var client = new HttpClient();
                var response = await client.GetStringAsync(""https://api.github.com/repos/user/repo"");
                var json = JsonSerializer.Deserialize<object>(response);
            ";

            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(secureCode);

            // Assert
            Assert.True(result.IsValid);
            Assert.False(result.IsBlocked);
        }

        #endregion

        #region System Call Safety Tests

        [Theory]
        [InlineData("Process.Start(\"cmd.exe\", \"/c del C:\\\\*\");", true, SafetyIssueType.MaliciousCode, SafetySeverity.Critical)]
        [InlineData("Process.Start(\"powershell.exe\", userInput);", true, SafetyIssueType.SecurityVulnerability, SafetySeverity.High)]
        [InlineData("Process.Start(\"notepad.exe\", \"readme.txt\");", false, SafetyIssueType.None, SafetySeverity.None)]
        [InlineData("Process.Start(new ProcessStartInfo { FileName = \"git\", Arguments = \"status\" });", false, SafetyIssueType.None, SafetySeverity.None)]
        public async Task SystemCalls_ShouldDetectDangerousProcesses(
            string code, 
            bool shouldBlock, 
            SafetyIssueType expectedType, 
            SafetySeverity expectedSeverity)
        {
            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(code);

            // Assert
            if (shouldBlock)
            {
                Assert.True(result.IsBlocked || result.RequiresHumanReview);
                Assert.Contains(result.Issues, i => i.Type == expectedType && i.Severity == expectedSeverity);
            }
            else
            {
                Assert.DoesNotContain(result.Issues, i => i.Type == expectedType);
            }
        }

        #endregion

        #region False Positive Regression Tests

        [Theory]
        [InlineData("var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), \"MyApp\");")]
        [InlineData("Directory.CreateDirectory(Path.GetTempPath());")]
        [InlineData("File.WriteAllText(Path.GetTempFileName(), \"log data\");")]
        [InlineData("var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(\"appsettings.json\"));")]
        [InlineData("using var stream = File.OpenRead(\"template.html\");")]
        public async Task FalsePositives_ShouldNotFlagLegitimateFileOperations(string code)
        {
            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(code);

            // Assert
            Assert.True(result.IsValid);
            Assert.False(result.IsBlocked);
            Assert.False(result.RequiresHumanReview);
            Assert.Empty(result.Issues);
        }

        [Theory]
        [InlineData("var client = new HttpClient(); var response = await client.GetAsync(\"https://api.example.com/data\");")]
        [InlineData("using var httpClient = new HttpClient(); var json = await httpClient.GetStringAsync(\"https://jsonplaceholder.typicode.com/posts\");")]
        [InlineData("var request = new HttpRequestMessage(HttpMethod.Get, \"https://api.github.com/user\");")]
        public async Task FalsePositives_ShouldNotFlagSecureNetworkOperations(string code)
        {
            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(code);

            // Assert
            Assert.True(result.IsValid);
            Assert.False(result.IsBlocked);
        }

        [Theory]
        [InlineData("Process.Start(\"git\", \"clone https://github.com/user/repo.git\");")]
        [InlineData("Process.Start(\"dotnet\", \"build MyProject.csproj\");")]
        [InlineData("Process.Start(\"npm\", \"install\");")]
        [InlineData("Process.Start(new ProcessStartInfo { FileName = \"code\", Arguments = \".\" });")]
        public async Task FalsePositives_ShouldNotFlagLegitimateProcessCalls(string code)
        {
            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(code);

            // Assert
            Assert.True(result.IsValid);
            Assert.False(result.IsBlocked);
        }

        #endregion

        #region Edge Cases and Complex Scenarios

        [Fact]
        public async Task ComplexCode_ShouldDetectMultipleIssues()
        {
            // Arrange
            var dangerousCode = @"
                var userInput = Request.Query[""path""];
                File.Delete(userInput);
                Process.Start(""cmd.exe"", ""/c format C:"");
                var client = new HttpClient();
                var response = await client.GetAsync(""http://malicious.com/steal-data"");
            ";

            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(dangerousCode);

            // Assert
            Assert.True(result.IsBlocked);
            Assert.True(result.Issues.Count >= 3);
            Assert.Contains(result.Issues, i => i.Type == SafetyIssueType.SecurityVulnerability);
            Assert.Contains(result.Issues, i => i.Type == SafetyIssueType.MaliciousCode);
        }

        [Fact]
        public async Task CommentedDangerousCode_ShouldNotTriggerSafetyIssues()
        {
            // Arrange
            var codeWithComments = @"
                // File.Delete(""C:\\Windows\\System32\\*"");
                // Process.Start(""cmd.exe"", ""/c del C:\\*"");
                var safeOperation = File.ReadAllText(""config.json"");
            ";

            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(codeWithComments);

            // Assert
            Assert.True(result.IsValid);
            Assert.False(result.IsBlocked);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public async Task StringLiterals_ShouldNotTriggerFalsePositives()
        {
            // Arrange
            var codeWithStringLiterals = @"
                var message = ""File.Delete() is dangerous"";
                var documentation = ""Process.Start() can be risky"";
                var logEntry = ""HttpClient used for API call"";
            ";

            // Act
            var result = await _validator.ValidateGeneratedCodeAsync(codeWithStringLiterals);

            // Assert
            Assert.True(result.IsValid);
            Assert.False(result.IsBlocked);
            Assert.Empty(result.Issues);
        }

        #endregion

        #region Performance and Stress Tests

        [Fact]
        public async Task LargeCodeFile_ShouldValidateEfficiently()
        {
            // Arrange
            var largeCode = string.Join("\n", Enumerable.Range(1, 1000).Select(i => 
                $"var variable{i} = \"This is line {i} with some File.Delete() and Process.Start() references\";"));

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _validator.ValidateGeneratedCodeAsync(largeCode);
            var duration = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(duration.TotalSeconds < 5, "Validation should complete within 5 seconds");
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ConcurrentValidation_ShouldHandleMultipleRequests()
        {
            // Arrange
            var tasks = Enumerable.Range(1, 10).Select(i => 
                _validator.ValidateGeneratedCodeAsync($"File.Delete(\"test{i}.txt\");"));

            // Act
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.True(result.RequiresHumanReview));
        }

        #endregion
    }
}
