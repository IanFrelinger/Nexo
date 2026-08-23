# Engine bridge (phases C–D)

Thin integration patterns so **Unity**, **Godot**, or other hosts consume the same Forge contracts:

- **`GET /api/forge/engine/{engineId}/aesthetic-manifest`** — bind surface roles to materials/shaders.
- **`GET /api/forge/map/tile-pyramid`** — drive prefetch distance / streaming zoom.
- **`GET /api/forge/map/material-hints`** — procedural colours + LOD notes without tessellation in Ashlar.API.

On-disk layouts (copy into a game repo):

- **`unity-package/`** — UPM-style **`package.json`** + **`Runtime/ForgeMapBridge.cs`** (same ideas as **`snippets/UnitySample.cs`**).
- **`godot-addon/addons/forge_map_bridge/`** — optional EditorPlugin stub + **`godot_tile_bridge.gd`** (same behavior as **`snippets/GodotTileBridge.gd`**).

Loose snippets remain under **`snippets/`** for quick copy-paste.

## Unity (.NET)

**`snippets/UnitySample.cs`** or **`unity-package/Runtime/ForgeMapBridge.cs`**: `HttpClient` calls manifest + material hints.

## Godot 4 (GDScript)

**`snippets/GodotTileBridge.gd`** or **`godot-addon/addons/forge_map_bridge/godot_tile_bridge.gd`**: `HTTPRequest` GET pyramid JSON.

## Related

- **`docs/architecture/forge-map-host-integration.md`** — milestones M1–M6 + terrain/material extras.
- **`commercial/samples/ForgeMapHostSample`** — console rehearsal including vector + Terrain-RGB pipeline fetch.
