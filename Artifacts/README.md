# Artifacts Directory

This directory contains generated test artifacts and example outputs from development iterations.

## Contents

### `iteration-1/` and `iteration-1-generated/`
- **Purpose:** Test artifacts from early development iterations
- **Status:** Historical - kept for reference
- **Action:** Can be archived or removed if no longer needed

### `iteration-2-generated/`
- **Purpose:** Test artifacts from second development iteration
- **Status:** Historical - kept for reference
- **Action:** Can be archived or removed if no longer needed

### `tmp_world_tri_chunked/`
- **Purpose:** Example world bundle output (terrain, buildings, roads, water)
- **Status:** Example/test data
- **Contents:**
  - OBJ mesh files (terrain, buildings, roads, water)
  - JSON metadata (instances, manifest, materials)
  - Unity import instructions
- **Action:** Useful as example output - keep for reference

## Maintenance

These artifacts are **not** part of the build process and are **not** required for the application to function.

- **Safe to delete:** All contents can be removed without affecting functionality
- **Git ignored:** This directory should be in `.gitignore` (verify)
- **Purpose:** Examples and test outputs for development reference

## Recommendations

1. **For CI/CD:** These should not be committed to version control
2. **For examples:** Consider moving to `examples/` directory if kept
3. **For cleanup:** Can be periodically archived or removed
