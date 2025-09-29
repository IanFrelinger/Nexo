using NUnit.Framework;
using System.IO;
using System.Text.Json;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Integration tests to verify content reference resolution and Addressables group assignment.
    /// </summary>
    [TestFixture]
    public class Integration_ContentRefs
    {
        private InteractionGraph _testGraph;
        private ContentBundle _testBundle;
        
        [SetUp]
        public void SetUp()
        {
            // Create a test interaction graph
            _testGraph = CreateTestInteractionGraph();
            
            // Create content bundle from the graph
            var command = new CreateContentBundleCommand();
            _testBundle = command.ExecuteAsync(_testGraph, CancellationToken.None).Result;
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test files
            CleanupTestFiles();
        }
        
        [Test]
        public void AllAssetReferences_ShouldResolve()
        {
            // Arrange
            var assets = _testBundle.Assets;
            
            // Act & Assert - All assets should have valid references
            foreach (var asset in assets)
            {
                Assert.IsNotNull(asset.Name, $"Asset {asset.Name} should have a name");
                Assert.IsNotNull(asset.AssetPath, $"Asset {asset.Name} should have a path");
                Assert.IsNotNull(asset.AssetDataJson, $"Asset {asset.Name} should have data");
                
                // Validate JSON data
                Assert.DoesNotThrow(() =>
                {
                    JsonDocument.Parse(asset.AssetDataJson);
                }, $"Asset {asset.Name} should have valid JSON data");
            }
        }
        
        [Test]
        public void AssetDependencies_ShouldBeValid()
        {
            // Arrange
            var assets = _testBundle.Assets;
            var assetNames = assets.Select(a => a.Name).ToHashSet();
            
            // Act & Assert - All dependencies should reference existing assets
            foreach (var asset in assets)
            {
                foreach (var dependency in asset.Dependencies)
                {
                    Assert.IsTrue(assetNames.Contains(dependency), 
                        $"Asset {asset.Name} dependency {dependency} should reference an existing asset");
                }
            }
        }
        
        [Test]
        public void AddressablesGroup_ShouldBePresent()
        {
            // Arrange
            var addressablesGroup = _testBundle.AddressablesGroup;
            
            // Assert
            Assert.IsNotNull(addressablesGroup, "Addressables group should be present");
            Assert.AreEqual("Generated", addressablesGroup.GroupName, "Group name should be 'Generated'");
            Assert.IsNotNull(addressablesGroup.BuildSettings, "Build settings should be present");
            Assert.IsNotNull(addressablesGroup.Labels, "Labels should be present");
        }
        
        [Test]
        public void AddressablesGroup_ShouldHaveValidBuildSettings()
        {
            // Arrange
            var buildSettings = _testBundle.AddressablesGroup.BuildSettings;
            
            // Assert
            Assert.IsNotNull(buildSettings.BuildTarget, "Build target should be specified");
            Assert.IsNotNull(buildSettings.CompressionType, "Compression type should be specified");
            Assert.IsFalse(buildSettings.UseRemoteCatalog, "Remote catalog should be disabled for local testing");
        }
        
        [Test]
        public void AddressablesGroup_ShouldHaveValidLabels()
        {
            // Arrange
            var labels = _testBundle.AddressablesGroup.Labels;
            
            // Assert
            Assert.IsNotNull(labels, "Labels should not be null");
            Assert.IsTrue(labels.Length > 0, "Should have at least one label");
            Assert.Contains("Generated", labels, "Should contain 'Generated' label");
            Assert.Contains("DirectorStudio", labels, "Should contain 'DirectorStudio' label");
        }
        
        [Test]
        public void AddressableAssets_ShouldHaveValidKeys()
        {
            // Arrange
            var addressableAssets = _testBundle.Assets.Where(a => a.IncludeInAddressables).ToList();
            
            // Assert
            Assert.IsTrue(addressableAssets.Count > 0, "Should have at least one addressable asset");
            
            foreach (var asset in addressableAssets)
            {
                Assert.IsNotNull(asset.AddressablesKey, $"Addressable asset {asset.Name} should have a key");
                Assert.IsFalse(string.IsNullOrWhiteSpace(asset.AddressablesKey), 
                    $"Addressable asset {asset.Name} should have a non-empty key");
            }
        }
        
        [Test]
        public void BundleMetadata_ShouldBeValid()
        {
            // Arrange
            var metadata = _testBundle.Metadata;
            
            // Assert
            Assert.IsNotNull(metadata, "Bundle metadata should be present");
            Assert.IsNotNull(metadata.Version, "Version should be specified");
            Assert.IsNotNull(metadata.Author, "Author should be specified");
            Assert.IsNotNull(metadata.Description, "Description should be specified");
            Assert.IsTrue(metadata.SizeBytes > 0, "Size should be greater than 0");
            Assert.IsNotNull(metadata.ContentHash, "Content hash should be present");
        }
        
        [Test]
        public void BundleMetadata_ShouldHaveValidTags()
        {
            // Arrange
            var tags = _testBundle.Metadata.Tags;
            
            // Assert
            Assert.IsNotNull(tags, "Tags should not be null");
            Assert.IsTrue(tags.Length > 0, "Should have at least one tag");
            Assert.Contains("Generated", tags, "Should contain 'Generated' tag");
            Assert.Contains("DirectorStudio", tags, "Should contain 'DirectorStudio' tag");
        }
        
        [Test]
        public void ScriptableObjectAssets_ShouldHaveValidData()
        {
            // Arrange
            var scriptableObjects = _testBundle.Assets.Where(a => a.AssetType == "ScriptableObject").ToList();
            
            // Assert
            Assert.IsTrue(scriptableObjects.Count > 0, "Should have at least one ScriptableObject asset");
            
            foreach (var so in scriptableObjects)
            {
                Assert.IsNotNull(so.AssetDataJson, $"ScriptableObject {so.Name} should have data");
                
                // Validate JSON structure
                var jsonDoc = JsonDocument.Parse(so.AssetDataJson);
                var root = jsonDoc.RootElement;
                
                Assert.IsTrue(root.TryGetProperty("NodeId", out _), $"ScriptableObject {so.Name} should have NodeId");
                Assert.IsTrue(root.TryGetProperty("NodeType", out _), $"ScriptableObject {so.Name} should have NodeType");
                Assert.IsTrue(root.TryGetProperty("Name", out _), $"ScriptableObject {so.Name} should have Name");
            }
        }
        
        [Test]
        public void PrefabAssets_ShouldHaveValidData()
        {
            // Arrange
            var prefabs = _testBundle.Assets.Where(a => a.AssetType == "Prefab").ToList();
            
            // Assert
            Assert.IsTrue(prefabs.Count > 0, "Should have at least one Prefab asset");
            
            foreach (var prefab in prefabs)
            {
                Assert.IsNotNull(prefab.AssetDataJson, $"Prefab {prefab.Name} should have data");
                
                // Validate JSON structure
                var jsonDoc = JsonDocument.Parse(prefab.AssetDataJson);
                var root = jsonDoc.RootElement;
                
                Assert.IsTrue(root.TryGetProperty("Name", out _), $"Prefab {prefab.Name} should have Name");
                Assert.IsTrue(root.TryGetProperty("Components", out _), $"Prefab {prefab.Name} should have Components");
            }
        }
        
        [Test]
        public void MaterialAssets_ShouldHaveValidData()
        {
            // Arrange
            var materials = _testBundle.Assets.Where(a => a.AssetType == "Material").ToList();
            
            // Assert
            Assert.IsTrue(materials.Count > 0, "Should have at least one Material asset");
            
            foreach (var material in materials)
            {
                Assert.IsNotNull(material.AssetDataJson, $"Material {material.Name} should have data");
                
                // Validate JSON structure
                var jsonDoc = JsonDocument.Parse(material.AssetDataJson);
                var root = jsonDoc.RootElement;
                
                Assert.IsTrue(root.TryGetProperty("Name", out _), $"Material {material.Name} should have Name");
                Assert.IsTrue(root.TryGetProperty("Shader", out _), $"Material {material.Name} should have Shader");
                Assert.IsTrue(root.TryGetProperty("Properties", out _), $"Material {material.Name} should have Properties");
            }
        }
        
        [Test]
        public void AudioAssets_ShouldHaveValidData()
        {
            // Arrange
            var audioAssets = _testBundle.Assets.Where(a => a.AssetType == "Audio").ToList();
            
            // Assert
            Assert.IsTrue(audioAssets.Count > 0, "Should have at least one Audio asset");
            
            foreach (var audio in audioAssets)
            {
                Assert.IsNotNull(audio.AssetDataJson, $"Audio {audio.Name} should have data");
                
                // Validate JSON structure
                var jsonDoc = JsonDocument.Parse(audio.AssetDataJson);
                var root = jsonDoc.RootElement;
                
                Assert.IsTrue(root.TryGetProperty("Name", out _), $"Audio {audio.Name} should have Name");
                Assert.IsTrue(root.TryGetProperty("ClipType", out _), $"Audio {audio.Name} should have ClipType");
            }
        }
        
        [Test]
        public void AssetImportSettings_ShouldBeValid()
        {
            // Arrange
            var assets = _testBundle.Assets;
            
            // Assert
            foreach (var asset in assets)
            {
                Assert.IsNotNull(asset.ImportSettings, $"Asset {asset.Name} should have import settings");
                
                // Validate import settings based on asset type
                switch (asset.AssetType)
                {
                    case "Audio":
                        Assert.IsNotNull(asset.ImportSettings.AudioSettings, 
                            $"Audio asset {asset.Name} should have audio import settings");
                        break;
                    case "Material":
                        // Materials don't need special import settings
                        break;
                    case "Prefab":
                        // Prefabs should have collider and lightmap settings
                        break;
                }
            }
        }
        
        private static InteractionGraph CreateTestInteractionGraph()
        {
            return new InteractionGraph
            {
                Id = "test-graph-1",
                WorldLayoutId = "test-layout-1",
                Nodes = new[]
                {
                    new InteractionNode
                    {
                        Id = "spawn-1",
                        NodeType = "Spawn",
                        WorldPosition = new Vector3(1, 1, 1),
                        Name = "Player Spawn",
                        Description = "Spawns the player",
                        Actions = Array.Empty<InteractionAction>(),
                        IsRepeatable = false,
                        Priority = 10
                    },
                    new InteractionNode
                    {
                        Id = "collectible-1",
                        NodeType = "Collectible",
                        WorldPosition = new Vector3(5, 1, 5),
                        Name = "Collectible 1",
                        Description = "A collectible item",
                        Actions = Array.Empty<InteractionAction>(),
                        IsRepeatable = false,
                        Priority = 3
                    }
                },
                Connections = Array.Empty<InteractionConnection>(),
                Variables = Array.Empty<InteractionVariable>(),
                EntryPointIds = new[] { "spawn-1" },
                Seed = 12345
            };
        }
        
        private static void CleanupTestFiles()
        {
            try
            {
                // Clean up any test files that might have been created
                if (Directory.Exists("Assets/Generated"))
                {
                    Directory.Delete("Assets/Generated", true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
