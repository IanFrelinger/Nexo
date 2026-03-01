using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Agents.UniversalTester.Bricks;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Agents;

/// <summary>
/// Unit tests for UnderstandingBrick: deterministic understanding, vision/LLM fallback,
/// and desktop fallback actions when screenshot is present but no actions returned.
/// </summary>
public class UnderstandingBrickTests : UnitTestBase
{
    [Fact]
    public async Task UnderstandingBrick_AllTests_Pass()
    {
        var result = await ExecuteAsync(CancellationToken.None);
        Assert.True(result.Passed, result.ErrorMessage ?? result.Message);
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestDeterministicUnderstanding_ReturnsActionsFromInteractiveElements();
            await TestAgentic_WhenProviderReturnsEmptyActions_NoFallbackInjected();
            await TestAgentic_WhenProviderReturnsValidActions_DoesNotOverride();
            await TestVisualModel_WhenScreenshotProvided_VisionResponseParsedIntoCoordinateActions();
            return new TestResult
            {
                Name = nameof(UnderstandingBrickTests),
                Category = "Agents",
                Passed = true,
                Message = "All UnderstandingBrick tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(UnderstandingBrickTests),
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
                Name = nameof(UnderstandingBrickTests),
                Category = "Agents",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestDeterministicUnderstanding_ReturnsActionsFromInteractiveElements()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var brick = new UnderstandingBrick(mockFactory.Object, null);
        var perception = new PerceptionState
        {
            CurrentUrl = "https://example.com",
            InteractiveElements =
            [
                new InteractiveElement { Id = "btn1", Type = "button", Label = "Submit" },
                new InteractiveElement { Id = "link1", Type = "link", Label = "Home" }
            ]
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["perception"] = perception,
            ["goal"] = "Test the form"
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.IsAirGapped).Returns(true);

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context.Object, CancellationToken.None);

        var understanding = output.Get<Understanding>("understanding");
        AssertNotNull(understanding);
        AssertEqual("Web", understanding.ScreenType);
        AssertTrue(understanding.AvailableActions.Count == 2, "Should derive one action per interactive element");
        AssertEqual("btn1", understanding.AvailableActions[0].Id);
        AssertEqual("Submit", understanding.AvailableActions[0].Description);
    }

