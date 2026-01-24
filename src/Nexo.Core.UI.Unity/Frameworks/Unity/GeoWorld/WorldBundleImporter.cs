using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nexo.GeoWorld.Unity;

#if UNITY_EDITOR
public sealed class WorldBundleImporter : EditorWindow
{
    [Serializable] private sealed class WorldArtifactDto { public string kind; public string path; public string contentType; }
    [Serializable] private sealed class WorldArtifactMetadataDto
    {
        public string id;
        public string parentId;
        public int lodLevel;
        public int vertexCount;
        public int triangleCount;
        public int instanceCount;
        public WorldGridChunkRefDto gridChunk;
        public WorldAabbDto localBoundsMeters;
    }
    [Serializable] private sealed class WorldGridChunkRefDto { public int x0; public int y0; public int width; public int height; }
    [Serializable] private sealed class WorldAabbDto { public float minX; public float minY; public float minZ; public float maxX; public float maxY; public float maxZ; }

    [Serializable] private sealed class WorldArtifactDtoV2 { public string kind; public string path; public string contentType; public WorldArtifactMetadataDto metadata; }
    [Serializable] private sealed class GeoPointDto { public LatitudeDto latitude; public LongitudeDto longitude; }
    [Serializable] private sealed class LatitudeDto { public double degrees; }
    [Serializable] private sealed class LongitudeDto { public double degrees; }
    [Serializable] private sealed class GeoBoundsDto { public LatitudeDto minLatitude; public LongitudeDto minLongitude; public LatitudeDto maxLatitude; public LongitudeDto maxLongitude; }
    [Serializable] private sealed class WorldCoordinateFrameDto { public string projectionId; /* parameters intentionally ignored (JsonUtility limitation) */ }

    [Serializable]
    private sealed class WorldBundleManifestDto
    {
        public string version;
        public string createdAtUtc;
        public GeoBoundsDto bounds;
        public GeoPointDto origin;
        public float metersPerUnit = 1f;
        public WorldCoordinateFrameDto coordinateFrame;
        public WorldArtifactDtoV2[] artifacts;
    }

    [Serializable] private sealed class MaterialAssignmentDto { public string meshKind; public string materialName; public float uvMetersPerRepeat; public string baseColorTexturePath; }
    [Serializable] private sealed class MaterialAssignmentListDto { public MaterialAssignmentDto[] items; }

    [Serializable] private sealed class Vec3Dto { public float x; public float y; public float z; }
    [Serializable] private sealed class InstanceDto { public string kind; public Vec3Dto position; public float yawDegrees; public float uniformScale; }
    [Serializable] private sealed class InstancesDto { public string kind; public string units; public InstanceDto[] instances; }

    private DefaultAsset _bundleFolder;
    private PrefabLibrary _prefabLibrary;
    private MaterialLibrary _materialLibrary;
    private bool _importTerrainAsLodGroups = true;
    private int _maxAssetsPerEditorTick = 2;
    private bool _batchInstances = true;

    private IEnumerator _importEnumerator;

