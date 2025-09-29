using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Policies;
using System.Text.Json;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Implementation of the create content bundle command.
    /// Creates ScriptableObjects, prefabs, and other Unity assets from an interaction graph.
    /// </summary>
    public sealed class CreateContentBundleCommand : ICreateContentBundleCommand
    {
        public async ValueTask<ContentBundle> ExecuteAsync(InteractionGraph input, CancellationToken ct)
        {
            // TODO: Replace with real content generation in later phases
            // For now, create a deterministic content bundle based on the interaction graph
            
            await Task.Delay(100, ct); // Simulate processing time
            
            var random = new Random(input.Seed);
            
            // Generate assets based on interaction graph
            var assets = GenerateAssets(input, random);
            
            // Generate Addressables group configuration
            var addressablesGroup = GenerateAddressablesGroup(input, random);
            
            // Generate bundle metadata
            var metadata = GenerateBundleMetadata(input, assets);
            
            var bundle = new ContentBundle
            {
                InteractionGraphId = input.Id,
                Assets = assets,
                AddressablesGroup = addressablesGroup,
                Metadata = metadata,
                Seed = input.Seed
            };
            
            return bundle;
        }
        
        private static IReadOnlyList<AssetData> GenerateAssets(InteractionGraph graph, Random random)
        {
            var assets = new List<AssetData>();
            
            // Generate ScriptableObject assets for interaction data
            foreach (var node in graph.Nodes)
            {
                if (ShouldCreateScriptableObject(node))
                {
                    var asset = CreateScriptableObjectAsset(node, random);
                    assets.Add(asset);
                }
            }
            
            // Generate prefab assets
            var prefabAssets = GeneratePrefabAssets(graph, random);
            assets.AddRange(prefabAssets);
            
            // Generate material assets
            var materialAssets = GenerateMaterialAssets(graph, random);
            assets.AddRange(materialAssets);
            
            // Generate audio assets
            var audioAssets = GenerateAudioAssets(graph, random);
            assets.AddRange(audioAssets);
            
            return assets;
        }
        
        private static bool ShouldCreateScriptableObject(InteractionNode node)
        {
            return node.NodeType switch
            {
                "Quest" => true,
                "Dialog" => true,
                "Collectible" => true,
                "EnemySpawn" => true,
                _ => false
            };
        }
        
        private static AssetData CreateScriptableObjectAsset(InteractionNode node, Random random)
        {
            var assetName = $"{node.NodeType}_{node.Name.Replace(" ", "")}";
            var assetPath = $"ScriptableObjects/{assetName}.asset";
            
            // Create ScriptableObject data
            var soData = new
            {
                NodeId = node.Id,
                NodeType = node.NodeType,
                Name = node.Name,
                Description = node.Description,
                WorldPosition = node.WorldPosition,
                Conditions = node.Conditions,
                Actions = node.Actions,
                IsRepeatable = node.IsRepeatable,
                Priority = node.Priority,
                Properties = node.Properties
            };
            
            var jsonData = JsonSerializer.Serialize(soData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            return new AssetData
            {
                AssetType = "ScriptableObject",
                Name = assetName,
                AssetPath = assetPath,
                AssetDataJson = jsonData,
                Dependencies = Array.Empty<string>(),
                ImportSettings = new AssetImportSettings(),
                IncludeInAddressables = true,
                AddressablesKey = $"SO_{assetName}"
            };
        }
        
        private static IReadOnlyList<AssetData> GeneratePrefabAssets(InteractionGraph graph, Random random)
        {
            var assets = new List<AssetData>();
            
            // Generate player prefab
            assets.Add(new AssetData
            {
                AssetType = "Prefab",
                Name = "Player",
                AssetPath = "Prefabs/Player.prefab",
                AssetDataJson = GeneratePlayerPrefabData(random),
                Dependencies = new[] { "Materials/PlayerMaterial" },
                ImportSettings = new AssetImportSettings
                {
                    GenerateColliders = true,
                    GenerateLightmapUVs = false
                },
                IncludeInAddressables = true,
                AddressablesKey = "Player"
            });
            
            // Generate collectible prefabs
            var collectibleNodes = graph.Nodes.Where(n => n.NodeType == "Collectible").ToList();
            for (int i = 0; i < collectibleNodes.Count; i++)
            {
                var node = collectibleNodes[i];
                assets.Add(new AssetData
                {
                    AssetType = "Prefab",
                    Name = $"Collectible_{i + 1}",
                    AssetPath = $"Prefabs/Collectible_{i + 1}.prefab",
                    AssetDataJson = GenerateCollectiblePrefabData(node, random),
                    Dependencies = new[] { "Materials/CollectibleMaterial" },
                    ImportSettings = new AssetImportSettings
                    {
                        GenerateColliders = true,
                        GenerateLightmapUVs = false
                    },
                    IncludeInAddressables = true,
                    AddressablesKey = $"Collectible_{i + 1}"
                });
            }
            
            // Generate enemy prefabs
            var enemyNodes = graph.Nodes.Where(n => n.NodeType == "EnemySpawn").ToList();
            for (int i = 0; i < enemyNodes.Count; i++)
            {
                var node = enemyNodes[i];
                assets.Add(new AssetData
                {
                    AssetType = "Prefab",
                    Name = $"Enemy_{i + 1}",
                    AssetPath = $"Prefabs/Enemy_{i + 1}.prefab",
                    AssetDataJson = GenerateEnemyPrefabData(node, random),
                    Dependencies = new[] { "Materials/EnemyMaterial" },
                    ImportSettings = new AssetImportSettings
                    {
                        GenerateColliders = true,
                        GenerateLightmapUVs = false
                    },
                    IncludeInAddressables = true,
                    AddressablesKey = $"Enemy_{i + 1}"
                });
            }
            
            return assets;
        }
        
        private static IReadOnlyList<AssetData> GenerateMaterialAssets(InteractionGraph graph, Random random)
        {
            var assets = new List<AssetData>();
            
            // Generate player material
            assets.Add(new AssetData
            {
                AssetType = "Material",
                Name = "PlayerMaterial",
                AssetPath = "Materials/PlayerMaterial.mat",
                AssetDataJson = GenerateMaterialData("Player", random),
                Dependencies = new[] { "Textures/PlayerTexture" },
                ImportSettings = new AssetImportSettings(),
                IncludeInAddressables = false,
                AddressablesKey = string.Empty
            });
            
            // Generate collectible material
            assets.Add(new AssetData
            {
                AssetType = "Material",
                Name = "CollectibleMaterial",
                AssetPath = "Materials/CollectibleMaterial.mat",
                AssetDataJson = GenerateMaterialData("Collectible", random),
                Dependencies = new[] { "Textures/CollectibleTexture" },
                ImportSettings = new AssetImportSettings(),
                IncludeInAddressables = false,
                AddressablesKey = string.Empty
            });
            
            // Generate enemy material
            assets.Add(new AssetData
            {
                AssetType = "Material",
                Name = "EnemyMaterial",
                AssetPath = "Materials/EnemyMaterial.mat",
                AssetDataJson = GenerateMaterialData("Enemy", random),
                Dependencies = new[] { "Textures/EnemyTexture" },
                ImportSettings = new AssetImportSettings(),
                IncludeInAddressables = false,
                AddressablesKey = string.Empty
            });
            
            return assets;
        }
        
        private static IReadOnlyList<AssetData> GenerateAudioAssets(InteractionGraph graph, Random random)
        {
            var assets = new List<AssetData>();
            
            // Generate collectible sound
            assets.Add(new AssetData
            {
                AssetType = "Audio",
                Name = "CollectibleSound",
                AssetPath = "Audio/CollectibleSound.wav",
                AssetDataJson = GenerateAudioData("Collectible", random),
                Dependencies = Array.Empty<string>(),
                ImportSettings = new AssetImportSettings
                {
                    AudioSettings = new AudioImportSettings
                    {
                        AudioFormat = "Compressed",
                        CompressionFormat = "Vorbis",
                        Quality = 0.7f,
                        LoadOnStartup = false
                    }
                },
                IncludeInAddressables = true,
                AddressablesKey = "CollectibleSound"
            });
            
            // Generate environmental sound
            assets.Add(new AssetData
            {
                AssetType = "Audio",
                Name = "EnvironmentalSound",
                AssetPath = "Audio/EnvironmentalSound.wav",
                AssetDataJson = GenerateAudioData("Environmental", random),
                Dependencies = Array.Empty<string>(),
                ImportSettings = new AssetImportSettings
                {
                    AudioSettings = new AudioImportSettings
                    {
                        AudioFormat = "Compressed",
                        CompressionFormat = "Vorbis",
                        Quality = 0.5f,
                        LoadOnStartup = false
                    }
                },
                IncludeInAddressables = true,
                AddressablesKey = "EnvironmentalSound"
            });
            
            return assets;
        }
        
        private static AddressablesGroupData GenerateAddressablesGroup(InteractionGraph graph, Random random)
        {
            return new AddressablesGroupData
            {
                GroupName = "Generated",
                BuildSettings = new AddressablesBuildSettings
                {
                    BuildTarget = "StandaloneWindows64",
                    CompressionType = "LZ4",
                    UseRemoteCatalog = false,
                    RemoteCatalogUrl = string.Empty
                },
                Labels = new[] { "Generated", "DirectorStudio", "GameSlice" }
            };
        }
        
        private static BundleMetadata GenerateBundleMetadata(InteractionGraph graph, IReadOnlyList<AssetData> assets)
        {
            var totalSize = assets.Sum(a => a.AssetDataJson.Length);
            var contentHash = GenerateContentHash(assets);
            
            return new BundleMetadata
            {
                Version = "1.0.0",
                Author = "Director Studio",
                Description = $"Generated content bundle for interaction graph {graph.Id}",
                Tags = new[] { "Generated", "DirectorStudio", "GameSlice" },
                SizeBytes = totalSize,
                ContentHash = contentHash
            };
        }
        
        private static string GenerateContentHash(IReadOnlyList<AssetData> assets)
        {
            var content = string.Join("|", assets.Select(a => $"{a.Name}:{a.AssetDataJson.Length}"));
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));
        }
        
        private static string GeneratePlayerPrefabData(Random random)
        {
            return JsonSerializer.Serialize(new
            {
                Name = "Player",
                Components = new[]
                {
                    new { Type = "Transform", Position = new { x = 0, y = 0, z = 0 } },
                    new { Type = "Rigidbody", Mass = 1.0f, Drag = 0.0f },
                    new { Type = "CapsuleCollider", Height = 2.0f, Radius = 0.5f },
                    new { Type = "PlayerController", Speed = 5.0f, JumpHeight = 2.0f }
                },
                Children = new object[0]
            });
        }
        
        private static string GenerateCollectiblePrefabData(InteractionNode node, Random random)
        {
            return JsonSerializer.Serialize(new
            {
                Name = $"Collectible_{node.Name}",
                Components = new[]
                {
                    new { Type = "Transform", Position = new { x = node.WorldPosition.x, y = node.WorldPosition.y, z = node.WorldPosition.z } },
                    new { Type = "SphereCollider", Radius = 0.5f, IsTrigger = true },
                    new { Type = "CollectibleController", Value = 10, CollectibleType = "Coin" }
                },
                Children = new object[0]
            });
        }
        
        private static string GenerateEnemyPrefabData(InteractionNode node, Random random)
        {
            return JsonSerializer.Serialize(new
            {
                Name = $"Enemy_{node.Name}",
                Components = new[]
                {
                    new { Type = "Transform", Position = new { x = node.WorldPosition.x, y = node.WorldPosition.y, z = node.WorldPosition.z } },
                    new { Type = "Rigidbody", Mass = 1.0f, Drag = 0.5f },
                    new { Type = "CapsuleCollider", Height = 2.0f, Radius = 0.5f },
                    new { Type = "EnemyController", Health = 100, Damage = 10, Speed = 3.0f }
                },
                Children = new object[0]
            });
        }
        
        private static string GenerateMaterialData(string materialType, Random random)
        {
            return JsonSerializer.Serialize(new
            {
                Name = $"{materialType}Material",
                Shader = "Standard",
                Properties = new Dictionary<string, object>
                {
                    ["_Color"] = new { r = 0.5f + (float)random.NextDouble() * 0.5f, g = 0.5f + (float)random.NextDouble() * 0.5f, b = 0.5f + (float)random.NextDouble() * 0.5f, a = 1.0f },
                    ["_Metallic"] = 0.0f,
                    ["_Smoothness"] = 0.5f + (float)random.NextDouble() * 0.5f
                }
            });
        }
        
        private static string GenerateAudioData(string audioType, Random random)
        {
            return JsonSerializer.Serialize(new
            {
                Name = $"{audioType}Sound",
                ClipType = "AudioClip",
                Length = 1.0f + (float)random.NextDouble() * 2.0f,
                Frequency = 44100,
                Channels = 1,
                Format = "PCM"
            });
        }
    }
}
