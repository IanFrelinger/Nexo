# Functional Validation

This suite runs in CI inside `CiEditorSmoke` and emits:
- `functional_smoke.json` — list of assertions with pass/fail
- `playmode-smoke.junit.xml` — extended with Functional cases for CI parsing

## What we check
- **Gameplay:** Player presence, locomotion substrate, collider, interactions armed, NavMesh, opposition.
- **Visuals:** MainCamera, Light, materials assigned, no error shaders, sane transforms.
- **Audio:** Exactly one enabled AudioListener, AudioSource presence, playOnAwake clips, volume ranges.
- **Systems:** EventSystem present, TimeScale > 0, physics collider presence, basic raycast sanity.

Add new checks by implementing `IFunctionalValidator` and registering it in `CiEditorSmoke`.
