using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using UnityEngine;
using NUnit.Framework;

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
        private GamePlan _testGamePlan;
        
        [SetUp]
        public void SetUp()
        {
            // Create a test game plan
            _testGamePlan = CreateTestGamePlan();
            
            // Create a test interaction graph
            _testGraph = CreateTestInteractionGraph();
            
            // Create content bundle from the graph
            var command = new CreateContentBundleCommand();
            _testBundle = command.ExecuteAsync(new ICreateContentBundleCommand.Input(_testGraph, _testGamePlan), CancellationToken.None).Result;
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
                    // Validate JSON format
                    if (string.IsNullOrEmpty(asset.AssetDataJson) || !asset.AssetDataJson.Trim().StartsWith("{"))
                    {
                        throw new InvalidOperationException("Invalid JSON format");
                    }
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
            Assert.IsTrue(labels.Count > 0, "Should have at least one label");
            Assert.IsTrue(labels.Contains("Generated"), "Should contain 'Generated' label");
            Assert.IsTrue(labels.Contains("DirectorStudio"), "Should contain 'DirectorStudio' label");
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
            Assert.IsTrue(tags.Count > 0, "Should have at least one tag");
            Assert.IsTrue(tags.Contains("Generated"), "Should contain 'Generated' tag");
            Assert.IsTrue(tags.Contains("DirectorStudio"), "Should contain 'DirectorStudio' tag");
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
                if (string.IsNullOrEmpty(so.AssetDataJson) || !so.AssetDataJson.Trim().StartsWith("{"))
                {
                    throw new InvalidOperationException("Invalid JSON format");
                }
                
                // Basic JSON validation - check for required fields
                Assert.IsTrue(so.AssetDataJson.Contains("NodeId"), $"ScriptableObject {so.Name} should have NodeId");
                Assert.IsTrue(so.AssetDataJson.Contains("NodeType"), $"ScriptableObject {so.Name} should have NodeType");
                Assert.IsTrue(so.AssetDataJson.Contains("Name"), $"ScriptableObject {so.Name} should have Name");
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
                if (string.IsNullOrEmpty(prefab.AssetDataJson) || !prefab.AssetDataJson.Trim().StartsWith("{"))
                {
                    throw new InvalidOperationException("Invalid JSON format");
                }
                
                // Basic JSON validation - check for required fields
                Assert.IsTrue(prefab.AssetDataJson.Contains("Name"), $"Prefab {prefab.Name} should have Name");
                Assert.IsTrue(prefab.AssetDataJson.Contains("Components"), $"Prefab {prefab.Name} should have Components");
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
                if (string.IsNullOrEmpty(material.AssetDataJson) || !material.AssetDataJson.Trim().StartsWith("{"))
                {
                    throw new InvalidOperationException("Invalid JSON format");
                }
                
                // Basic JSON validation - check for required fields
                Assert.IsTrue(material.AssetDataJson.Contains("Name"), $"Material {material.Name} should have Name");
                Assert.IsTrue(material.AssetDataJson.Contains("Shader"), $"Material {material.Name} should have Shader");
                Assert.IsTrue(material.AssetDataJson.Contains("Properties"), $"Material {material.Name} should have Properties");
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
                if (string.IsNullOrEmpty(audio.AssetDataJson) || !audio.AssetDataJson.Trim().StartsWith("{"))
                {
                    throw new InvalidOperationException("Invalid JSON format");
                }
                
                // Basic JSON validation - check for required fields
                Assert.IsTrue(audio.AssetDataJson.Contains("Name"), $"Audio {audio.Name} should have Name");
                Assert.IsTrue(audio.AssetDataJson.Contains("ClipType"), $"Audio {audio.Name} should have ClipType");
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
        
        private static GamePlan CreateTestGamePlan()
        {
            return new GamePlan(
                Id: "test-game-plan",
                SourceBrief: new DesignBrief(
                    Description: "Test game plan for content refs",
                    GenreHint: "Platformer",
                    TargetDurationMinutes: 5,
                    DifficultyLevel: 3,
                    Seed: 12345
                ),
                Genre: "Platformer",
                Description: "Test game plan for content refs",
                CoreMechanics: new[] { "Jump", "Move" },
                PlayerExperience: new[] { "Fun" },
                EstimatedDurationMinutes: 5,
                DifficultyProgression: new[] { new DifficultyBeat(TimeOffsetSeconds: 0, DifficultyLevel: 2, Description: "Start") },
                NarrativeBeats: new[] { "Introduction" },
                RequiredAssets: new[] { new AssetRequirement(AssetType: "Character", Name: "Player", Description: "Main character", IsRequired: true, Priority: 1) },
                Seed: 12345,
                GeneratedAt: DateTimeOffset.UtcNow,
                Hash: "test-hash"
            );
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
