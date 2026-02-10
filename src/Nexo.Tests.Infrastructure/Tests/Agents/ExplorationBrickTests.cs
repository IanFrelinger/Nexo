using Microsoft.Extensions.Logging;
using Moq;
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
/// Unit tests for ExplorationBrick: deterministic action selection and
/// conversion of "x,y" targets into TestAction.Coordinates.
/// </summary>
public class ExplorationBrickTests : UnitTestBase
{
    [Fact]
    public async Task ExplorationBrick_AllTests_Pass()
    {
        var result = await ExecuteAsync(CancellationToken.None);
        Assert.True(result.Passed, result.ErrorMessage ?? result.Message);
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestDeterministic_WhenActionHasCoordinateTarget_SetsCoordinatesOnTestAction();
            await TestDeterministic_WhenActionHasTypeAndValue_SetsInputValue();
            await TestDeterministic_NoActions_ReturnsWaitAndShouldStop();
            return new TestResult
            {
                TestName = nameof(ExplorationBrickTests),
                Category = "Agents",
                Passed = true,
                Message = "All ExplorationBrick tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ExplorationBrickTests),
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
                TestName = nameof(ExplorationBrickTests),
                Category = "Agents",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestDeterministic_WhenActionHasCoordinateTarget_SetsCoordinatesOnTestAction()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var brick = new ExplorationBrick(mockFactory.Object, null);
        var understanding = new Understanding
        {
            ScreenType = "Desktop",
            CurrentContext = "Main window",
            CurrentObjective = "Explore",
            ProgressPercent = 0,
            Confidence = 0.8,
            AvailableActions =
            [
                new AvailableAction
                {
                    Id = "click_chat",
                    Description = "Click chat area",
                    Type = "click",
                    Target = "600,580",
                    RelevanceToGoal = 0.9,
                    ExplorationValue = 0.7,
                    RiskLevel = 0.1
                }
            ]
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["understanding"] = understanding,
            ["goal"] = "Test chat",
            ["depth"] = TestingDepth.Quick
        });
        var context = new Mock<IExecutionContext>();

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context.Object, CancellationToken.None);

        var nextAction = output.Get<TestAction>("nextAction");
        AssertNotNull(nextAction);
        AssertNotNull(nextAction.Coordinates, "Target \"600,580\" should be parsed into Coordinates");
        AssertEqual(600.0, nextAction.Coordinates!.X);
        AssertEqual(580.0, nextAction.Coordinates!.Y);
        AssertEqual("600,580", nextAction.Selector);
    }

    private async Task TestDeterministic_WhenActionHasTypeAndValue_SetsInputValue()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var brick = new ExplorationBrick(mockFactory.Object, null);
        var understanding = new Understanding
        {
            ScreenType = "Desktop",
            CurrentContext = "Chat",
            CurrentObjective = "Type message",
            ProgressPercent = 0,
            Confidence = 0.8,
            AvailableActions =
            [
                new AvailableAction
                {
                    Id = "type_message",
                    Description = "Type in chat",
                    Type = "type",
                    Target = "600,580",
                    Value = "hello",
                    RelevanceToGoal = 0.95,
                    ExplorationValue = 0.8,
                    RiskLevel = 0.1
                }
            ]
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["understanding"] = understanding,
            ["goal"] = "Test chat",
            ["depth"] = TestingDepth.Quick
        });
        var context = new Mock<IExecutionContext>();

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context.Object, CancellationToken.None);

        var nextAction = output.Get<TestAction>("nextAction");
        AssertNotNull(nextAction);
        AssertEqual("hello", nextAction.InputValue);
        AssertNotNull(nextAction.Coordinates);
        AssertEqual(600.0, nextAction.Coordinates!.X);
        AssertEqual(580.0, nextAction.Coordinates!.Y);
    }

    private async Task TestDeterministic_NoActions_ReturnsWaitAndShouldStop()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var brick = new ExplorationBrick(mockFactory.Object, null);
        var understanding = new Understanding
        {
            ScreenType = "Unknown",
            CurrentContext = "No UI",
            CurrentObjective = "Wait",
            ProgressPercent = 0,
            Confidence = 0.5,
            AvailableActions = Array.Empty<AvailableAction>()
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["understanding"] = understanding,
            ["goal"] = "Test",
            ["depth"] = TestingDepth.Quick
        });
        var context = new Mock<IExecutionContext>();

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context.Object, CancellationToken.None);

        var nextAction = output.Get<TestAction>("nextAction");
        var shouldStop = output.Get<bool>("shouldStop");
        AssertNotNull(nextAction);
        AssertEqual(ActionType.Wait, nextAction.Type);
        AssertTrue(shouldStop, "Should stop when no actions available");
    }
}
