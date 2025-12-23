#!/usr/bin/env bash
set -euo pipefail

# Create visible demo assets in Unity project
# Usage: ./scripts/create-unity-demo-assets.sh [project-path] [iteration-number]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_PATH="${1:-$ART/IterativeGameDemo}"
ITERATION="${2:-1}"

echo "🎨 Creating Unity Demo Assets"
echo "============================="
echo "  Project: $PROJECT_PATH"
echo "  Iteration: $ITERATION"
echo ""

# Create directories
mkdir -p "$PROJECT_PATH/Assets/GeneratedScenes"
mkdir -p "$PROJECT_PATH/Assets/Iteration${ITERATION}"
mkdir -p "$PROJECT_PATH/Assets/Scripts"

# Create a simple scene file
SCENE_FILE="$PROJECT_PATH/Assets/Iteration${ITERATION}/Iteration${ITERATION}Scene.unity"
cat > "$SCENE_FILE" << 'EOF'
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 9
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 0
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_IndirectSpecularColor: {r: 0, g: 0, b: 0, a: 1}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 12
  m_GIWorkflowMode: 1
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_FinalGather: 0
    m_FinalGatherFiltering: 1
    m_FinalGatherRayCount: 256
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 1
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserType: 1
    m_PVRFilterType: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadius: 1
    m_PVRFilteringAtrousPositionSigma: 0.5
    m_PVRFilteringAtrousColorSigma: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 2
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    accuratePlacement: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
--- !u!1 &519420028
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 519420032}
  - component: {fileID: 519420031}
  - component: {fileID: 519420029}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!81 &519420029
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 519420028}
  m_Enabled: 1
--- !u!114 &519420031
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 519420028}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: a79441f348f89743a2939f4d699eac1, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_BackgroundColor: {r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}
  m_orthographic: 1
  m_orthographicSize: 5
  m_near: 0.3
  m_far: 1000
  m_useOcclusionCulling: 1
  m_occlusionCullingMask: 4294967295
  m_projectionMatrixMode: 1
  m_sensorSize: {x: 36, y: 24}
  m_lensShift: {x: 0, y: 0}
  m_focalLength: 50
  m_normalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near plane: 0.3
  far plane: 1000
  field of view: 60
  orthographic: 1
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!4 &519420032
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 519420028}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 1, z: -10}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &1234567890
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 1234567891}
  - component: {fileID: 1234567892}
  m_Layer: 0
  m_Name: Iteration ITERATION_PLACEHOLDER Demo Object
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &1234567891
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1234567890}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_RootOrder: 1
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &1234567892
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1234567890}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 0000000000000000, type: 0}
  m_Name: 
  m_EditorClassIdentifier: 
EOF

# Replace placeholder with actual iteration number
sed -i '' "s/ITERATION_PLACEHOLDER/$ITERATION/g" "$SCENE_FILE"

# Create .meta file for the scene
cat > "${SCENE_FILE}.meta" << EOF
fileFormatVersion: 2
guid: $(uuidgen | tr '[:upper:]' '[:lower:]' | tr -d '-' | head -c 32)
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
EOF

# Create a simple C# script to show in the editor
SCRIPT_FILE="$PROJECT_PATH/Assets/Scripts/Iteration${ITERATION}Demo.cs"
cat > "$SCRIPT_FILE" << EOF
using UnityEngine;

/// <summary>
/// Demo script for Iteration ${ITERATION}
/// Generated by Nexo iterative game development pipeline
/// </summary>
public class Iteration${ITERATION}Demo : MonoBehaviour
{
    [SerializeField] private string iterationInfo = "Iteration ${ITERATION} - Generated by Nexo";
    
    void Start()
    {
        Debug.Log($"Iteration ${ITERATION} Demo Started");
        Debug.Log("This script was generated by the iterative game development pipeline");
    }
    
    void Update()
    {
        // Demo script - visible in Unity editor
    }
}
EOF

# Create .meta file for the script
cat > "${SCRIPT_FILE}.meta" << EOF
fileFormatVersion: 2
guid: $(uuidgen | tr '[:upper:]' '[:lower:]' | tr -d '-' | head -c 32)
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
EOF

# Create a README in the iteration folder
cat > "$PROJECT_PATH/Assets/Iteration${ITERATION}/README.txt" << EOF
Iteration ${ITERATION} Generated Assets
=======================================

This folder contains assets generated during iteration ${ITERATION} of the
iterative game development process.

Generated: $(date)
Scene: Iteration${ITERATION}Scene.unity
Script: Iteration${ITERATION}Demo.cs

These assets were created automatically by the Nexo iterative pipeline.
EOF

echo "✅ Created Unity assets for iteration $ITERATION:"
echo "   Scene: $SCENE_FILE"
echo "   Script: $SCRIPT_FILE"
echo "   README: $PROJECT_PATH/Assets/Iteration${ITERATION}/README.txt"
echo ""
echo "💡 Unity Editor should refresh and show these assets automatically"
echo ""

