using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Agents.UniversalTester.Bricks;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Agents;

/// <summary>
/// Unit tests that validate the testing agent can interact with the screen via clicks,
/// keystrokes (type and keypress). Uses ExplorationBrick to produce TestActions and
/// RecordingTargetAdapter to assert the correct actions are executed.
/// </summary>
public class ScreenInteractionTests : UnitTestBase
{
    [Fact]
    public async Task ScreenInteraction_AllTests_Pass()
    {
        var result = await ExecuteAsync(CancellationToken.None);
        Assert.True(result.Passed, result.ErrorMessage ?? result.Message);
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestAgent_ProducesClickAction_WithCoordinates_WhenUnderstandingHasCoordinateTarget();
            await TestAgent_ProducesTypeAction_WithInputValue_WhenUnderstandingHasTypeAndValue();
            await TestAgent_ProducesKeyPressAction_WhenUnderstandingHasKeyPress();
            await TestRecordingAdapter_RecordsClicksAndKeystrokes_InOrder();
            await TestActionExecutorBrick_ExecutesClickViaAdapter_AndAdapterReceivesAction();
            await TestDesktopAdapter_AcceptsClickWithCoordinates_WithoutThrowing();
            return new TestResult
            {
                Name = nameof(ScreenInteractionTests),
                Category = "Agents",
                Passed = true,
                Message = "All screen interaction tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ScreenInteractionTests),
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
                Name = nameof(ScreenInteractionTests),
                Category = "Agents",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestAgent_ProducesClickAction_WithCoordinates_WhenUnderstandingHasCoordinateTarget()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var brick = new ExplorationBrick(mockFactory.Object, null);
        var understanding = new Understanding
        {
            ScreenType = "Desktop",
            CurrentContext = "Main window",
            CurrentObjective = "Click send button",
            ProgressPercent = 0,
            Confidence = 0.8,
            AvailableActions =
            [
                new AvailableAction
                {
                    Id = "click_send",
                    Description = "Click Send button",
                    Type = "click",
                    Target = "820,580",
                    RelevanceToGoal = 0.95,
                    ExplorationValue = 0.6,
                    RiskLevel = 0.1
                }
            ]
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["understanding"] = understanding,
            ["goal"] = "Send a message",
            ["depth"] = TestingDepth.Quick
        });
        var context = new Mock<IExecutionContext>();

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context.Object, cancellationToken: default);

        var nextAction = output.Get<TestAction>("nextAction");
        AssertNotNull(nextAction);
        AssertEqual(ActionType.Click, nextAction.Type);
        AssertNotNull(nextAction.Coordinates, "Click action should have coordinates for screen interaction");
        AssertEqual(820.0, nextAction.Coordinates!.X);
        AssertEqual(580.0, nextAction.Coordinates!.Y);

