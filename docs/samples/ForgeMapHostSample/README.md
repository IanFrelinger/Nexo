# Forge map host sample (M1–M6 + phases A/B/C)

Runnable **.NET 8** console app that walks through **M1–M5** plus **phase A (M6)**, **phase B (tile cache)**, and points at **phase C (engine bridge)** docs:

1. **M1 — Manifest consumption:** `GET /api/forge/engine/{engineId}/aesthetic-manifest`, unwraps the JSON envelope, prints pack id / profile / binding count.
2. **M4 — LOD pyramid:** `GET /api/forge/map/tile-pyramid` — prints zoom tier(s) from **`LodLevels`** vs **`PYRAMID_FINEST_ZOOM`** (runs early so you see pyramid without Mapbox).
3. **Phase A / M6 — Material hints:** `GET /api/forge/map/material-hints` — procedural surface hints for the active aesthetic.
4. **M2 — Tile orchestration:** Uses **`WebMercatorTileMath`** + **`VectorTileUrlBuilder`** (from `Nexo.GameDomain`) to build a Mapbox vector tile URL from lon/lat/zoom + **`MAPBOX_ACCESS_TOKEN`**.
5. **Phase B — Tile disk cache:** When **`NEXO_TILE_CACHE_DIR`** is set, writes raw MVT bytes under **`MapTileDiskCache`** keyed by **`MapTileCacheKey`** (aesthetic + provider + **`z/x/y`**).
6. **M3 — Pipeline:** `POST /api/forge/map/pipeline/run` with the tile URL and tile indices; prints parse/verification detail from the API.
7. **Phase C — Engine bridge:** After run, prints a pointer to **`docs/engine-bridge/README.md`** and Unity/Godot snippets.

## Prerequisites

- **Nexo.API** running locally (or set **`NEXO_API_BASE_URL`** to your deployment).
- Optional: **Mapbox access token** for live tile fetch — must match **`Nexo:ForgeSession:AllowedMapFetchHosts`** on the API (include `api.mapbox.com`).

## Run

```bash
cd docs/samples/ForgeMapHostSample
dotnet run
```

With Mapbox:

```bash
export MAPBOX_ACCESS_TOKEN="pk...."
export NEXO_API_BASE_URL="http://localhost:5000"
export SAMPLE_LAT="37.7749"
export SAMPLE_LON="-122.4194"
export SAMPLE_ZOOM="14"
dotnet run
```

## Environment variables

| Variable | Default | Purpose |
|----------|---------|---------|
| `NEXO_API_BASE_URL` | `http://localhost:5000` | Nexo.API base URL |
| `FORGE_ENGINE_ID` | `unity` | Path segment for manifest endpoint |
| `MAPBOX_ACCESS_TOKEN` | _(empty)_ | Enables M2/M3 Mapbox URL + pipeline fetch |
| `MAPBOX_TILESET_ID` | `mapbox.mapbox-streets-v8` | Mapbox tileset path |
| `PYRAMID_FINEST_ZOOM` | `14` | Query param for **`GET /api/forge/map/tile-pyramid`** (M4 LOD pyramid). |
| `FORGE_AESTHETIC_ID` | `voxel` | Used as the aesthetic segment in **`MapTileCacheKey`** when caching tiles (phase B). |
| `NEXO_TILE_CACHE_DIR` | _(empty)_ | When set, enables **`MapTileDiskCache`** demo after building the Mapbox URL (phase B). |

## Security

Do **not** commit tokens. The reference pipeline only fetches hosts allow-listed on the server.

See also: **`docs/architecture/forge-map-host-integration.md`**.
