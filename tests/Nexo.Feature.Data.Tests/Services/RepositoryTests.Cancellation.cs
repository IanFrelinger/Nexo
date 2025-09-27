using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Data.Interfaces;
using Nexo.Feature.Data.Services;
using Xunit;

namespace Nexo.Feature.Data.Tests.Services
{
    /// <summary>
    /// Cancellation tests for Repository
    /// </summary>
    public partial class RepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.GetByIdAsync(id, cts.Token));
        }

        [Fact]
        public async Task GetAllAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.GetAllAsync(cts.Token));
        }

        [Fact]
        public async Task AddAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.AddAsync(entity, cts.Token));
        }

        [Fact]
        public async Task UpdateAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.UpdateAsync(entity, cts.Token));
        }

        [Fact]
        public async Task DeleteAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.DeleteAsync(id, cts.Token));
        }

        [Fact]
        public async Task CountAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.CountAsync(cts.Token));
        }
    }
}
