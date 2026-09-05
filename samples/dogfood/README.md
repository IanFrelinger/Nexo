# Dogfood write targets

Autonomy campaign objectives under [`../autonomy-objectives/`](../autonomy-objectives/) used to
name `applications/Ashlar.Samples.Dogfood/...` as `touch.pathPrefixes`. That tree left the
monorepo in the 2026-08-31 native-responsibility slim.

These directories are the in-repo write targets the loop is allowed to touch. The brick
skeletons and witnesses stay in `samples/autonomy-objectives/`; generated candidates land
here.

| Prefix | Objective |
|--------|-----------|
| `Text/` | `text-slug` |
| `Colours/` | `rgb-hex-parse` |
| `Versions/` | `semver-parse` |
| `Locks/` | `door-lock-transition` |
