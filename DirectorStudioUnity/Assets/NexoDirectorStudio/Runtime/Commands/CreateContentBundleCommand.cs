using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
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
            // Generate FPS content bundle based on interaction graph and game plan
            await Task.Delay(100, cancellationToken); // Simulate async work
            
            var random = new System.Random(input.InteractionGraph.Seed);
            var assets = GenerateFPSAssets(input.InteractionGraph, input.GamePlan, random);
            var addressablesGroup = GenerateAddressablesGroup(input.InteractionGraph, random);
            var metadata = GenerateBundleMetadata(input.InteractionGraph, assets);
            
            return new ContentBundle
            {
                Id = Guid.NewGuid().ToString(),
                InteractionGraphId = input.InteractionGraph.Id,
                Assets = assets,
                AddressablesGroup = addressablesGroup,
                Metadata = metadata,
                Seed = input.InteractionGraph.Seed,
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }

        private List<AssetData> GenerateFPSAssets(InteractionGraph graph, GamePlan gamePlan, System.Random random)
        {
            var assets = new List<AssetData>();
            
            // Generate basic assets based on interaction nodes
            foreach (var node in graph.Nodes)
            {
                assets.Add(new AssetData
                {
                    Id = node.Id,
                    AssetType = "Prefab"
                });
            }
            
            return assets;
        }

        private AddressablesGroupData GenerateAddressablesGroup(InteractionGraph graph, System.Random random)
        {
            return new AddressablesGroupData
            {
                GroupName = $"FPS_Level_{graph.Id.Substring(0, 8)}"
            };
        }

        private BundleMetadata GenerateBundleMetadata(InteractionGraph graph, List<AssetData> assets)
        {
            return new BundleMetadata
            {
                Version = "1.0.0"
            };
        }
    }
}
