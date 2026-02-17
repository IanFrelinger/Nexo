using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Bricks;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Agents;

/// <summary>
/// Validates each Universal Tester brick component individually.
/// Ensures both primary (agentic) and fallback (deterministic) implementations work when applicable.
/// </summary>
public class UniversalTesterBrickComponentTests : UnitTestBase
{
    [Fact]
    public async Task UniversalTesterBrick_AllComponentTests_Pass()
    {
        var result = await ExecuteAsync(CancellationToken.None);
        Assert.True(result.Passed, result.ErrorMessage ?? result.Message);
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // PerceptionBrick - deterministic and agentic (primary)
            await TestPerceptionBrick_Deterministic_CapturesStateFromAdapter();
            await TestPerceptionBrick_Agentic_WhenProviderReturnsElements_AugmentsCapture();

            // ActionExecutorBrick - deterministic only (also covered in ScreenInteractionTests)
            await TestActionExecutorBrick_Deterministic_ExecutesActionViaAdapter();

            // UnderstandingBrick - deterministic and agentic
            await TestUnderstandingBrick_Deterministic_WhenAirGapped_ReturnsHeuristicUnderstanding();
            await TestUnderstandingBrick_Agentic_WhenInteractiveElementsExist_UsesLLMPathNotVision();

            // ExplorationBrick - deterministic and agentic
            await TestExplorationBrick_Agentic_WhenProviderReturnsValidJson_ParsesNextAction();

            // ValidationBrick - deterministic and agentic (has fallback in runtime)
            await TestValidationBrick_Deterministic_WhenSuccess_ReturnsPassed();
            await TestValidationBrick_Deterministic_WhenFailure_ReturnsIssues();
            await TestValidationBrick_Agentic_WhenProviderReturnsValidJson_ParsesValidation();

            // ReportingBrick - deterministic and agentic (has fallback in runtime)
            await TestReportingBrick_Deterministic_GeneratesReportWithFindings();
            await TestReportingBrick_Agentic_WhenProviderReturnsValidJson_AddsAiFindings();

            return new TestResult
            {
                TestName = nameof(UniversalTesterBrickComponentTests),
                Category = "Agents",
                Passed = true,
                Message = "All Universal Tester brick component tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(UniversalTesterBrickComponentTests),
                Category = "Agents",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(UniversalTesterBrickComponentTests),
                Category = "Agents",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    #region PerceptionBrick (Deterministic + Agentic)

    private async Task TestPerceptionBrick_Deterministic_CapturesStateFromAdapter()
    {
        var adapter = new RecordingTargetAdapter();
        var brick = new PerceptionBrick(null, null);
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["adapter"] = adapter,
            ["captureScreenshot"] = true,
            ["captureDOM"] = true,
            ["capturePerformance"] = true
        });
        var context = new Mock<IExecutionContext>().Object;

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context, CancellationToken.None);

