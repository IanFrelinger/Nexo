using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NexoDirectorStudio.Adapters;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for adapter health and functionality validation
    /// </summary>
    public class AdapterValidationTests : IDisposable
    {
        private readonly IDirectorStudioService _service;
        
        public AdapterValidationTests()
        {
            _service = new DirectorStudioServiceUnified();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public async Task AdapterHealthChecks_ShouldWork()
        {
            // Act
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyuiAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();
            
            // Assert - Adapters should be available
            Assert.IsNotNull(ollamaAdapter, "Ollama adapter should be available");
            Assert.IsNotNull(comfyuiAdapter, "ComfyUI adapter should be available");
            Assert.IsNotNull(piperAdapter, "Piper adapter should be available");
            
            // Act - Health Checks
            var ollamaHealth = await ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            var comfyuiHealth = await comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
            var piperHealth = await piperAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert - Health Check Results
            Assert.IsNotNull(ollamaHealth, "Ollama health check should return result");
            Assert.IsNotNull(comfyuiHealth, "ComfyUI health check should return result");
            Assert.IsNotNull(piperHealth, "Piper health check should return result");
            
            Assert.IsNotNull(ollamaHealth.Message, "Ollama health check should have message");
            Assert.IsNotNull(comfyuiHealth.Message, "ComfyUI health check should have message");
            Assert.IsNotNull(piperHealth.Message, "Piper health check should have message");
        }
    }
}
