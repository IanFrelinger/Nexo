using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Collaboration;
using Nexo.Infrastructure.Services.Collaboration;

namespace Nexo.Infrastructure.Tests.Services.Collaboration
{
    /// <summary>
    /// Comprehensive E2E tests for Team Collaboration Service in Phase 9.
    /// Tests all team collaboration capabilities including team management,
    /// collaboration workflows, and team analytics.
    /// </summary>
    public class TeamCollaborationServiceTests : IDisposable
    {
        private readonly Mock<ILogger<TeamCollaborationService>> _mockLogger;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly TeamCollaborationService _teamCollaborationService;

        public TeamCollaborationServiceTests()
        {
            _mockLogger = new Mock<ILogger<TeamCollaborationService>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _teamCollaborationService = new TeamCollaborationService(_mockLogger.Object, _mockModelOrchestrator.Object);
        }

        [Fact]
        public async Task CreateTeamAsync_ValidTeamData_ReturnsTeamResult()
        {
            // Arrange
            var teamData = new TeamData
            {
                Id = "test-team-1",
                Name = "Development Team",
                Description = "Core development team for the project",
                Members = new List<string> { "user-1", "user-2", "user-3" },
                Roles = new Dictionary<string, string> { { "user-1", "Lead" }, { "user-2", "Developer" }, { "user-3", "Tester" } },
                Permissions = new List<string> { "read", "write", "admin" },
                CreatedBy = "admin-1"
            };

            var mockResponse = new ModelResponse
            {
                Content = "Team created successfully. Name: Development Team, Members: 3, Roles: Lead, Developer, Tester, Permissions: read, write, admin. Team ID: test-team-1.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _teamCollaborationService.CreateTeamAsync(teamData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully created team", result.Message);
            Assert.Equal(teamData.Id, result.TeamId);
            Assert.Equal(teamData.Name, result.TeamName);
            Assert.Equal(teamData.Members.Count, result.MemberCount);
            Assert.NotEmpty(result.Roles);
            Assert.NotEmpty(result.Permissions);
            Assert.NotNull(result.Metrics);
            Assert.True(result.CreatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AddTeamMemberAsync_ValidMemberData_ReturnsMemberResult()
        {
            // Arrange
            var memberData = new TeamMemberData
            {
                Id = "test-member-1",
                TeamId = "test-team-1",
                UserId = "user-4",
                Role = "Developer",
                Permissions = new List<string> { "read", "write" },
                AddedBy = "admin-1"
            };

            var mockResponse = new ModelResponse
            {
                Content = "Team member added successfully. User: user-4, Role: Developer, Permissions: read, write, Team: test-team-1. Member ID: test-member-1.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _teamCollaborationService.AddTeamMemberAsync(memberData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully added team member", result.Message);
            Assert.Equal(memberData.Id, result.MemberId);
            Assert.Equal(memberData.TeamId, result.TeamId);
            Assert.Equal(memberData.UserId, result.UserId);
            Assert.Equal(memberData.Role, result.Role);
            Assert.NotEmpty(result.Permissions);
            Assert.NotNull(result.Metrics);
            Assert.True(result.AddedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task CreateCollaborationWorkflowAsync_ValidWorkflowData_ReturnsWorkflowResult()
        {
            // Arrange
            var workflowData = new CollaborationWorkflowData
            {
                Id = "test-workflow-1",
                Name = "Feature Development Workflow",
                Description = "Standard workflow for feature development",
                Steps = new List<string> { "Design", "Development", "Testing", "Review", "Deployment" },
                TeamId = "test-team-1",
                Assignees = new Dictionary<string, string> { { "Design", "user-1" }, { "Development", "user-2" }, { "Testing", "user-3" } },
                Deadlines = new Dictionary<string, DateTimeOffset> { { "Design", DateTimeOffset.UtcNow.AddDays(2) }, { "Development", DateTimeOffset.UtcNow.AddDays(5) } }
            };

            var mockResponse = new ModelResponse
            {
                Content = "Collaboration workflow created successfully. Name: Feature Development Workflow, Steps: 5, Team: test-team-1, Assignees: 3, Deadlines: 2. Workflow ID: test-workflow-1.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _teamCollaborationService.CreateCollaborationWorkflowAsync(workflowData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully created collaboration workflow", result.Message);
            Assert.Equal(workflowData.Id, result.WorkflowId);
            Assert.Equal(workflowData.Name, result.WorkflowName);
            Assert.Equal(workflowData.Steps.Count, result.StepCount);
            Assert.Equal(workflowData.TeamId, result.TeamId);
            Assert.True(result.AssigneeCount > 0);
            Assert.True(result.DeadlineCount > 0);
            Assert.NotNull(result.Metrics);
            Assert.True(result.CreatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ExecuteWorkflowStepAsync_ValidStepData_ReturnsStepResult()
        {
            // Arrange
            var stepData = new WorkflowStepData
            {
                Id = "test-step-1",
                WorkflowId = "test-workflow-1",
                StepName = "Development",
                Assignee = "user-2",
                Status = "In Progress",
                Comments = "Starting development work",
                Attachments = new List<string> { "design-doc.pdf", "requirements.md" }
            };

            var mockResponse = new ModelResponse
            {
                Content = "Workflow step executed successfully. Step: Development, Assignee: user-2, Status: In Progress, Comments: 1, Attachments: 2. Step ID: test-step-1.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _teamCollaborationService.ExecuteWorkflowStepAsync(stepData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully executed workflow step", result.Message);
            Assert.Equal(stepData.Id, result.StepId);
            Assert.Equal(stepData.WorkflowId, result.WorkflowId);
            Assert.Equal(stepData.StepName, result.StepName);
            Assert.Equal(stepData.Assignee, result.Assignee);
            Assert.Equal(stepData.Status, result.Status);
            Assert.True(result.CommentCount > 0);
            Assert.True(result.AttachmentCount > 0);
            Assert.NotNull(result.Metrics);
            Assert.True(result.ExecutedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GetTeamAnalyticsAsync_ValidTeamId_ReturnsAnalyticsResult()
        {
            // Arrange
            var teamId = "test-team-1";

            var mockResponse = new ModelResponse
            {
                Content = "Team analytics generated successfully. Team: test-team-1, Members: 3, Active workflows: 2, Completed tasks: 15, Productivity score: 85%, Collaboration index: 0.92.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _teamCollaborationService.GetTeamAnalyticsAsync(teamId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully generated team analytics", result.Message);
            Assert.Equal(teamId, result.TeamId);
            Assert.True(result.MemberCount > 0);
            Assert.True(result.ActiveWorkflows >= 0);
            Assert.True(result.CompletedTasks >= 0);
            Assert.True(result.ProductivityScore > 0);
            Assert.True(result.CollaborationIndex > 0);
            Assert.NotNull(result.Metrics);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateTeamReportAsync_ValidReportRequest_ReturnsTeamReport()
        {
            // Arrange
            var reportRequest = new TeamReportRequest
            {
                Id = "test-report-1",
                TeamId = "test-team-1",
                ReportType = "Monthly Performance",
                Period = "Last 30 days",
                IncludeMetrics = true,
                IncludeWorkflows = true,
                Format = "PDF"
            };

            var mockResponse = new ModelResponse
            {
                Content = "Team report generated successfully. Type: Monthly Performance, Period: Last 30 days, Format: PDF, Pages: 12, Sections: 6, Metrics: 20, Workflows: 3.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _teamCollaborationService.GenerateTeamReportAsync(reportRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully generated team report", result.Message);
            Assert.Equal(reportRequest.Id, result.RequestId);
            Assert.Equal(reportRequest.TeamId, result.TeamId);
            Assert.Equal(reportRequest.ReportType, result.ReportType);
            Assert.Equal(reportRequest.Period, result.Period);
            Assert.Equal(reportRequest.Format, result.Format);
            Assert.True(result.Pages > 0);
            Assert.True(result.Sections > 0);
            Assert.True(result.MetricsCount > 0);
            Assert.True(result.WorkflowCount > 0);
            Assert.NotNull(result.ReportData);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task CreateTeamAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var teamData = new TeamData
            {
                Id = "test-team-error",
                Name = "Error Team"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _teamCollaborationService.CreateTeamAsync(teamData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(teamData.Id, result.TeamId);
            Assert.True(result.CreatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AddTeamMemberAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var memberData = new TeamMemberData
            {
                Id = "test-member-error",
                TeamId = "test-team-error"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _teamCollaborationService.AddTeamMemberAsync(memberData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(memberData.Id, result.MemberId);
            Assert.True(result.AddedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task CreateCollaborationWorkflowAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var workflowData = new CollaborationWorkflowData
            {
                Id = "test-workflow-error",
                Name = "Error Workflow"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _teamCollaborationService.CreateCollaborationWorkflowAsync(workflowData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(workflowData.Id, result.WorkflowId);
            Assert.True(result.CreatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ExecuteWorkflowStepAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var stepData = new WorkflowStepData
            {
                Id = "test-step-error",
                WorkflowId = "test-workflow-error"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _teamCollaborationService.ExecuteWorkflowStepAsync(stepData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(stepData.Id, result.StepId);
            Assert.True(result.ExecutedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GetTeamAnalyticsAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var teamId = "test-team-error";

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _teamCollaborationService.GetTeamAnalyticsAsync(teamId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(teamId, result.TeamId);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateTeamReportAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var reportRequest = new TeamReportRequest
            {
                Id = "test-report-error",
                TeamId = "test-team-error"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _teamCollaborationService.GenerateTeamReportAsync(reportRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(reportRequest.Id, result.RequestId);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task CreateTeamAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var teamData = new TeamData
            {
                Id = "test-team-cancel",
                Name = "Cancel Team"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _teamCollaborationService.CreateTeamAsync(teamData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task AddTeamMemberAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var memberData = new TeamMemberData
            {
                Id = "test-member-cancel",
                TeamId = "test-team-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _teamCollaborationService.AddTeamMemberAsync(memberData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task CreateCollaborationWorkflowAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var workflowData = new CollaborationWorkflowData
            {
                Id = "test-workflow-cancel",
                Name = "Cancel Workflow"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _teamCollaborationService.CreateCollaborationWorkflowAsync(workflowData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ExecuteWorkflowStepAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var stepData = new WorkflowStepData
            {
                Id = "test-step-cancel",
                WorkflowId = "test-workflow-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _teamCollaborationService.ExecuteWorkflowStepAsync(stepData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GetTeamAnalyticsAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var teamId = "test-team-cancel";

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _teamCollaborationService.GetTeamAnalyticsAsync(teamId, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateTeamReportAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var reportRequest = new TeamReportRequest
            {
                Id = "test-report-cancel",
                TeamId = "test-team-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _teamCollaborationService.GenerateTeamReportAsync(reportRequest, cancellationTokenSource.Token));
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
