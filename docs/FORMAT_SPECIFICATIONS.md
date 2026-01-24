# Nexo Geospatial Format Specifications

This document describes the file formats used by Nexo for geospatial data exchange.

---

## World Bundle Format

### Directory Structure

A world bundle is a directory containing the following structure:

```
world_bundle/
├── manifest.json          # Bundle metadata and structure
├── materials.json        # Material assignments
├── instances.json        # Vegetation/object instances
├── terrain_chunks/       # Chunked terrain meshes
│   ├── terrain_0_0.obj
│   ├── terrain_0_1.obj
│   └── ...
├── buildings.obj         # Building meshes
├── roads.obj            # Road meshes
├── water.obj            # Water meshes
├── world.mtl            # Material library (OBJ format)
└── unity/               # Unity-specific files (optional)
    └── IMPORT_INSTRUCTIONS.txt
```

### manifest.json

The manifest file describes the world bundle structure and metadata.

**Schema:**
```json
{
  "version": "1.0",
  "bounds": {
    "minLatitude": 37.7749,
    "maxLatitude": 37.8049,
    "minLongitude": -122.4194,
    "maxLongitude": -122.3894
  },
  "coordinateSystem": {
    "projection": "webmercator",
    "parameters": {}
  },
  "terrain": {
    "chunked": true,
    "chunkSizeMeters": 1000.0,
    "chunks": [
      {
        "x": 0,
        "y": 0,
        "file": "terrain_chunks/terrain_0_0.obj"
      }
    ]
  },
  "features": {
    "buildings": {
      "file": "buildings.obj",
      "count": 1234
    },
    "roads": {
      "file": "roads.obj",
      "count": 567
    },
    "water": {
      "file": "water.obj",
      "count": 89
    }
  },
  "instances": {
    "file": "instances.json",
    "count": 10000
  },
  "materials": {
    "file": "materials.json"
  }
}
```

**Fields:**
- `version` (string, required): Bundle format version
- `bounds` (object, required): Geographic bounds in WGS84
- `coordinateSystem` (object, required): Coordinate system information
- `terrain` (object, optional): Terrain mesh information
- `features` (object, optional): Feature mesh information
- `instances` (object, optional): Instance placement information
- `materials` (object, optional): Material assignment information

### materials.json

Material assignments for different mesh types.

**Schema:**
```json
{
  "terrain": {
    "material": "terrain_default",
    "texture": "terrain_texture.png"
  },
  "buildings": {
    "material": "building_concrete",
    "texture": "building_texture.png"
  },
  "roads": {
    "material": "road_asphalt",
    "texture": "road_texture.png"
  },
  "water": {
    "material": "water_default",
    "texture": null
  }
}
```

**Material Properties:**
- `material` (string): Material name/identifier
- `texture` (string|null): Path to texture file (relative to bundle root)
- `normalMap` (string|null, optional): Path to normal map texture
- `roughness` (float, optional): Material roughness (0.0-1.0)
- `metallic` (float, optional): Material metallic value (0.0-1.0)
- `emission` (array, optional): Emission color [R, G, B] (0.0-1.0)

### instances.json

Vegetation and object instance placements.

**Schema:**
```json
{
  "instances": [
    {
      "type": "tree",
      "position": [100.5, 25.3, 200.7],
      "rotation": [0.0, 45.0, 0.0],
      "scale": [1.0, 1.0, 1.0],
      "prefab": "tree_oak_01"
    },
    {
      "type": "bush",
      "position": [150.2, 24.8, 180.1],
      "rotation": [0.0, 0.0, 0.0],
      "scale": [0.8, 0.8, 0.8],
      "prefab": "bush_01"
    }
  ]
}
```

**Fields:**
- `type` (string, required): Instance type identifier
- `position` (array, required): [X, Y, Z] position in meters
- `rotation` (array, optional): [X, Y, Z] rotation in degrees. Default: [0, 0, 0]
- `scale` (array, optional): [X, Y, Z] scale. Default: [1, 1, 1]
- `prefab` (string, optional): Prefab/model identifier

---

## Mesh Formats

### OBJ Format

Nexo generates standard Wavefront OBJ format meshes.

**Features:**
- Vertex positions (v)
- Optional texture coordinates (vt)
- Optional vertex normals (vn)
- Face definitions (f)

**Example:**
```
# Nexo.GeoTerrain OBJ
g terrain
v 0.0 0.0 0.0
v 1.0 0.0 0.0
v 1.0 0.0 1.0
v 0.0 0.0 1.0
vt 0.0 0.0
vt 1.0 0.0
vt 1.0 1.0
vt 0.0 1.0
vn 0.0 1.0 0.0
f 1/1/1 2/2/1 3/3/1
f 1/1/1 3/3/1 4/4/1
```

### glTF 2.0 Format

Nexo generates glTF 2.0 format for modern 3D engines.

**Features:**
- JSON scene description
- Binary buffer for geometry data
- Material definitions
- Texture references

