using System;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command to create content bundle from interaction graph.
    /// </summary>
    public class CreateContentBundleCommand : ICreateContentBundleCommand
    {
        public async Task<ContentBundle> ExecuteAsync(ICreateContentBundleCommand.Input input, CancellationToken cancellationToken)
        {
            // Basic implementation - in a real scenario this would create content
            await Task.Delay(100, cancellationToken); // Simulate async work
            
            return new ContentBundle
            {
                Id = Guid.NewGuid().ToString(),
                InteractionGraphId = input.InteractionGraph.Id,
                Seed = input.InteractionGraph.Seed,
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
