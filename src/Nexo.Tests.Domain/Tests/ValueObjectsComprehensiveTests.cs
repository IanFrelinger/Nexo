using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Values;
using DomainTaskStatus = Nexo.Core.Domain.Values.TaskStatus;

namespace Nexo.Tests.Domain.Tests;

/// <summary>
/// Comprehensive tests for all domain value objects.
/// </summary>
public class ValueObjectsComprehensiveTests : UnitTestBase
{

    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test RiskLevel (already tested in DomainValueObjectsTests, but verify here too)
            TestRiskLevel();
            
            // Test TaskStatus
            TestTaskStatus();
            
            // Test TaskPriority
            TestTaskPriority();
            
            // Test HealthStatus
            TestHealthStatus();
            
            // Test ProjectStatus
            TestProjectStatus();
            
            // Test OnboardingStatus
            TestOnboardingStatus();
            
            // Test MethodVisibility
            TestMethodVisibility();
            
            // Test BaseTypeValue-based value objects
            TestAgentStatus();
            TestSecuritySeverity();
            
            // Test other BaseTypeValue-based value objects
            TestAIConfidenceLevel();
            TestAIEngineType();
            TestAIProviderType();
            TestBetaProgramStatus();
            TestSprintStatus();

            return Task.FromResult(new TestResult
            {
                TestName = TestName,
                Category = Category,
                Passed = true,
                Message = "All value object tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = TestName,
                Category = Category,
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestRiskLevel()
    {
        var low = RiskLevel.Low;
        var medium = RiskLevel.Medium;
        var high = RiskLevel.High;
        var critical = RiskLevel.Critical;

        AssertNotNull(low);
        AssertEqual("Low", low.Name);
        AssertEqual(1, low.Value);
        AssertTrue(low == RiskLevel.Low);
        AssertTrue(low != medium);
        AssertEqual(low, RiskLevel.FromName("Low"));
        AssertEqual(low, RiskLevel.FromValue(1));
        AssertEqual(4, RiskLevel.All.Count);
    }

    private void TestTaskStatus()
    {
        var todo = DomainTaskStatus.Todo;
        var inProgress = DomainTaskStatus.InProgress;
        var done = DomainTaskStatus.Done;
        var blocked = DomainTaskStatus.Blocked;

        AssertNotNull(todo);
        AssertEqual("Todo", todo.Name);
        AssertEqual(0, todo.Value);
        AssertTrue(todo == DomainTaskStatus.Todo);
        AssertTrue(todo != inProgress);
        AssertEqual(todo, DomainTaskStatus.FromName("Todo"));
        AssertEqual(todo, DomainTaskStatus.FromValue(0));
        AssertEqual(4, DomainTaskStatus.All.Count);
        AssertEqual("Todo", todo.ToString());
    }

    private void TestTaskPriority()
    {
        var low = TaskPriority.Low;
        var medium = TaskPriority.Medium;
        var high = TaskPriority.High;
        var critical = TaskPriority.Critical;
        var urgent = TaskPriority.Urgent;

        AssertNotNull(low);
        AssertEqual("Low", low.Name);
        AssertEqual(1, low.Value);
        AssertTrue(low == TaskPriority.Low);
        AssertTrue(low != medium);
        AssertTrue(low < medium);
        AssertTrue(urgent > critical);
        AssertEqual(low, TaskPriority.FromName("Low"));
        AssertEqual(low, TaskPriority.FromValue(1));
        AssertEqual(5, TaskPriority.All.Count);
    }

    private void TestHealthStatus()
    {
        var critical = HealthStatus.Critical;
        var poor = HealthStatus.Poor;
        var fair = HealthStatus.Fair;
        var good = HealthStatus.Good;
        var excellent = HealthStatus.Excellent;

        AssertNotNull(critical);
        AssertEqual("Critical", critical.Name);
        AssertEqual(0, critical.Value);
        AssertTrue(critical == HealthStatus.Critical);
        AssertEqual(critical, HealthStatus.FromName("Critical"));
        AssertEqual(critical, HealthStatus.FromValue(0));
        AssertEqual(5, HealthStatus.All.Count);
    }

    private void TestProjectStatus()
    {
        var notInitialized = ProjectStatus.NotInitialized;
        var planning = ProjectStatus.Planning;
        var active = ProjectStatus.Active;
        var completed = ProjectStatus.Completed;

        AssertNotNull(notInitialized);
        AssertEqual("NotInitialized", notInitialized.Name);
        AssertEqual(0, notInitialized.Value);
        AssertTrue(notInitialized == ProjectStatus.NotInitialized);
        AssertEqual(notInitialized, ProjectStatus.FromName("NotInitialized"));
        AssertEqual(notInitialized, ProjectStatus.FromValue(0));
        AssertEqual(10, ProjectStatus.All.Count);
    }

    private void TestOnboardingStatus()
    {
        var pending = OnboardingStatus.Pending;
        var inProgress = OnboardingStatus.InProgress;
        var completed = OnboardingStatus.Completed;

        AssertNotNull(pending);
        AssertEqual("Pending", pending.Name);
        AssertEqual(0, pending.Value);
        AssertTrue(pending == OnboardingStatus.Pending);
        AssertEqual(pending, OnboardingStatus.FromName("Pending"));
        AssertEqual(pending, OnboardingStatus.FromValue(0));
        AssertEqual(6, OnboardingStatus.All.Count);
    }

    private void TestMethodVisibility()
    {
        var public_ = MethodVisibility.Public;
        var private_ = MethodVisibility.Private;
        var protected_ = MethodVisibility.Protected;
        var internal_ = MethodVisibility.Internal;

        AssertNotNull(public_);
        AssertEqual("Public", public_.Name);
        AssertEqual(0, public_.Value);
        AssertTrue(public_ == MethodVisibility.Public);
        AssertEqual(public_, MethodVisibility.FromName("Public"));
        AssertEqual(public_, MethodVisibility.FromValue(0));
        AssertEqual(4, MethodVisibility.All.Count);
    }

    private void TestAgentStatus()
    {
        var idle = AgentStatus.Idle;
        var active = AgentStatus.Active;

        AssertNotNull(idle);
        AssertEqual("idle", idle.Value);
        AssertEqual("Idle", idle.Display);
        AssertTrue(idle is ITypeValue);
        AssertTrue(idle is BaseTypeValue);
    }

    private void TestSecuritySeverity()
    {
        var low = SecuritySeverity.Low;
        var medium = SecuritySeverity.Medium;
        var high = SecuritySeverity.High;

        AssertNotNull(low);
        AssertEqual("low", low.Value);
        AssertEqual("Low", low.Display);
        AssertTrue(low is ITypeValue);
        AssertTrue(low is BaseTypeValue);
    }

    private void TestAIConfidenceLevel()
    {
        var veryLow = AIConfidenceLevel.VeryLow;
        var low = AIConfidenceLevel.Low;
        var medium = AIConfidenceLevel.Medium;
        var high = AIConfidenceLevel.High;
        var veryHigh = AIConfidenceLevel.VeryHigh;

        AssertNotNull(veryLow);
        AssertEqual("VeryLow", veryLow.Name);
        AssertEqual(0, veryLow.Value);
        AssertTrue(veryLow == AIConfidenceLevel.VeryLow);
        AssertEqual(veryLow, AIConfidenceLevel.FromName("VeryLow"));
        AssertEqual(veryLow, AIConfidenceLevel.FromValue(0));
        AssertEqual(5, AIConfidenceLevel.All.Count);
    }

    private void TestAIEngineType()
    {
        var gpt = AIEngineType.GPT;
        var claude = AIEngineType.Claude;
        var gemini = AIEngineType.Gemini;
        var llama = AIEngineType.Llama;
        var custom = AIEngineType.Custom;

        AssertNotNull(gpt);
        AssertEqual("GPT", gpt.Name);
        AssertEqual(0, gpt.Value);
        AssertTrue(gpt == AIEngineType.GPT);
        AssertEqual(gpt, AIEngineType.FromName("GPT"));
        AssertEqual(gpt, AIEngineType.FromValue(0));
        AssertEqual(5, AIEngineType.All.Count);
    }

    private void TestAIProviderType()
    {
        var mock = AIProviderType.Mock;
        var openAI = AIProviderType.OpenAI;
        var anthropic = AIProviderType.Anthropic;

        AssertNotNull(mock);
        AssertEqual("Mock", mock.Name);
        AssertEqual(0, mock.Value);
        AssertTrue(mock == AIProviderType.Mock);
        AssertEqual(mock, AIProviderType.FromName("Mock"));
        AssertEqual(mock, AIProviderType.FromValue(0));
        AssertEqual(10, AIProviderType.All.Count);
    }

    private void TestBetaProgramStatus()
    {
        var pending = BetaProgramStatus.Pending;
        var active = BetaProgramStatus.Active;
        var completed = BetaProgramStatus.Completed;

        AssertNotNull(pending);
        AssertEqual("Pending", pending.Name);
        AssertEqual(0, pending.Value);
        AssertTrue(pending == BetaProgramStatus.Pending);
        AssertEqual(pending, BetaProgramStatus.FromName("Pending"));
        AssertEqual(pending, BetaProgramStatus.FromValue(0));
        AssertEqual(5, BetaProgramStatus.All.Count);
    }

    private void TestSprintStatus()
    {
        var planning = SprintStatus.Planning;
        var active = SprintStatus.Active;
        var completed = SprintStatus.Completed;
        var closed = SprintStatus.Closed;
        var cancelled = SprintStatus.Cancelled;

        AssertNotNull(planning);
        AssertEqual("Planning", planning.Name);
        AssertEqual(0, planning.Value);
        AssertTrue(planning == SprintStatus.Planning);
        AssertEqual(planning, SprintStatus.FromName("Planning"));
        AssertEqual(planning, SprintStatus.FromValue(0));
        AssertEqual(5, SprintStatus.All.Count);
    }
}

