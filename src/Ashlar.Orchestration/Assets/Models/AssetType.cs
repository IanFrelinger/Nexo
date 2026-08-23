using System.Text.Json;

namespace Ashlar.Orchestration.Assets.Models;

/// <summary>
/// Types of assets that can be generated.
/// </summary>
public enum AssetType
{
    Image,
    Audio,
    Model3D,
    Shader,
    Animation,
    Texture,
    Material,
    Prefab
}