        var perception = output.Get<PerceptionState>("perception");
        AssertNotNull(perception);
        AssertNotNull(output.Summary);
        // Adapter returns empty arrays; perception should still be valid
        AssertEqual(0, perception.InteractiveElements.Count);
    }

    private async Task TestPerceptionBrick_Agentic_WhenProviderReturnsElements_AugmentsCapture()
    {
        var mockFactory = new Mock<IProviderFactory>();
        mockFactory.Setup(f => f.IsProviderAvailable(It.IsAny<string>())).Returns(true);
        var visionJson = @"{
  ""elements"": [
    { ""id"": ""btn_send"", ""type"": ""button"", ""label"": ""Send"", ""x"": 820, ""y"": 580, ""width"": 60, ""height"": 30 },
    { ""id"": ""input_chat"", ""type"": ""input"", ""label"": ""Message"", ""x"": 590, ""y"": 570, ""width"": 200, ""height"": 40 }
  ]
}";
        mockFactory
            .Setup(f => f.ExecuteVisionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(visionJson);

        var adapter = new RecordingTargetAdapter(new byte[] { 0x89, 0x50, 0x4E }); // Non-empty screenshot, empty InteractiveElements
        var brick = new PerceptionBrick(mockFactory.Object, null);
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["adapter"] = adapter,
            ["captureScreenshot"] = true,
            ["captureDOM"] = true,
            ["capturePerformance"] = true
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.IsAirGapped).Returns(false);
        context.Setup(c => c.Provider).Returns("mock");

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, context.Object, CancellationToken.None);

        var perception = output.Get<PerceptionState>("perception");
        AssertNotNull(perception);
        AssertEqual(2, perception.InteractiveElements.Count);
        AssertEqual("btn_send", perception.InteractiveElements[0].Id);
        AssertEqual("button", perception.InteractiveElements[0].Type);
        AssertEqual("Send", perception.InteractiveElements[0].Label);
        AssertNotNull(perception.InteractiveElements[0].Bounds);
        AssertEqual(820.0, perception.InteractiveElements[0].Bounds!.X);
        AssertEqual("input_chat", perception.InteractiveElements[1].Id);
        AssertEqual("input", perception.InteractiveElements[1].Type);
    }

    #endregion

    #region ActionExecutorBrick (Deterministic only)

    private async Task TestActionExecutorBrick_Deterministic_ExecutesActionViaAdapter()
    {
        var recording = new RecordingTargetAdapter();
        var brick = new ActionExecutorBrick(null);
        var action = new TestAction
        {
            Id = "click_1",
            Type = ActionType.Click,
            Description = "Click",
            Coordinates = new Vector2 { X = 100, Y = 200 }
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["action"] = action,
            ["adapter"] = recording
        });
        var context = new Mock<IExecutionContext>().Object;

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context, CancellationToken.None);

        var result = output.Get<ActionExecutionResult>("result");
        AssertNotNull(result);
        AssertTrue(result.Success);
        AssertEqual(1, recording.ExecutedActions.Count);
        AssertEqual(ActionType.Click, recording.ExecutedActions[0].Type);
    }

    #endregion

    #region UnderstandingBrick (Deterministic + Agentic)

    private async Task TestUnderstandingBrick_Deterministic_WhenAirGapped_ReturnsHeuristicUnderstanding()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var brick = new UnderstandingBrick(mockFactory.Object, null);
        var perception = new PerceptionState
        {
            InteractiveElements =
            [
                new InteractiveElement { Id = "btn1", Type = "button", Label = "Submit" }
            ]
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["perception"] = perception,
            ["goal"] = "Test form"
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.IsAirGapped).Returns(true);

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context.Object, CancellationToken.None);

        var understanding = output.Get<Understanding>("understanding");
        AssertNotNull(understanding);
        AssertEqual(1, understanding.AvailableActions.Count);
        AssertEqual("btn1", understanding.AvailableActions[0].Id);
        mockFactory.Verify(f => f.ExecuteLLMAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task TestUnderstandingBrick_Agentic_WhenInteractiveElementsExist_UsesLLMPathNotVision()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var llmJson = @"{
  ""screenType"": ""Web form"",
  ""currentContext"": ""Login form with username and password."",
  ""availableActions"": [
    { ""id"": ""click_submit"", ""description"": ""Click Submit"", ""type"": ""click"", ""target"": ""submit_btn"", ""relevanceToGoal"": 0.95, ""explorationValue"": 0.5, ""riskLevel"": 0.1 }
  ],
  ""currentObjective"": ""Submit form"",
  ""progressPercent"": 0,
  ""issues"": [],
  ""unexploredAreas"": [],
  ""confidence"": 0.9
}";
        mockFactory
            .Setup(f => f.ExecuteLLMAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmJson);
        var brick = new UnderstandingBrick(mockFactory.Object, null);
        var perception = new PerceptionState
        {
            CurrentUrl = "https://example.com/login",
            Screenshot = new byte[] { 0x89, 0x50 }, // has screenshot
            InteractiveElements = [new InteractiveElement { Id = "user", Type = "input", Label = "Username" }] // has elements -> uses LLM, not vision
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["perception"] = perception,
            ["goal"] = "Test login"
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.IsAirGapped).Returns(false);
        context.Setup(c => c.Provider).Returns("mock");

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, context.Object, CancellationToken.None);

        var understanding = output.Get<Understanding>("understanding");
        AssertNotNull(understanding);
        AssertEqual("Web form", understanding.ScreenType);
        AssertEqual(1, understanding.AvailableActions.Count);
        AssertEqual("click_submit", understanding.AvailableActions[0].Id);
        mockFactory.Verify(f => f.ExecuteLLMAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        mockFactory.Verify(f => f.ExecuteVisionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ExplorationBrick (Deterministic + Agentic)

    private async Task TestExplorationBrick_Agentic_WhenProviderReturnsValidJson_ParsesNextAction()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var llmJson = @"{
  ""nextActionId"": ""click_send"",
  ""reasoning"": ""Send is most relevant to goal"",
  ""shouldStop"": false
}";
        mockFactory
            .Setup(f => f.ExecuteLLMAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmJson);
        var brick = new ExplorationBrick(mockFactory.Object, null);
        var understanding = new Understanding
        {
            ScreenType = "Chat",
            CurrentContext = "Main chat window",
            CurrentObjective = "Send a message",
            AvailableActions =
            [
                new AvailableAction { Id = "click_send", Description = "Click Send", Type = "click", Target = "820,580", RelevanceToGoal = 0.9, ExplorationValue = 0.6, RiskLevel = 0.1 }
            ]
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["understanding"] = understanding,
            ["goal"] = "Send a message",
            ["depth"] = TestingDepth.Standard
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.Provider).Returns("mock");

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, context.Object, CancellationToken.None);

        var nextAction = output.Get<TestAction>("nextAction");
        var shouldStop = output.Get<bool>("shouldStop");
        AssertNotNull(nextAction);
        AssertEqual("click_send", nextAction.Id);
        AssertFalse(shouldStop);
        AssertEqual(ActionType.Click, nextAction.Type);
    }

    #endregion

    #region ValidationBrick (Deterministic + Agentic)

    private async Task TestValidationBrick_Deterministic_WhenSuccess_ReturnsPassed()
    {
        var brick = new ValidationBrick(null!, null);
        var executionResult = new ActionExecutionResult { Success = true };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["action"] = new TestAction { Id = "click_1", Type = ActionType.Click, Description = "Click" },
            ["executionResult"] = executionResult,
            ["goal"] = "Test"
        });
        var context = new Mock<IExecutionContext>().Object;

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context, CancellationToken.None);

        var validation = output.Get<ValidationResult>("validation");
        AssertNotNull(validation);
        AssertTrue(validation.Passed);
        AssertEqual(0, validation.IssuesFound.Count);
    }

    private async Task TestValidationBrick_Deterministic_WhenFailure_ReturnsIssues()
    {
        var brick = new ValidationBrick(null!, null);
        var executionResult = new ActionExecutionResult { Success = false, Error = "Click failed" };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["action"] = new TestAction { Id = "click_1", Type = ActionType.Click, Description = "Click" },
            ["executionResult"] = executionResult,
            ["goal"] = "Test"
        });
        var context = new Mock<IExecutionContext>().Object;

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context, CancellationToken.None);

        var validation = output.Get<ValidationResult>("validation");
        AssertNotNull(validation);
        AssertFalse(validation.Passed);
        AssertEqual(1, validation.IssuesFound.Count);
        AssertEqual("error", validation.IssuesFound[0].Type);
        AssertEqual(IssueSeverity.High, validation.IssuesFound[0].Severity);
    }

    private async Task TestValidationBrick_Agentic_WhenProviderReturnsValidJson_ParsesValidation()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var llmJson = @"{
  ""passed"": true,
  ""reasoning"": ""Action completed successfully"",
  ""issues"": [],
  ""confidence"": 0.85
}";
        mockFactory
            .Setup(f => f.ExecuteLLMAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmJson);
        var brick = new ValidationBrick(mockFactory.Object, null);
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["action"] = new TestAction { Id = "click_1", Type = ActionType.Click, Description = "Click" },
            ["executionResult"] = new ActionExecutionResult { Success = true },
            ["goal"] = "Test"
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.Provider).Returns("mock");

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, context.Object, CancellationToken.None);

        var validation = output.Get<ValidationResult>("validation");
        AssertNotNull(validation);
        AssertTrue(validation.Passed);
        AssertEqual(0.85, validation.Confidence);
    }

    #endregion

    #region ReportingBrick (Deterministic + Agentic)

    private async Task TestReportingBrick_Deterministic_GeneratesReportWithFindings()
    {
        var brick = new ReportingBrick(null, null);
        var session = new TestSession
        {
            Goal = "Test checkout",
            TargetDescription = "Web: https://shop.com",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            Steps =
            [
                new TestStep
                {
                    StepNumber = 1,
                    Action = new TestAction { Id = "click", Type = ActionType.Click, Description = "Click" },
                    ExecutionResult = new ActionExecutionResult { Success = true },
                    Validation = new ValidationResult { Passed = true, Reasoning = "OK", Confidence = 0.9 }
                }
            ],
            AllIssues = [],
            ProgressPercent = 90,
            GoalAchieved = true,
            StopReason = "Complete"
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["session"] = session,
            ["format"] = "html"
        });
        var context = new Mock<IExecutionContext>().Object;

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context, CancellationToken.None);

        var report = output.Get<TestReport>("report");
        AssertNotNull(report);
        AssertNotNull(report.Summary);
        AssertEqual(1, report.Summary.TotalTests);
        AssertEqual(1, report.Summary.Passed);
        AssertNotNull(report.HtmlReport);
        AssertTrue((report.HtmlReport ?? "").Contains("Test Report", StringComparison.OrdinalIgnoreCase));
    }

    private async Task TestReportingBrick_Agentic_WhenProviderReturnsValidJson_AddsAiFindings()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var llmJson = @"{
  ""findings"": [""Login flow works correctly""],
  ""recommendations"": [""Add error handling for network timeouts""]
}";
        mockFactory
            .Setup(f => f.ExecuteLLMAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmJson);
        var brick = new ReportingBrick(mockFactory.Object, null);
        var session = new TestSession
        {
            Goal = "Test login",
            TargetDescription = "Web: https://example.com",
            StartTime = DateTime.UtcNow.AddMinutes(-2),
            EndTime = DateTime.UtcNow,
            Steps = [],
            AllIssues = [],
            ProgressPercent = 100,
            GoalAchieved = true,
            StopReason = "Complete"
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["session"] = session,
            ["format"] = "html"
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.Provider).Returns("mock");

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, context.Object, CancellationToken.None);

        var report = output.Get<TestReport>("report");
        AssertNotNull(report);
        AssertNotNull(report.Summary.KeyFindings);
        AssertNotNull(report.Summary.Recommendations);
        AssertTrue(report.Summary.KeyFindings.Contains("Login flow works correctly"));
        AssertTrue(report.Summary.Recommendations.Contains("Add error handling for network timeouts"));
    }

    #endregion
}
