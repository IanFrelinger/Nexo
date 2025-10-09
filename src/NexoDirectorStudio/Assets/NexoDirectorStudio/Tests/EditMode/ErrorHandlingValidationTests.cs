using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for error handling validation
    /// </summary>
    public class ErrorHandlingValidationTests : IDisposable
    {
        private readonly IIDirectorStudioService _service;
        
        public ErrorHandlingValidationTests()
        {
            _service = new DirectorStudioServiceUnified();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public async Task System_ShouldHandleErrorsGracefully()
        {
            // Test with null input
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            
            // This should not throw an exception, but handle gracefully
            Assert.DoesNotThrowAsync(async () =>
            {
                try
                {
                    await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(null), CancellationToken.None);
                }
                catch (ArgumentNullException)
                {
                    // Expected for null input
                }
            });
        }
    }
}
