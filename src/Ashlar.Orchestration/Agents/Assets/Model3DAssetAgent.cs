using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using System.Text.RegularExpressions;

namespace Ashlar.Orchestration.Agents.Assets;

/// <summary>
/// Agent that generates 3D model assets for games.
/// 
/// Specialized agent for 3D model generation that:
/// - Uses IModel3DGenerator to create game 3D assets
/// - Supports multiple formats (GLB, GLTF, FBX, OBJ, USD)
/// - Handles prompt generation and refinement for 3D models
/// - Validates model constraints (format, polygon count, textures)
/// - Supports image-to-3D generation from reference images
/// - Manages asset storage and retrieval
/// - Implements retry logic with prompt refinement
/// 
/// Inherits from BaseAssetAgent for common asset generation functionality.
/// </summary>
public sealed class Model3DAssetAgent : BaseAssetAgent
{
    private readonly IModel3DGenerator _model3DGenerator;

    public override AssetType AssetType => AssetType.Model3D;

    public Model3DAssetAgent(
        AgentSpawnSpec spec,
        IModel3DGenerator model3DGenerator,
        IModel? model,
        IAssetStorage assetStorage,
        ILogger<BaseAgent> logger) : base(spec, model, assetStorage, logger)
    {
        _model3DGenerator = model3DGenerator ?? throw new ArgumentNullException(nameof(model3DGenerator));
    }

    protected override async Task<GeneratedAssetBase> GenerateAssetAsync(
        GenerationPrompt prompt,
        CancellationToken cancellationToken)
    {
        var request = new Model3DGenerationRequest
        {
            Prompt = prompt.TextPrompt,
            OutputFormat = DetermineFormat(prompt.Parameters),
            GenerateTextures = prompt.Parameters.TryGetValue("generateTextures", out var textures) 
                ? Convert.ToBoolean(textures) 
                : true,
            TargetPolyCount = prompt.Parameters.TryGetValue("polyCount", out var polyCount) 
                ? Convert.ToInt32(polyCount) 
                : null,
            Quality = DetermineQuality(prompt.Parameters)
        };

        // Check if we have a reference image from dependencies
        var referenceImage = prompt.Parameters.TryGetValue("referenceImage", out var imgPath) 
            ? imgPath.ToString() 
            : null;

        Generated3DModel result;
        if (!string.IsNullOrEmpty(referenceImage) && new FileInfo(referenceImage).Exists)
        {
            result = await _model3DGenerator.GenerateFromImageAsync(referenceImage, request, cancellationToken);
        }
        else
        {
            result = await _model3DGenerator.GenerateFromTextAsync(request, cancellationToken);
        }

        return new GeneratedModel3DAsset
        {
            FilePath = result.FilePath,
            Format = result.Format,
            VertexCount = result.VertexCount,
            TriangleCount = result.TriangleCount,
            TexturePaths = result.TexturePaths
        };
    }

    protected override string GetPromptGenerationSystemPrompt()
    {
        return """
            You are an expert at crafting prompts for AI 3D model generation.
            Given a game asset requirement, generate a detailed prompt that will produce
            a high-quality 3D model.

            Consider:
            - Model type (character, prop, environment, weapon, etc.)
            - Art style (realistic, stylized, low-poly, etc.)
            - Level of detail and polygon count
            - Texture requirements
            - Game context and consistency

            Respond with JSON:
            {
                "prompt": "detailed generation prompt",
                "format": "GLB|GLTF|FBX|OBJ",
                "quality": "draft|low|medium|high|production",
                "polyCount": 5000,
                "generateTextures": true
            }
            """;
    }

    protected override ConstraintResult EvaluateConstraint(
        GeneratedAssetBase asset,
        AgentConstraint constraint)
    {
        var modelAsset = (GeneratedModel3DAsset)asset;
        var desc = constraint.Description.ToLowerInvariant();

        // Check format constraints
        if (desc.Contains("glb") && modelAsset.Format != Model3DFormat.GLB)
        {
            return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
        }
        if (desc.Contains("gltf") && modelAsset.Format != Model3DFormat.GLTF)
        {
            return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
        }
        if (desc.Contains("fbx") && modelAsset.Format != Model3DFormat.FBX)
        {
            return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
        }

        // Check polygon count constraints
        if (desc.Contains("poly") || desc.Contains("triangle"))
        {
            var match = Regex.Match(desc, @"(?:max|maximum|under|less than)\s*(\d+)");
            if (match.Success)
            {
                var maxPoly = int.Parse(match.Groups[1].Value);
                if (modelAsset.TriangleCount > maxPoly)
                {
                    return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
                }
            }

            match = Regex.Match(desc, @"(?:min|minimum|at least|over)\s*(\d+)");
            if (match.Success)
            {
                var minPoly = int.Parse(match.Groups[1].Value);
                if (modelAsset.TriangleCount < minPoly)
                {
                    return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
                }
            }
        }

        // Check texture requirements
        if (desc.Contains("texture") && !modelAsset.TexturePaths.Any())
        {
            if (desc.Contains("must") || desc.Contains("required"))
            {
                return ConstraintResult.Failed;
            }
            return ConstraintResult.Warning;
        }

        return ConstraintResult.Passed;
    }

    protected override string GetMimeType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".glb" => "model/gltf-binary",
            ".gltf" => "model/gltf+json",
            ".fbx" => "application/octet-stream",
            ".obj" => "model/obj",
            ".usd" => "model/vnd.usd",
            _ => "application/octet-stream"
        };
    }

    private Model3DFormat DetermineFormat(IReadOnlyDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("format", out var formatObj))
        {
            var formatStr = formatObj.ToString()?.ToUpperInvariant();
            return formatStr switch
            {
                "GLB" => Model3DFormat.GLB,
                "GLTF" => Model3DFormat.GLTF,
                "FBX" => Model3DFormat.FBX,
                "OBJ" => Model3DFormat.OBJ,
                "USD" => Model3DFormat.USD,
                _ => Model3DFormat.GLB
            };
        }

        return Model3DFormat.GLB; // Default to GLB
    }

    private ModelQuality DetermineQuality(IReadOnlyDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("quality", out var qualityObj))
        {
            var qualityStr = qualityObj.ToString()?.ToLowerInvariant();
            return qualityStr switch
            {
                "draft" => ModelQuality.Draft,
                "low" => ModelQuality.Low,
                "medium" => ModelQuality.Medium,
                "high" => ModelQuality.High,
                "production" => ModelQuality.Production,
                _ => ModelQuality.Medium
            };
        }

        return ModelQuality.Medium;
    }
}
