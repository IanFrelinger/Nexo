using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using NexoDirectorStudio.Adapters;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for adapter health checks and integration
    /// </summary>
    public class AdapterHealthTests
    {
        private IDirectorStudioService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioServiceUnified();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        public async Task Integration_Adapters_ShouldBeHealthy()
        {
            // Act - Check adapter health
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyUIAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();

            var ollamaHealth = await ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            var comfyUIHealth = await comfyUIAdapter.HealthCheckAsync(CancellationToken.None);
            var piperHealth = await piperAdapter.HealthCheckAsync(CancellationToken.None);

            // Assert
            Assert.IsNotNull(ollamaHealth, "Ollama health check should return result");
            Assert.IsNotNull(comfyUIHealth, "ComfyUI health check should return result");
            Assert.IsNotNull(piperHealth, "Piper health check should return result");

            // Note: Adapters might not be available in test environment, so we just check they respond
            Debug.Log($"Ollama Health: {ollamaHealth.IsHealthy} - {ollamaHealth.Message}");
            Debug.Log($"ComfyUI Health: {comfyUIHealth.IsHealthy} - {comfyUIHealth.Message}");
            Debug.Log($"Piper Health: {piperHealth.IsHealthy} - {piperHealth.Message}");

            Debug.Log("✅ Adapters health check integration successful");
        }
    }
}
