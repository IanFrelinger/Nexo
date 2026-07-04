namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Supported 3D model formats.
/// 
/// Defines 3D model file formats:
/// - GLB: Binary glTF (recommended for web)
/// - GLTF: Text-based glTF
/// - FBX: Autodesk FBX format
/// - OBJ: Wavefront OBJ format
/// - USD: Universal Scene Description
/// 
/// Used by IModel3DGenerator to specify output format.
/// </summary>
public enum Model3DFormat
{
    GLB,
    GLTF,
    FBX,
    OBJ,
    USD
}
