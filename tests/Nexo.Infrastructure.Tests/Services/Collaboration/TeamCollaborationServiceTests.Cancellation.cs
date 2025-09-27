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
    /// Cancellation tests for TeamCollaborationServiceTests.
    /// </summary>
    public partial class TeamCollaborationServiceTests
    {
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
    }
}