    [MenuItem("Nexo/GeoWorld/Import World Bundle...")]
    public static void ShowWindow()
    {
        var w = GetWindow<WorldBundleImporter>("GeoWorld Import");
        w.minSize = new Vector2(420, 220);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("World bundle folder (must be under Assets/)", EditorStyles.boldLabel);
        _bundleFolder = (DefaultAsset)EditorGUILayout.ObjectField("Bundle Folder", _bundleFolder, typeof(DefaultAsset), false);
        _prefabLibrary = (PrefabLibrary)EditorGUILayout.ObjectField("Prefab Library", _prefabLibrary, typeof(PrefabLibrary), false);
        _materialLibrary = (MaterialLibrary)EditorGUILayout.ObjectField("Material Library", _materialLibrary, typeof(MaterialLibrary), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Import options", EditorStyles.boldLabel);
        _importTerrainAsLodGroups = EditorGUILayout.Toggle("Terrain LOD Groups", _importTerrainAsLodGroups);
        _batchInstances = EditorGUILayout.Toggle("Batch Instances", _batchInstances);
        _maxAssetsPerEditorTick = EditorGUILayout.IntSlider("Assets Per Tick", _maxAssetsPerEditorTick, 1, 20);

        EditorGUILayout.Space();

        if (GUILayout.Button("Import Into Scene"))
        {
            try
            {
                StartImport();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorUtility.DisplayDialog("GeoWorld Import Failed", ex.Message, "OK");
            }
        }
    }

    private void StartImport()
    {
        if (_importEnumerator != null)
        {
            throw new InvalidOperationException("Import is already running.");
        }

        _importEnumerator = ImportEnumerator();
        EditorApplication.update += ImportTick;
    }

    private void ImportTick()
    {
        try
        {
            var steps = Math.Max(1, _maxAssetsPerEditorTick);
            for (var i = 0; i < steps; i++)
            {
                if (_importEnumerator == null) break;
                if (!_importEnumerator.MoveNext())
                {
                    _importEnumerator = null;
                    EditorApplication.update -= ImportTick;
                    EditorUtility.ClearProgressBar();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _importEnumerator = null;
            EditorApplication.update -= ImportTick;
            EditorUtility.ClearProgressBar();
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("GeoWorld Import Failed", ex.Message, "OK");
        }
    }

    private IEnumerator ImportEnumerator()
    {
        if (_bundleFolder == null) throw new InvalidOperationException("Select a bundle folder.");

        var folderPath = AssetDatabase.GetAssetPath(_bundleFolder);
        if (string.IsNullOrEmpty(folderPath) || !folderPath.StartsWith("Assets", StringComparison.Ordinal))
            throw new InvalidOperationException("Bundle folder must be under Assets/.");

        var manifestPath = Path.Combine(folderPath, "manifest.json").Replace("\\", "/");
        if (!File.Exists(manifestPath)) throw new FileNotFoundException("manifest.json not found.", manifestPath);
        var manifestJson = File.ReadAllText(manifestPath);
        var manifest = JsonUtility.FromJson<WorldBundleManifestDto>(manifestJson);
        if (manifest == null) throw new InvalidOperationException("Failed to parse manifest.json.");

        // Load materials.json (array) by wrapping.
        var materials = LoadMaterials(folderPath);

        // Idempotent-ish import: replace previous root if present.
        var existing = GameObject.Find("GeoWorldBundle");
        if (existing != null)
        {
            DestroyImmediate(existing);
        }

        var root = new GameObject("GeoWorldBundle");
        root.transform.position = Vector3.zero;

        // Build a simple index for terrain chunk LOD groups.
        var terrainByChunk = new Dictionary<string, List<WorldArtifactDtoV2>>();

        if (manifest.artifacts != null)
        {
            for (var i = 0; i < manifest.artifacts.Length; i++)
            {
                var a = manifest.artifacts[i];
                if (a == null) continue;
                if (string.IsNullOrEmpty(a.path)) continue;
                if (!a.path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase)) continue;

                // Terrain chunk LODs (metadata-driven)
                if (_importTerrainAsLodGroups && a.metadata != null && a.metadata.gridChunk != null &&
                    (a.kind != null && a.kind.StartsWith("terrain_chunk", StringComparison.OrdinalIgnoreCase)))
                {
                    var key = a.metadata.gridChunk.x0 + "_" + a.metadata.gridChunk.y0 + "_" + a.metadata.gridChunk.width + "_" + a.metadata.gridChunk.height;
                    if (!terrainByChunk.TryGetValue(key, out var list))
                    {
                        list = new List<WorldArtifactDtoV2>();
                        terrainByChunk[key] = list;
                    }
                    list.Add(a);
                    continue;
                }

                // Default: import as a single model.
                var assetPath = Path.Combine(folderPath, a.path).Replace("\\", "/");
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (go == null)
                {
                    Debug.LogWarning("OBJ not imported yet (or missing): " + assetPath);
                    continue;
                }

                EditorUtility.DisplayProgressBar("GeoWorld Import", "Importing " + a.path, 0.1f);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(go);
                instance.name = a.kind;
                instance.transform.SetParent(root.transform, worldPositionStays: false);
                ApplyMaterial(instance, a.kind, folderPath, materials);
                yield return null;
            }
        }

        // Import terrain chunks as LODGroups.
        if (_importTerrainAsLodGroups && terrainByChunk.Count > 0)
        {
            var terrainRoot = new GameObject("TerrainChunks");
            terrainRoot.transform.SetParent(root.transform, worldPositionStays: false);

            foreach (var kvp in terrainByChunk)
            {
                var list = kvp.Value;
                if (list == null || list.Count == 0) continue;

                // Sort by "detail rank" descending (higher is more detailed).
                list.Sort((a, b) => GetDetailRank(b).CompareTo(GetDetailRank(a)));

                var chunkGo = new GameObject("terrain_chunk_" + kvp.Key);
                chunkGo.transform.SetParent(terrainRoot.transform, worldPositionStays: false);

                var lodRenderers = new List<Renderer>();
                var lods = new List<LOD>();

                for (var i = 0; i < list.Count; i++)
                {
                    var a = list[i];
                    var assetPath = Path.Combine(folderPath, a.path).Replace("\\", "/");
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab == null) continue;

                    EditorUtility.DisplayProgressBar("GeoWorld Import", "Importing LOD " + a.path, 0.2f);
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    inst.name = a.kind + "_lod";
                    inst.transform.SetParent(chunkGo.transform, worldPositionStays: false);
                    ApplyMaterial(inst, "terrain", folderPath, materials);

                    var r = inst.GetComponentInChildren<Renderer>();
                    if (r != null) lodRenderers.Add(r);
                    yield return null;
                }

                // Create LODGroup if we have multiple.
                if (lodRenderers.Count > 0)
                {
                    var group = chunkGo.AddComponent<LODGroup>();
                    var rendererArr = lodRenderers.ToArray();
                    var count = rendererArr.Length;
                    for (var i = 0; i < count; i++)
                    {
                        // Simple spacing: most detailed at highest screen size.
                        var t = 1f - (i / (float)Math.Max(1, count));
                        var screen = Mathf.Clamp01(0.6f * t + 0.05f);
                        lods.Add(new LOD(screen, new[] { rendererArr[i] }));
                    }
                    group.SetLODs(lods.ToArray());
                    group.RecalculateBounds();
                }
            }
        }

        // Spawn instances.
        SpawnScenery(folderPath, manifest, root.transform, _batchInstances);

        Selection.activeGameObject = root;
        Debug.Log("GeoWorld import complete.");
        yield break;
    }

    private void ApplyMaterial(GameObject model, string meshKind, string folderPath, MaterialAssignmentListDto materials)
    {
        if (_materialLibrary == null) return;
        if (materials == null || materials.items == null) return;

        string matName = null;
        string texRel = null;
        for (var i = 0; i < materials.items.Length; i++)
        {
            var m = materials.items[i];
            if (m == null) continue;
            if (string.Equals(m.meshKind, meshKind, StringComparison.OrdinalIgnoreCase))
            {
                matName = m.materialName;
                texRel = m.baseColorTexturePath;
                break;
            }
        }
        if (string.IsNullOrEmpty(matName)) return;

        var mat = _materialLibrary.GetMaterial(matName);
        if (mat == null)
        {
            Debug.LogWarning("Material not found in library: " + matName);
            return;
        }

        var renderers = model.GetComponentsInChildren<Renderer>(includeInactive: true);
        for (var i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial = mat;
        }

        // Optional: apply mesh-specific basecolor texture if provided by materials.json
        if (!string.IsNullOrEmpty(texRel))
        {
            var texPath = Path.Combine(folderPath, texRel).Replace("\\", "/");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                for (var i = 0; i < renderers.Length; i++)
                {
                    var m = renderers[i].sharedMaterial;
                    if (m != null) m.mainTexture = tex;
                }
            }
        }

        // Optional: auto-hook terrain texture if present.
        if (string.Equals(meshKind, "terrain", StringComparison.OrdinalIgnoreCase))
        {
            var texPath = Path.Combine(folderPath, "terrain_texture.png").Replace("\\", "/");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                for (var i = 0; i < renderers.Length; i++)
                {
                    var m = renderers[i].sharedMaterial;
                    if (m != null) m.mainTexture = tex;
                }
            }
        }
    }

    private void SpawnScenery(string folderPath, WorldBundleManifestDto manifest, Transform parent, bool batch)
    {
        if (_prefabLibrary == null) return;

        // Prefer chunked instances if present in manifest.
        var chunkPaths = new System.Collections.Generic.List<string>();
        if (manifest != null && manifest.artifacts != null)
        {
            for (var i = 0; i < manifest.artifacts.Length; i++)
            {
                var a = manifest.artifacts[i];
                if (a == null) continue;
                if (!string.Equals(a.kind, "instances_chunk", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(a.path)) continue;
                chunkPaths.Add(Path.Combine(folderPath, a.path).Replace("\\", "/"));
            }
        }

        var instanceFiles = chunkPaths.Count > 0
            ? chunkPaths.ToArray()
            : new[] { Path.Combine(folderPath, "instances.json").Replace("\\", "/") };

        var root = new GameObject("Instances");
        root.transform.SetParent(parent, worldPositionStays: false);

        if (batch)
        {
            SpawnSceneryBatched(instanceFiles, root.transform);
            return;
        }

        for (var f = 0; f < instanceFiles.Length; f++)
        {
            var instancesPath = instanceFiles[f];
            if (!File.Exists(instancesPath)) continue;

            var json = File.ReadAllText(instancesPath);
            var dto = JsonUtility.FromJson<InstancesDto>(json);
            if (dto == null || dto.instances == null) continue;

            for (var i = 0; i < dto.instances.Length; i++)
            {
                var inst = dto.instances[i];
                if (inst == null || inst.position == null) continue;
                var prefab = _prefabLibrary.GetPrefab(inst.kind);
                if (prefab == null) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetParent(root.transform, worldPositionStays: false);
                go.transform.localPosition = new Vector3(inst.position.x, inst.position.y, inst.position.z);
                go.transform.localRotation = Quaternion.Euler(0f, inst.yawDegrees, 0f);
                go.transform.localScale = Vector3.one * (inst.uniformScale <= 0 ? 1f : inst.uniformScale);
            }
        }
    }

    private void SpawnSceneryBatched(string[] instanceFiles, Transform parent)
    {
        // Simple batching: group by kind and combine meshes when possible.
        var byKind = new Dictionary<string, List<InstanceXform>>();

        for (var f = 0; f < instanceFiles.Length; f++)
        {
            var instancesPath = instanceFiles[f];
            if (!File.Exists(instancesPath)) continue;

            var json = File.ReadAllText(instancesPath);
            var dto = JsonUtility.FromJson<InstancesDto>(json);
            if (dto == null || dto.instances == null) continue;

            for (var i = 0; i < dto.instances.Length; i++)
            {
                var inst = dto.instances[i];
                if (inst == null || inst.position == null) continue;
                if (string.IsNullOrEmpty(inst.kind)) continue;

                if (!byKind.TryGetValue(inst.kind, out var list))
                {
                    list = new List<InstanceXform>();
                    byKind[inst.kind] = list;
                }
                list.Add(new InstanceXform
                {
                    Position = new Vector3(inst.position.x, inst.position.y, inst.position.z),
                    YawDegrees = inst.yawDegrees,
                    UniformScale = (inst.uniformScale <= 0 ? 1f : inst.uniformScale)
                });
            }
        }

        foreach (var kvp in byKind)
        {
            var kind = kvp.Key;
            var transforms = kvp.Value;
            if (transforms == null || transforms.Count == 0) continue;

            var prefab = _prefabLibrary.GetPrefab(kind);
            if (prefab == null) continue;

            // If prefab isn't a simple MeshFilter/Renderer, fall back to instantiation.
            var mf = prefab.GetComponentInChildren<MeshFilter>();
            var mr = prefab.GetComponentInChildren<MeshRenderer>();
            if (mf == null || mr == null || mf.sharedMesh == null)
            {
                for (var i = 0; i < transforms.Count; i++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.transform.SetParent(parent, worldPositionStays: false);
                    var t = transforms[i];
                    go.transform.localPosition = t.Position;
                    go.transform.localRotation = Quaternion.Euler(0f, t.YawDegrees, 0f);
                    go.transform.localScale = Vector3.one * t.UniformScale;
                }
                continue;
            }

            var combined = new CombineInstance[transforms.Count];
            for (var i = 0; i < transforms.Count; i++)
            {
                var t = transforms[i];
                combined[i] = new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = Matrix4x4.TRS(t.Position, Quaternion.Euler(0f, t.YawDegrees, 0f), Vector3.one * t.UniformScale)
                };
            }

            var combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combined, mergeSubMeshes: true, useMatrices: true, hasLightmapData: false);

            var goCombined = new GameObject("Instances_" + kind);
            goCombined.transform.SetParent(parent, worldPositionStays: false);
            var mfOut = goCombined.AddComponent<MeshFilter>();
            mfOut.sharedMesh = combinedMesh;
            var mrOut = goCombined.AddComponent<MeshRenderer>();
            mrOut.sharedMaterial = mr.sharedMaterial;
        }
    }

    [Serializable]
    private sealed class InstanceXform
    {
        public Vector3 Position;
        public float YawDegrees;
        public float UniformScale = 1f;
    }

    private static float GetDetailRank(WorldArtifactDtoV2 a)
    {
        if (a == null) return 0;
        var kind = a.kind ?? "";
        var lod = a.metadata != null ? a.metadata.lodLevel : 0;

        // Higher rank = more detailed.
        if (kind.IndexOf("_lod", StringComparison.OrdinalIgnoreCase) >= 0 && lod > 0)
        {
            // Factor LOD: smaller factor is more detailed -> invert.
            return 1f / Math.Max(1, lod);
        }
        if (kind.IndexOf("_tri", StringComparison.OrdinalIgnoreCase) >= 0 && lod > 0)
        {
            // Triangle budget: higher budget is more detailed.
            return lod;
        }
        return 1000000f; // base chunk is most detailed
    }

    private static MaterialAssignmentListDto LoadMaterials(string folderPath)
    {
        var path = Path.Combine(folderPath, "materials.json").Replace("\\", "/");
        if (!File.Exists(path)) return new MaterialAssignmentListDto { items = new MaterialAssignmentDto[0] };

        var json = File.ReadAllText(path).Trim();
        if (json.StartsWith("[", StringComparison.Ordinal))
        {
            json = "{\"items\":" + json + "}";
        }
        var dto = JsonUtility.FromJson<MaterialAssignmentListDto>(json);
        return dto ?? new MaterialAssignmentListDto { items = new MaterialAssignmentDto[0] };
    }
}
#endif

