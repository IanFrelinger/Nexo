using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Integration tests for adapter health checks and functionality.
    /// Tests the offline adapters (Ollama, ComfyUI, Piper) and their health status.
    /// </summary>
    [TestFixture]
    public class Integration_AdapterHealth
    {
        private OllamaAdapter _ollamaAdapter;
        private ComfyUIAdapter _comfyuiAdapter;
        private PiperAdapter _piperAdapter;
        
        [SetUp]
        public void SetUp()
        {
            _ollamaAdapter = new OllamaAdapter();
            _comfyuiAdapter = new ComfyUIAdapter();
            _piperAdapter = new PiperAdapter();
        }
        
        [TearDown]
        public void TearDown()
        {
            _ollamaAdapter?.Dispose();
            _comfyuiAdapter?.Dispose();
            _piperAdapter?.Dispose();
        }
        
        [Test]
        public async Task OllamaAdapter_ShouldHaveCorrectProperties()
        {
            // Assert
            Assert.AreEqual("ollama", _ollamaAdapter.Id, "Should have correct ID");
            Assert.AreEqual("Ollama LLM Adapter", _ollamaAdapter.Name, "Should have correct name");
            Assert.IsNotNull(_ollamaAdapter.Description, "Should have description");
            Assert.IsFalse(_ollamaAdapter.IsAvailable, "Should not be available initially");
        }
        
        [Test]
        public async Task ComfyUIAdapter_ShouldHaveCorrectProperties()
        {
            // Assert
            Assert.AreEqual("comfyui", _comfyuiAdapter.Id, "Should have correct ID");
            Assert.AreEqual("ComfyUI Texture Generation Adapter", _comfyuiAdapter.Name, "Should have correct name");
            Assert.IsNotNull(_comfyuiAdapter.Description, "Should have description");
            Assert.IsFalse(_comfyuiAdapter.IsAvailable, "Should not be available initially");
        }
        
        [Test]
        public async Task PiperAdapter_ShouldHaveCorrectProperties()
        {
            // Assert
            Assert.AreEqual("piper", _piperAdapter.Id, "Should have correct ID");
            Assert.AreEqual("Piper TTS Adapter", _piperAdapter.Name, "Should have correct name");
            Assert.IsNotNull(_piperAdapter.Description, "Should have description");
            Assert.IsFalse(_piperAdapter.IsAvailable, "Should not be available initially");
        }
        
        [Test]
        public async Task OllamaAdapter_ShouldPerformHealthCheck()
        {
            // Act
            var healthResult = await _ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(healthResult, "Should return health check result");
            Assert.IsNotNull(healthResult.Message, "Should have health check message");
            Assert.IsTrue(healthResult.ResponseTimeMs >= 0, "Should have valid response time");
            Assert.IsNotNull(healthResult.Details, "Should have health check details");
            Assert.IsNotNull(healthResult.Timestamp, "Should have timestamp");
        }
        
        [Test]
        public async Task ComfyUIAdapter_ShouldPerformHealthCheck()
        {
            // Act
            var healthResult = await _comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(healthResult, "Should return health check result");
            Assert.IsNotNull(healthResult.Message, "Should have health check message");
            Assert.IsTrue(healthResult.ResponseTimeMs >= 0, "Should have valid response time");
            Assert.IsNotNull(healthResult.Details, "Should have health check details");
            Assert.IsNotNull(healthResult.Timestamp, "Should have timestamp");
        }
        
        [Test]
        public async Task PiperAdapter_ShouldPerformHealthCheck()
        {
            // Act
            var healthResult = await _piperAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(healthResult, "Should return health check result");
            Assert.IsNotNull(healthResult.Message, "Should have health check message");
            Assert.IsTrue(healthResult.ResponseTimeMs >= 0, "Should have valid response time");
            Assert.IsNotNull(healthResult.Details, "Should have health check details");
            Assert.IsNotNull(healthResult.Timestamp, "Should have timestamp");
        }
        
        [Test]
        public async Task OllamaAdapter_ShouldInitialize()
        {
            // Act
            var initResult = await _ollamaAdapter.InitializeAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(initResult, "Should return initialization result");
            Assert.IsNotNull(initResult.Message, "Should have initialization message");
            Assert.IsNotNull(initResult.Details, "Should have initialization details");
            Assert.IsNotNull(initResult.Timestamp, "Should have timestamp");
        }
        
        [Test]
        public async Task ComfyUIAdapter_ShouldInitialize()
        {
            // Act
            var initResult = await _comfyuiAdapter.InitializeAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(initResult, "Should return initialization result");
            Assert.IsNotNull(initResult.Message, "Should have initialization message");
            Assert.IsNotNull(initResult.Details, "Should have initialization details");
            Assert.IsNotNull(initResult.Timestamp, "Should have timestamp");
        }
        
        [Test]
        public async Task PiperAdapter_ShouldInitialize()
        {
            // Act
            var initResult = await _piperAdapter.InitializeAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(initResult, "Should return initialization result");
            Assert.IsNotNull(initResult.Message, "Should have initialization message");
            Assert.IsNotNull(initResult.Details, "Should have initialization details");
            Assert.IsNotNull(initResult.Timestamp, "Should have timestamp");
        }
        
        [Test]
        public async Task OllamaAdapter_ShouldHandleUnreachableService()
        {
            // Arrange
            var unreachableAdapter = new OllamaAdapter("http://localhost:9999");
            
            // Act
            var healthResult = await unreachableAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(healthResult, "Should return health check result");
            Assert.IsFalse(healthResult.IsHealthy, "Should report unhealthy for unreachable service");
            Assert.IsNotNull(healthResult.Message, "Should have error message");
            Assert.IsTrue(healthResult.Message.Contains("unreachable") || healthResult.Message.Contains("timeout"), "Should indicate unreachable service");
        }
        
        [Test]
        public async Task ComfyUIAdapter_ShouldHandleUnreachableService()
        {
            // Arrange
            var unreachableAdapter = new ComfyUIAdapter("http://localhost:9999");
            
            // Act
            var healthResult = await unreachableAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(healthResult, "Should return health check result");
            Assert.IsFalse(healthResult.IsHealthy, "Should report unhealthy for unreachable service");
            Assert.IsNotNull(healthResult.Message, "Should have error message");
            Assert.IsTrue(healthResult.Message.Contains("unreachable") || healthResult.Message.Contains("timeout"), "Should indicate unreachable service");
        }
        
        [Test]
        public async Task PiperAdapter_ShouldHandleUnreachableService()
        {
            // Arrange
            var unreachableAdapter = new PiperAdapter("nonexistent-command");
            
            // Act
            var healthResult = await unreachableAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(healthResult, "Should return health check result");
            Assert.IsFalse(healthResult.IsHealthy, "Should report unhealthy for unreachable service");
            Assert.IsNotNull(healthResult.Message, "Should have error message");
            Assert.IsTrue(healthResult.Message.Contains("not available") || healthResult.Message.Contains("not found"), "Should indicate unreachable service");
        }
        
        [Test]
        public async Task OllamaAdapter_ShouldHandleCancellation()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _ollamaAdapter.HealthCheckAsync(cancellationTokenSource.Token);
            });
        }
        
        [Test]
        public async Task ComfyUIAdapter_ShouldHandleCancellation()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _comfyuiAdapter.HealthCheckAsync(cancellationTokenSource.Token);
            });
        }
        
        [Test]
        public async Task PiperAdapter_ShouldHandleCancellation()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _piperAdapter.HealthCheckAsync(cancellationTokenSource.Token);
            });
        }
        
        [Test]
        public async Task OllamaAdapter_ShouldUpdateAvailabilityStatus()
        {
            // Act
            var healthResult = await _ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.AreEqual(healthResult.IsHealthy, _ollamaAdapter.IsAvailable, "Availability should match health check result");
            Assert.IsTrue(_ollamaAdapter.LastHealthCheck <= DateTime.UtcNow, "Last health check should be recent");
        }
        
        [Test]
        public async Task ComfyUIAdapter_ShouldUpdateAvailabilityStatus()
        {
            // Act
            var healthResult = await _comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.AreEqual(healthResult.IsHealthy, _comfyuiAdapter.IsAvailable, "Availability should match health check result");
            Assert.IsTrue(_comfyuiAdapter.LastHealthCheck <= DateTime.UtcNow, "Last health check should be recent");
        }
        
        [Test]
        public async Task PiperAdapter_ShouldUpdateAvailabilityStatus()
        {
            // Act
            var healthResult = await _piperAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.AreEqual(healthResult.IsHealthy, _piperAdapter.IsAvailable, "Availability should match health check result");
            Assert.IsTrue(_piperAdapter.LastHealthCheck <= DateTime.UtcNow, "Last health check should be recent");
        }
        
        [Test]
        public async Task OllamaAdapter_ShouldHandleMultipleHealthChecks()
        {
            // Act
            var healthResult1 = await _ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            await Task.Delay(100); // Small delay
            var healthResult2 = await _ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(healthResult1, "First health check should succeed");
            Assert.IsNotNull(healthResult2, "Second health check should succeed");
            Assert.IsTrue(healthResult2.Timestamp >= healthResult1.Timestamp, "Second health check should be more recent");
        }
        
        [Test]
        public async Task ComfyUIAdapter_ShouldHandleMultipleHealthChecks()
        {
            // Act
            var healthResult1 = await _comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
            await Task.Delay(100); // Small delay
            var healthResult2 = await _comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(healthResult1, "First health check should succeed");
            Assert.IsNotNull(healthResult2, "Second health check should succeed");
            Assert.IsTrue(healthResult2.Timestamp >= healthResult1.Timestamp, "Second health check should be more recent");
        }
        
        [Test]
        public async Task PiperAdapter_ShouldHandleMultipleHealthChecks()
        {
            // Act
            var healthResult1 = await _piperAdapter.HealthCheckAsync(CancellationToken.None);
            await Task.Delay(100); // Small delay
            var healthResult2 = await _piperAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(healthResult1, "First health check should succeed");
            Assert.IsNotNull(healthResult2, "Second health check should succeed");
            Assert.IsTrue(healthResult2.Timestamp >= healthResult1.Timestamp, "Second health check should be more recent");
        }
        
        [Test]
        public async Task OllamaAdapter_ShouldHandleDisposal()
        {
            // Act
            _ollamaAdapter.Dispose();
            
            // Assert - Should not throw exception
            Assert.DoesNotThrow(() => _ollamaAdapter.Dispose(), "Multiple disposal should not throw");
        }
        
        [Test]
        public async Task ComfyUIAdapter_ShouldHandleDisposal()
        {
            // Act
            _comfyuiAdapter.Dispose();
            
            // Assert - Should not throw exception
            Assert.DoesNotThrow(() => _comfyuiAdapter.Dispose(), "Multiple disposal should not throw");
        }
        
        [Test]
        public async Task PiperAdapter_ShouldHandleDisposal()
        {
            // Act
            _piperAdapter.Dispose();
            
            // Assert - Should not throw exception
            Assert.DoesNotThrow(() => _piperAdapter.Dispose(), "Multiple disposal should not throw");
        }
    }
}
