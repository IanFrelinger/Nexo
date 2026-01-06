using Microsoft.AspNetCore.Mvc;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;
using Nexo.Demo.Api.Controllers;
using Nexo.Infrastructure.Export;

namespace Nexo.Demo.Api.Tests.Controllers;

/// <summary>
/// Smoke tests for ExportController.
/// </summary>
public class ExportControllerSmokeTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestExport();
            await TestDownload();
            
            return new TestResult
            {
                TestName = nameof(ExportControllerSmokeTests),
                Category = "API",
                Passed = true,
                Message = "All ExportController smoke tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ExportControllerSmokeTests),
                Category = "API",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(ExportControllerSmokeTests),
                Category = "API",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestExport()
    {
        var mockExporter = new Mock<IWorkflowExporter>();
        
        var exportResult = new ExportResult
        {
            Success = true,
            Mode = ExportMode.PureDeterministic,
            Target = ExportTarget.CSharp,
            Files = [
                new ExportedFile
                {
                    Path = "test.cs",
                    Content = "// Test",
                    Language = "csharp"
                }
            ],
            RuntimeRequirements = [],
            Messages = []
        };
        
        mockExporter.Setup(e => e.ExportAsync(
            It.IsAny<Workflow>(),
            It.IsAny<ExportConfig>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);
        
        var controller = new ExportController(mockExporter.Object);
        
        var request = new ExportRequest(
            new ExportWorkflowRequest(
                null,
                "Test Workflow",
                "Test",
                [],
                []
            ),
            "PureDeterministic",
            "CSharp",
            null,
            true,
            "Files",
            "Generated"
        );
        
        var result = await controller.Export(request, CancellationToken.None);
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        await Task.CompletedTask;
    }
    
    private async Task TestDownload()
    {
        var mockExporter = new Mock<IWorkflowExporter>();
        
        var exportResult = new ExportResult
        {
            Success = true,
            Mode = ExportMode.PureDeterministic,
            Target = ExportTarget.CSharp,
            Files = [
                new ExportedFile
                {
                    Path = "test.cs",
                    Content = "// Test",
                    Language = "csharp"
                }
            ],
            RuntimeRequirements = [],
            Messages = []
        };
        
        mockExporter.Setup(e => e.ExportAsync(
            It.IsAny<Workflow>(),
            It.IsAny<ExportConfig>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);
        
        var controller = new ExportController(mockExporter.Object);
        
        var request = new ExportRequest(
            new ExportWorkflowRequest(
                null,
                "Test",
                "Test",
                [],
                []
            ),
            "PureDeterministic",
            "CSharp",
            null,
            true,
            "Zip",
            null
        );
        
        var result = await controller.Download(request, CancellationToken.None);
        var fileResult = result as FileResult;
        
        AssertNotNull(fileResult);
        AssertEqual("application/zip", fileResult.ContentType);
        AssertEqual("nexo-export.zip", fileResult.FileDownloadName);
        
        await Task.CompletedTask;
    }
}