    private async Task TestAgentic_WhenProviderReturnsEmptyActions_NoFallbackInjected()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var emptyActionsJson = @"{
  ""screenType"": ""Unknown"",
  ""currentContext"": ""Parsed but no actions."",
  ""availableActions"": [],
  ""currentObjective"": ""Explore"",
  ""progressPercent"": 0,
  ""issues"": [],
  ""unexploredAreas"": [],
  ""confidence"": 0.5
}";
        mockFactory
            .Setup(f => f.ExecuteVisionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyActionsJson);
        var brick = new UnderstandingBrick(mockFactory.Object, null);
        var perception = new PerceptionState
        {
            Screenshot = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            InteractiveElements = Array.Empty<InteractiveElement>()
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["perception"] = perception,
            ["goal"] = "Test the app"
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.IsAirGapped).Returns(false);
        context.Setup(c => c.Provider).Returns("mock");

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, context.Object, CancellationToken.None);

        var understanding = output.Get<Understanding>("understanding");
        AssertNotNull(understanding);
        AssertEqual(0, understanding.AvailableActions.Count, "No fallback injection; vision response is returned as-is");
    }

    private async Task TestAgentic_WhenProviderReturnsValidActions_DoesNotOverride()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var oneActionJson = @"{
  ""screenType"": ""Chat app"",
  ""currentContext"": ""Main window."",
  ""availableActions"": [
    { ""id"": ""click_send"", ""description"": ""Click Send"", ""type"": ""click"", ""target"": ""820,580"", ""relevanceToGoal"": 0.9, ""explorationValue"": 0.5, ""riskLevel"": 0.1 }
  ],
  ""currentObjective"": ""Send a message"",
  ""progressPercent"": 0,
  ""issues"": [],
  ""unexploredAreas"": [],
  ""confidence"": 0.8
}";
        mockFactory
            .Setup(f => f.ExecuteVisionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(oneActionJson);
        var brick = new UnderstandingBrick(mockFactory.Object, null);
        var perception = new PerceptionState
        {
            Screenshot = new byte[] { 0x89, 0x50 },
            InteractiveElements = Array.Empty<InteractiveElement>()
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["perception"] = perception,
            ["goal"] = "Test chat"
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.IsAirGapped).Returns(false);
        context.Setup(c => c.Provider).Returns("mock");

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, context.Object, CancellationToken.None);

        var understanding = output.Get<Understanding>("understanding");
        AssertNotNull(understanding);
        AssertEqual(1, understanding.AvailableActions.Count);
        AssertEqual("click_send", understanding.AvailableActions[0].Id);
        AssertEqual("820,580", understanding.AvailableActions[0].Target);
    }

    /// <summary>
    /// Ensures the visual/vision model path works: when a screenshot is provided and no
    /// interactive elements exist, the brick calls the vision API and parses its response
    /// into an understanding with coordinate-based actions (click at x,y, type with value).
    /// </summary>
    private async Task TestVisualModel_WhenScreenshotProvided_VisionResponseParsedIntoCoordinateActions()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var visionResponseJson = @"{
  ""screenType"": ""Chat app with sidebar"",
  ""currentContext"": ""Main window shows a sidebar on the left and a chat area with a text input and Send button at the bottom."",
  ""availableActions"": [
    { ""id"": ""click_sidebar_chat"", ""description"": ""Click Chat in sidebar"", ""type"": ""click"", ""target"": ""250,280"", ""relevanceToGoal"": 0.9, ""explorationValue"": 0.7, ""riskLevel"": 0.1 },
    { ""id"": ""click_chat_input"", ""description"": ""Click in chat input"", ""type"": ""click"", ""target"": ""600,580"", ""relevanceToGoal"": 0.95, ""explorationValue"": 0.8, ""riskLevel"": 0.1 },
    { ""id"": ""type_hello"", ""description"": ""Type hello in chat"", ""type"": ""type"", ""target"": ""600,580"", ""value"": ""hello"", ""relevanceToGoal"": 0.95, ""explorationValue"": 0.8, ""riskLevel"": 0.1 },
    { ""id"": ""click_send"", ""description"": ""Click Send button"", ""type"": ""click"", ""target"": ""820,580"", ""relevanceToGoal"": 0.9, ""explorationValue"": 0.6, ""riskLevel"": 0.1 }
  ],
  ""currentObjective"": ""Explore chat and send a message"",
  ""progressPercent"": 0,
  ""issues"": [],
  ""unexploredAreas"": [""Architecture tab""],
  ""confidence"": 0.85
}";
        mockFactory
            .Setup(f => f.ExecuteVisionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(visionResponseJson)
            .Verifiable();
        var brick = new UnderstandingBrick(mockFactory.Object, null);
        var screenshotBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // minimal PNG header
        var perception = new PerceptionState
        {
            Screenshot = screenshotBytes,
            InteractiveElements = Array.Empty<InteractiveElement>()
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["perception"] = perception,
            ["goal"] = "Test chat and send a message"
        });
        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.IsAirGapped).Returns(false);
        context.Setup(c => c.Provider).Returns("ollama");

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, context.Object, CancellationToken.None);

        mockFactory.Verify(
            f => f.ExecuteVisionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.Is<byte[]>(b => b != null && b.Length == screenshotBytes.Length), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Visual model (ExecuteVisionAsync) should be called once when screenshot is present and no interactive elements");

        var understanding = output.Get<Understanding>("understanding");
        AssertNotNull(understanding);
        AssertEqual("Chat app with sidebar", understanding.ScreenType);
        AssertTrue(understanding.CurrentContext.Contains("sidebar", StringComparison.OrdinalIgnoreCase) || understanding.CurrentContext.Length > 0,
            "CurrentContext should come from vision response");
        AssertEqual(4, understanding.AvailableActions.Count);
        AssertEqual(0.85, understanding.Confidence);

        var clickSend = understanding.AvailableActions.FirstOrDefault(a => a.Id == "click_send");
        AssertNotNull(clickSend);
        AssertEqual("click", clickSend!.Type);
        AssertEqual("820,580", clickSend.Target);

        var typeHello = understanding.AvailableActions.FirstOrDefault(a => a.Id == "type_hello");
        AssertNotNull(typeHello);
        AssertEqual("type", typeHello!.Type);
        AssertEqual("600,580", typeHello.Target);
        AssertEqual("hello", typeHello.Value);

        var allHaveCoordinateTargets = understanding.AvailableActions
            .All(a => a.Target != null && a.Target.Contains(',') && a.Target.Split(',').Length == 2);
        AssertTrue(allHaveCoordinateTargets, "All vision actions should have coordinate targets (x,y)");
    }
}