**Structure:**
- `.gltf` file: JSON scene description
- `.bin` file: Binary geometry data
- Texture files: Referenced by URI

### GLB Format

GLB is the binary version of glTF, containing everything in a single file.

**Structure:**
- Header (12 bytes)
- JSON chunk (scene description)
- BIN chunk (geometry data)

### FBX Format

Nexo generates ASCII FBX 7.4 format.

**Features:**
- Mesh geometry
- Vertex normals
- UV coordinates
- Material assignments

### USD Format

Nexo generates USD ASCII format for Pixar Universal Scene Description.

**Features:**
- Mesh definitions
- Vertex positions
- Face indices
- Optional normals and UVs

---

## Vector Feature Formats

### GeoJSON Format

Nexo can export vector features as GeoJSON.

**Schema:**
```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "id": "building_123",
      "geometry": {
        "type": "Polygon",
        "coordinates": [[
          [-122.4194, 37.7749],
          [-122.3894, 37.7749],
          [-122.3894, 37.8049],
          [-122.4194, 37.8049],
          [-122.4194, 37.7749]
        ]]
      },
      "properties": {
        "kind": "building",
        "height": 25.5,
        "provider": "osm"
      }
    }
  ]
}
```

### JSON Format

Nexo's internal JSON format for vector features.

**Schema:**
```json
{
  "features": [
    {
      "id": "building_123",
      "kind": "building",
      "geometry": {
        "type": "polygon",
        "points": [
          {"latitude": 37.7749, "longitude": -122.4194},
          {"latitude": 37.7749, "longitude": -122.3894},
          {"latitude": 37.8049, "longitude": -122.3894},
          {"latitude": 37.8049, "longitude": -122.4194}
        ]
      },
      "properties": {
        "height": 25.5,
        "provider": "osm"
      }
    }
  ]
}
```

---

## Coordinate Systems

### Supported Projections

1. **Equirectangular** (`local_equirectangular`)
   - Simple local approximation
   - Good for small areas (< 0.25° span)

2. **Web Mercator** (`webmercator`, `epsg:3857`)
   - Standard web mapping projection
   - Compatible with web tile services

3. **UTM** (`utm`)
   - Universal Transverse Mercator
   - Auto-zone selection based on bounds
   - Good for larger areas

4. **Lambert Conformal Conic** (`lambert`)
   - Regional mapping
   - Good for mid-latitude regions

5. **Albers Equal Area** (`albers`)
   - Continental mapping
   - Preserves area

6. **State Plane** (`state_plane`)
   - US-specific coordinate systems
   - State-based projections

### Coordinate Format

All geographic coordinates use WGS84 (EPSG:4326):
- **Latitude**: -90.0 to 90.0 degrees
- **Longitude**: -180.0 to 180.0 degrees

Bounds are specified as: `"minLat,maxLat,minLon,maxLon"`

Example: `"37.7749,37.8049,-122.4194,-122.3894"`

---

## Validation

### World Bundle Validation

Use the `nexo world validate` command or API endpoint to validate a world bundle:

```bash
nexo world validate --bundle-dir ./world_bundle
```

**Checks:**
- Manifest file exists and is valid JSON
- All referenced files exist
- Coordinate system is valid
- Bounds are valid (min < max)
- Material assignments are valid

### Mesh Validation

Mesh quality can be validated using `MeshQualityAnalyzer`:

```csharp
var report = MeshQualityAnalyzer.Analyze(elevationGrid, meshData, includeAdvancedMetrics: true);
```

**Metrics:**
- Vertex/triangle counts
- Height ranges
- Triangle quality (aspect ratio, area distribution)
- Mesh accuracy (deviation from source)
- Slope analysis

---

## Examples

### Complete World Bundle Example

```bash
# Generate world bundle
nexo world generate \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --output-dir ./san_francisco_world \
  --elevation-provider mapbox-terrain-rgb \
  --vector-provider hybrid \
  --mapbox-token $MAPBOX_ACCESS_TOKEN \
  --osm-pbf california-latest.osm.pbf

# Validate bundle
nexo world validate --bundle-dir ./san_francisco_world

# Result structure:
# san_francisco_world/
#   ├── manifest.json
#   ├── materials.json
#   ├── instances.json
#   ├── terrain_chunks/
#   │   ├── terrain_0_0.obj
#   │   └── terrain_0_1.obj
#   ├── buildings.obj
#   ├── roads.obj
#   ├── water.obj
#   └── world.mtl
```

---

## Version History

- **1.0** (2024-01-23): Initial format specification
  - World bundle structure
  - Material assignments
  - Instance placements
  - Mesh formats (OBJ, glTF, GLB, FBX, USD)

---

## References

- [glTF 2.0 Specification](https://www.khronos.org/gltf/)
- [Wavefront OBJ Format](https://en.wikipedia.org/wiki/Wavefront_.obj_file)
- [GeoJSON Specification](https://geojson.org/)
- [USD Documentation](https://openusd.org/)
