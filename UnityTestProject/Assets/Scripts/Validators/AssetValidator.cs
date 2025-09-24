using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Nexo.Agent.Contracts;

namespace NexoDoomGame.Validators
{
    public class AssetValidator
    {
        private readonly ITaskExecutionAgent _nexoAgent;

        public AssetValidator(ITaskExecutionAgent nexoAgent)
        {
            _nexoAgent = nexoAgent ?? throw new ArgumentNullException(nameof(nexoAgent));
        }

        public async Task<ValidationResult> ValidateAssetsAsync(float threshold)
        {
            Debug.Log("🔍 Validating asset quality...");
            
            try
            {
                var assets = FindAllAssets();
                var issues = new List<ValidationIssue>();
                var totalScore = 0f;
                var maxScore = assets.Count * 100f;

                foreach (var asset in assets)
                {
                    var assetScore = await ValidateSingleAsset(asset);
                    totalScore += assetScore;
                    
                    if (assetScore < threshold * 100f)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = "Asset Quality",
                            Description = $"Asset {asset.name} has quality issues",
                            Severity = assetScore < 50f ? "High" : "Medium",
                            Asset = asset
                        });
                    }
                }

                var finalScore = maxScore > 0 ? totalScore / maxScore : 0f;
                
                return new ValidationResult
                {
                    Phase = "Asset Quality",
                    Score = finalScore,
                    Threshold = threshold,
                    Passed = finalScore >= threshold,
                    Issues = issues,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Asset validation error: {ex.Message}");
                return new ValidationResult
                {
                    Phase = "Asset Quality",
                    Score = 0f,
                    Threshold = threshold,
                    Passed = false,
                    Issues = new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            Type = "Asset Validation Error",
                            Description = ex.Message,
                            Severity = "High"
                        }
                    },
                    Timestamp = DateTime.Now
                };
            }
        }

        public async Task ImproveAssetsAsync(ValidationResult result)
        {
            Debug.Log($"🔄 Improving assets based on validation results...");
            
            try
            {
                var improvementTasks = result.Issues
                    .Where(issue => issue.Asset != null)
                    .Select(issue => ImproveSingleAsset(issue.Asset, issue))
                    .ToArray();

                await Task.WhenAll(improvementTasks);
                
                Debug.Log("✅ Asset improvements completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Asset improvement error: {ex.Message}");
            }
        }

        private List<UnityEngine.Object> FindAllAssets()
        {
            var assets = new List<UnityEngine.Object>();
            
            // Find textures
            assets.AddRange(Resources.FindObjectsOfTypeAll<Texture2D>());
            
            // Find materials
            assets.AddRange(Resources.FindObjectsOfTypeAll<Material>());
            
            // Find meshes
            assets.AddRange(Resources.FindObjectsOfTypeAll<Mesh>());
            
            // Find audio clips
            assets.AddRange(Resources.FindObjectsOfTypeAll<AudioClip>());
            
            return assets;
        }

        private async Task<float> ValidateSingleAsset(UnityEngine.Object asset)
        {
            await Task.Delay(50); // Simulate validation time
            
            var score = 100f;
            
            if (asset == null) return 0f;
            
            // Check asset type specific quality
            switch (asset)
            {
                case Texture2D texture:
                    score = ValidateTexture(texture);
                    break;
                case Material material:
                    score = ValidateMaterial(material);
                    break;
                case Mesh mesh:
                    score = ValidateMesh(mesh);
                    break;
                case AudioClip audioClip:
                    score = ValidateAudioClip(audioClip);
                    break;
                default:
                    score = 80f; // Default score for unknown asset types
                    break;
            }
            
            return Math.Max(0f, score);
        }

        private float ValidateTexture(Texture2D texture)
        {
            var score = 100f;
            
            // Check texture size (power of 2 is preferred)
            if (!IsPowerOfTwo(texture.width) || !IsPowerOfTwo(texture.height))
            {
                score -= 20f;
            }
            
            // Check texture format
            if (texture.format != TextureFormat.RGBA32 && texture.format != TextureFormat.RGB24)
            {
                score -= 10f;
            }
            
            // Check texture compression
            if (texture.compressionQuality != 100)
            {
                score -= 5f;
            }
            
            return score;
        }

        private float ValidateMaterial(Material material)
        {
            var score = 100f;
            
            // Check for missing shader
            if (material.shader == null)
            {
                score -= 30f;
            }
            
            // Check for unused properties
            var shader = material.shader;
            if (shader != null)
            {
                var propertyCount = ShaderUtil.GetPropertyCount(shader);
                var usedProperties = 0;
                
                for (int i = 0; i < propertyCount; i++)
                {
                    if (material.HasProperty(ShaderUtil.GetPropertyName(shader, i)))
                    {
                        usedProperties++;
                    }
                }
                
                if (usedProperties < propertyCount * 0.5f)
                {
                    score -= 15f;
                }
            }
            
            return score;
        }

        private float ValidateMesh(Mesh mesh)
        {
            var score = 100f;
            
            // Check triangle count
            if (mesh.triangles.Length > 10000)
            {
                score -= 20f;
            }
            
            // Check for missing normals
            if (mesh.normals.Length == 0)
            {
                score -= 25f;
            }
            
            // Check for missing UVs
            if (mesh.uv.Length == 0)
            {
                score -= 15f;
            }
            
            return score;
        }

        private float ValidateAudioClip(AudioClip audioClip)
        {
            var score = 100f;
            
            // Check audio length
            if (audioClip.length > 60f)
            {
                score -= 10f;
            }
            
            // Check audio frequency
            if (audioClip.frequency < 22050)
            {
                score -= 20f;
            }
            
            return score;
        }

        private bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }

        private async Task ImproveSingleAsset(UnityEngine.Object asset, ValidationIssue issue)
        {
            Debug.Log($"🔧 Improving asset: {asset.name}");
            
            try
            {
                // Create improvement task for Nexo Agent
                var improvementTask = new Nexo.Agent.Models.Task
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Improve Asset: {asset.name}",
                    Description = $"Fix quality issues in {asset.name}: {issue.Description}",
                    Priority = issue.Severity == "High" ? 1 : 2,
                    Status = "Pending"
                };
                
                await _nexoAgent.ExecuteTaskAsync(improvementTask);
                
                Debug.Log($"✅ Asset improvement completed: {asset.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to improve asset {asset.name}: {ex.Message}");
            }
        }
    }
}
