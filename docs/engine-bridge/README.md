# Engine bridge (phase C)

Thin integration patterns so **Unity**, **Godot**, or other hosts consume the same Forge contracts:

- **`GET /api/forge/engine/{engineId}/aesthetic-manifest`** — bind surface roles to materials/shaders.
- **`GET /api/forge/map/tile-pyramid`** — drive prefetch distance / streaming zoom.
- **`GET /api/forge/map/material-hints`** — procedural colours + LOD notes without tessellation in Nexo.API.

Snippets below are **copy-paste starters**, not full plugins.

## Unity (.NET)

See **`snippets/UnitySample.cs`**: `HttpClient` calls manifest + material hints, deserialize manifest JSON with `System.Text.Json`.

## Godot 4 (GDScript)

See **`snippets/GodotTileBridge.gd`**: `HTTPRequest` GET pyramid JSON and print tier zoom levels.

## Related

- **`docs/architecture/forge-map-host-integration.md`** — milestones M1–M6.
- **`docs/samples/ForgeMapHostSample`** — end-to-end console rehearsal.
