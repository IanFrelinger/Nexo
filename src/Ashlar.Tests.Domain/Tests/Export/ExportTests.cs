using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Domain.Export;

namespace Ashlar.Tests.Domain.Tests.Export;

/// <summary>
/// Smoke tests for Export domain models.
/// </summary>
public class ExportTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestExportConfig();
            await TestExportResult();
            await TestGenerationConfig();
            await TestOutputConfig();
            
            return new TestResult
            {
                Name = nameof(ExportTests),
                Category = "Domain",
                Passed = true,
                Message = "All export smoke tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ExportTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(ExportTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestExportConfig()
    {
        var config = new ExportConfig
        {
            Mode = ExportMode.PureDeterministic,
            Target = ExportTarget.CSharp,
            GenerationBrickIds = ["brick-1", "brick-2"],
            GenerationConfig = new GenerationConfig
            {
                VariationsPerItem = 10,
                Provider = "openai",
                Model = "gpt-4",
                RequireReview = true
            },
            IncludeFallbacks = true,
            Output = new OutputConfig
            {
                Format = OutputFormat.Project,
                Namespace = "Generated",
                IncludeDocumentation = true,
                IncludeTests = false
            }
        };
        
        AssertEqual(ExportMode.PureDeterministic, config.Mode);
        AssertEqual(ExportTarget.CSharp, config.Target);
        AssertEqual(2, config.GenerationBrickIds?.Count);
        AssertNotNull(config.GenerationConfig);
        AssertEqual(10, config.GenerationConfig.VariationsPerItem);
        AssertEqual(OutputFormat.Project, config.Output.Format);
        
        await Task.CompletedTask;
    }
    
    private async Task TestExportResult()
    {
        var result = new ExportResult
        {
            Success = true,
            Mode = ExportMode.PureDeterministic,
            Target = ExportTarget.CSharp,
            Files = [
                new ExportedFile
                {
                    Path = "test.cs",
                    Content = "// Test code",
                    Language = "csharp"
                }
            ],
            RuntimeRequirements = [],
            Messages = ["Test message"]
        };
        
        AssertTrue(result.Success);
        AssertEqual(1, result.Files.Count);
        AssertEqual("test.cs", result.Files[0].Path);
        AssertEqual(0, result.RuntimeRequirements.Count);
        
        await Task.CompletedTask;
    }
    
    private async Task TestGenerationConfig()
    {
        var config = new GenerationConfig
        {
            VariationsPerItem = 20,
            Provider = "azure",
            Model = "gpt-4-turbo",
            RequireReview = false
        };
        
        AssertEqual(20, config.VariationsPerItem);
        AssertEqual("azure", config.Provider);
        AssertEqual("gpt-4-turbo", config.Model);
        AssertFalse(config.RequireReview);
        
        await Task.CompletedTask;
    }
    
    private async Task TestOutputConfig()
    {
        var config = new OutputConfig
        {
            Format = OutputFormat.Zip,
            Namespace = "MyNamespace",
            IncludeDocumentation = false,
            IncludeTests = true
        };
        
        AssertEqual(OutputFormat.Zip, config.Format);
        AssertEqual("MyNamespace", config.Namespace);
        AssertFalse(config.IncludeDocumentation);
        AssertTrue(config.IncludeTests);
        
        await Task.CompletedTask;
    }
}