        var recording = new RecordingTargetAdapter();
        var result = await recording.ExecuteActionAsync(nextAction, default);
        AssertEqual("OK", result);
        AssertEqual(1, recording.ExecutedActions.Count);
        AssertEqual(ActionType.Click, recording.ExecutedActions[0].Type);
        AssertEqual(820.0, recording.ExecutedActions[0].Coordinates!.X);
        AssertEqual(580.0, recording.ExecutedActions[0].Coordinates!.Y);
    }

    private async Task TestAgent_ProducesTypeAction_WithInputValue_WhenUnderstandingHasTypeAndValue()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var brick = new ExplorationBrick(mockFactory.Object, null);
        var understanding = new Understanding
        {
            ScreenType = "Desktop",
            CurrentContext = "Chat input focused",
            CurrentObjective = "Type message",
            ProgressPercent = 0,
            Confidence = 0.8,
            AvailableActions =
            [
                new AvailableAction
                {
                    Id = "type_hello",
                    Description = "Type hello in chat",
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

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context.Object, cancellationToken: default);

        var nextAction = output.Get<TestAction>("nextAction");
        AssertNotNull(nextAction);
        AssertEqual(ActionType.Type, nextAction.Type);
        AssertEqual("hello", nextAction.InputValue);
        AssertNotNull(nextAction.Coordinates);
        AssertEqual(600.0, nextAction.Coordinates!.X);

        var recording = new RecordingTargetAdapter();
        await recording.ExecuteActionAsync(nextAction, default);
        AssertEqual(1, recording.ExecutedActions.Count);
        AssertEqual(ActionType.Type, recording.ExecutedActions[0].Type);
        AssertEqual("hello", recording.ExecutedActions[0].InputValue);
    }

    private async Task TestAgent_ProducesKeyPressAction_WhenUnderstandingHasKeyPress()
    {
        var mockFactory = new Mock<IProviderFactory>();
        var brick = new ExplorationBrick(mockFactory.Object, null);
        var understanding = new Understanding
        {
            ScreenType = "Desktop",
            CurrentContext = "Input focused",
            CurrentObjective = "Press Enter",
            ProgressPercent = 0,
            Confidence = 0.8,
            AvailableActions =
            [
                new AvailableAction
                {
                    Id = "keypress_enter",
                    Description = "Press Enter",
                    Type = "keypress",
                    Target = null,
                    Value = "enter",
                    RelevanceToGoal = 0.9,
                    ExplorationValue = 0.5,
                    RiskLevel = 0.1
                }
            ]
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["understanding"] = understanding,
            ["goal"] = "Submit form",
            ["depth"] = TestingDepth.Quick
        });
        var context = new Mock<IExecutionContext>();

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context.Object, cancellationToken: default);

        var nextAction = output.Get<TestAction>("nextAction");
        AssertNotNull(nextAction);
        AssertEqual(ActionType.KeyPress, nextAction.Type);
        AssertEqual("enter", nextAction.Key);

        var recording = new RecordingTargetAdapter();
        await recording.ExecuteActionAsync(nextAction, default);
        AssertEqual(1, recording.ExecutedActions.Count);
        AssertEqual(ActionType.KeyPress, recording.ExecutedActions[0].Type);
        AssertEqual("enter", recording.ExecutedActions[0].Key);
    }

    private async Task TestRecordingAdapter_RecordsClicksAndKeystrokes_InOrder()
    {
        var recording = new RecordingTargetAdapter();

        await recording.ExecuteActionAsync(new TestAction
        {
            Id = "click_1",
            Type = ActionType.Click,
            Description = "Click",
            Coordinates = new Vector2 { X = 100, Y = 200 }
        }, default);
        await recording.ExecuteActionAsync(new TestAction
        {
            Id = "type_1",
            Type = ActionType.Type,
            Description = "Type",
            InputValue = "test"
        }, default);
        await recording.ExecuteActionAsync(new TestAction
        {
            Id = "key_1",
            Type = ActionType.KeyPress,
            Description = "Press Enter",
            Key = "enter"
        }, default);

        AssertEqual(3, recording.ExecutedActions.Count);
        AssertEqual(ActionType.Click, recording.ExecutedActions[0].Type);
        AssertEqual(100.0, recording.ExecutedActions[0].Coordinates!.X);
        AssertEqual(200.0, recording.ExecutedActions[0].Coordinates!.Y);
        AssertEqual(ActionType.Type, recording.ExecutedActions[1].Type);
        AssertEqual("test", recording.ExecutedActions[1].InputValue);
        AssertEqual(ActionType.KeyPress, recording.ExecutedActions[2].Type);
        AssertEqual("enter", recording.ExecutedActions[2].Key);
    }

    private async Task TestActionExecutorBrick_ExecutesClickViaAdapter_AndAdapterReceivesAction()
    {
        var executor = new ActionExecutorBrick(null);
        var recording = new RecordingTargetAdapter();
        var action = new TestAction
        {
            Id = "click_send",
            Type = ActionType.Click,
            Description = "Click Send",
            Coordinates = new Vector2 { X = 820, Y = 580 }
        };
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["action"] = action,
            ["adapter"] = recording
        });
        var context = new Mock<IExecutionContext>();

        var output = await executor.ExecuteAsync(input, ImplementationType.Deterministic, context.Object, cancellationToken: default);

        var result = output.Get<ActionExecutionResult>("result");
        AssertNotNull(result);
        AssertTrue(result.Success, "Action execution should succeed with recording adapter");
        AssertEqual(1, recording.ExecutedActions.Count);
        AssertEqual(ActionType.Click, recording.ExecutedActions[0].Type);
        AssertEqual(820.0, recording.ExecutedActions[0].Coordinates!.X);
        AssertEqual(580.0, recording.ExecutedActions[0].Coordinates!.Y);
    }

    private async Task TestDesktopAdapter_AcceptsClickWithCoordinates_WithoutThrowing()
    {
        var adapter = new DesktopAdapter(null);
        var clickAction = new TestAction
        {
            Id = "click_1",
            Type = ActionType.Click,
            Description = "Click at 100,200",
            Coordinates = new Vector2 { X = 100, Y = 200 }
        };

        var result = await adapter.ExecuteActionAsync(clickAction, CancellationToken.None);

        if (OperatingSystem.IsMacOS())
        {
            AssertNotNull(result);
            AssertTrue(result == "OK" || result?.Contains("osascript") == true || result?.Length > 0 == true,
                "On macOS, click should return OK or an error message, not throw");
        }
        else if (OperatingSystem.IsLinux())
        {
            AssertNotNull(result);
            AssertEqual("Linux action not implemented", result);
        }
        else
        {
            AssertNotNull(result);
            AssertEqual("Desktop automation not implemented for this platform", result);
        }
    }
}
