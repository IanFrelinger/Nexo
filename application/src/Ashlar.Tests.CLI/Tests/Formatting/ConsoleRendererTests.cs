using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Validation.Models;
using Ashlar.Core.Application.Agent.Models;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Domain.Values;
using System.Text.Json;

namespace Ashlar.Tests.CLI.Tests.Formatting;

/// <summary>Tests for console renderer.</summary>
public class ConsoleRendererTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test render success.</summary>
            TestRenderSuccess();
            /// <summary>Test render error.</summary>
            TestRenderError();
            /// <summary>Test render error with code.</summary>
            TestRenderErrorWithCode();
            /// <summary>Test render progress start.</summary>
            TestRenderProgressStart();
            /// <summary>Test render progress complete.</summary>
            TestRenderProgressComplete();
            /// <summary>Test render progress.</summary>
            TestRenderProgress();
            /// <summary>Test progress goes to standard error.</summary>
            TestProgressGoesToStandardError();
            /// <summary>Test render analysis result.</summary>
            TestRenderAnalysisResult();
            /// <summary>Test render analysis result json.</summary>
            TestRenderAnalysisResultJson();
            /// <summary>Test render validation result.</summary>
            TestRenderValidationResult();
            /// <summary>Test render validation result json.</summary>
            TestRenderValidationResultJson();
            /// <summary>Test render agent result.</summary>
            TestRenderAgentResult();
            /// <summary>Test render agent result json.</summary>
            TestRenderAgentResultJson();
            /// <summary>Test render agent list.</summary>
            TestRenderAgentList();
            /// <summary>Test render agent list json.</summary>
            TestRenderAgentListJson();

            return Task.FromResult(new TestResult
            {
                Name = nameof(ConsoleRendererTests),
                Category = "CLI",
                Passed = true,
                Message = "All ConsoleRenderer tests passed"
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(ConsoleRendererTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(ConsoleRendererTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }

    private Task TestRenderSuccess()
    {
        var renderer = new ConsoleRenderer();
        
        // Should not throw
        renderer.RenderSuccess("Test success message");
        
        return Task.CompletedTask;
    }

    private Task TestRenderError()
    {
        var renderer = new ConsoleRenderer();
        
        // Should not throw
        renderer.RenderError("Test error message");
        
        return Task.CompletedTask;
    }

    private Task TestRenderErrorWithCode()
    {
        var renderer = new ConsoleRenderer();
        
        // Should not throw
        renderer.RenderErrorWithCode("Test error", "ERROR_001", "Test suggestion");
        renderer.RenderErrorWithCode("Test error", "ERROR_001", null);
        renderer.RenderErrorWithCode("Test error", null, null);
        
        return Task.CompletedTask;
    }

    private Task TestRenderProgressStart()
    {
        var renderer = new ConsoleRenderer();
        
        // Should not throw
        renderer.RenderProgressStart("Starting operation");
        
        return Task.CompletedTask;
    }

    private Task TestRenderProgressComplete()
    {
        var renderer = new ConsoleRenderer();
        
        // Should not throw
        renderer.RenderProgressComplete("Operation completed");
        
        return Task.CompletedTask;
    }

    private Task TestRenderProgress()
    {
        var renderer = new ConsoleRenderer();
        
        var report = new ProgressReport
        {
            Percentage = 50,
            Message = "Halfway done",
            CurrentStep = 5,
            TotalSteps = 10
        };
        
        // Should not throw
        renderer.RenderProgress(report);
        
        return Task.CompletedTask;
    }

    private Task TestProgressGoesToStandardError()
    {
        var renderer = new ConsoleRenderer();

        // Not disposed on purpose: a disposed writer left in Console.Out poisons later tests.
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            renderer.RenderProgressStart("starting");
            renderer.RenderProgress(new ProgressReport
            {
                Percentage = 50,
                Message = "halfway",
                CurrentStep = 5,
                TotalSteps = 10
            });
            renderer.RenderProgressComplete("done");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }

        // Progress is diagnostic. On stdout it brackets the document --format-json promises, and a
        // caller piping stdout into a parser gets a syntax error instead of a result.
        AssertEqual(string.Empty, stdout.ToString(), "progress must not reach standard output");

        var diagnostics = stderr.ToString();
        AssertTrue(diagnostics.Contains("[progress] starting"), "progress start missing from standard error");
        AssertTrue(diagnostics.Contains("[50%] (5/10) halfway"), "progress report missing from standard error");
        AssertTrue(diagnostics.Contains("[complete] done"), "progress completion missing from standard error");

        return Task.CompletedTask;
    }

    private Task TestRenderAnalysisResult()
    {
        var renderer = new ConsoleRenderer();
        
        var result = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };
        
        // Should not throw
        renderer.RenderAnalysisResult(result, false);
        
        return Task.CompletedTask;
    }

    private Task TestRenderAnalysisResultJson()
    {
        var renderer = new ConsoleRenderer();
        
        var result = new AnalysisResult
        {
            HasViolations = true,
            Violations = new List<Violation>
            {
                new Violation { Rule = "TestRule", Message = "Test violation", FilePath = "test.cs", Severity = RiskLevel.High }
            },
            TotalViolations = 1
        };
        
        // Should not throw
        renderer.RenderAnalysisResult(result, true);
        
        return Task.CompletedTask;
    }

    private Task TestRenderValidationResult()
    {
        var renderer = new ConsoleRenderer();
        
        var result = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 5,
            TestsPassed = 5,
            TestsFailed = 0
        };
        
        // Should not throw
        renderer.RenderValidationResult(result, false);
        
        return Task.CompletedTask;
    }

    private Task TestRenderValidationResultJson()
    {
        var renderer = new ConsoleRenderer();
        
        var result = new ValidationResult
        {
            Passed = false,
            Message = "Some tests failed",
            TestsRun = 5,
            TestsPassed = 3,
            TestsFailed = 2,
            TestResults = new List<TestResult>
            {
                new TestResult { Name = "Test1", Passed = true },
                new TestResult { Name = "Test2", Passed = false, Message = "Failed" }
            }
        };
        
        // Should not throw
        renderer.RenderValidationResult(result, true);
        
        return Task.CompletedTask;
    }

    private Task TestRenderAgentResult()
    {
        var renderer = new ConsoleRenderer();
        
        var result = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = true,
            Message = "Agent executed successfully",
            Duration = TimeSpan.FromSeconds(1)
        };
        
        // Should not throw
        renderer.RenderAgentResult(result, false);
        
        return Task.CompletedTask;
    }

    private Task TestRenderAgentResultJson()
    {
        var renderer = new ConsoleRenderer();
        
        var result = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = false,
            Message = "Agent execution failed",
            Duration = TimeSpan.FromSeconds(2)
        };
        
        // Should not throw
        renderer.RenderAgentResult(result, true);
        
        return Task.CompletedTask;
    }

    private Task TestRenderAgentList()
    {
        var renderer = new ConsoleRenderer();
        
        var agents = new List<AgentMetadata>
        {
            new AgentMetadata { Name = "Agent1", Description = "Test agent 1" },
            new AgentMetadata { Name = "Agent2", Description = "Test agent 2" }
        };
        
        // Should not throw
        renderer.RenderAgentList(agents, false);
        
        return Task.CompletedTask;
    }

    private Task TestRenderAgentListJson()
    {
        var renderer = new ConsoleRenderer();
        
        var agents = new List<AgentMetadata>
        {
            new AgentMetadata { Name = "Agent1", Description = "Test agent 1" }
        };
        
        // Should not throw
        renderer.RenderAgentList(agents, true);
        
        return Task.CompletedTask;
    }
}

