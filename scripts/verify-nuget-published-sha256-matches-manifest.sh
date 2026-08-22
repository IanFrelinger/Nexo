#!/usr/bin/env bash
# Download each package from nuget.org flat-container and assert sha256 matches nuget-publish-manifest.json from the pack step.
# Env: ASHLAR_MANIFEST_JSON (path), ASHLAR_NUGET_VERIFY_VERSION (must match manifest entries)
set -euo pipefail
MAN="${ASHLAR_MANIFEST_JSON:?set ASHLAR_MANIFEST_JSON}"
VER="${ASHLAR_NUGET_VERIFY_VERSION:?set ASHLAR_NUGET_VERIFY_VERSION}"
VER="${VER#v}"
WORKDIR="$(mktemp -d)"
trap 'rm -rf "${WORKDIR}"' EXIT

python3 - "$MAN" "$VER" "$WORKDIR" <<'PY'
import hashlib, json, os, subprocess, sys, urllib.request

manifest_path, ver, workdir = sys.argv[1], sys.argv[2], sys.argv[3]
with open(manifest_path, encoding="utf-8") as f:
    rows = json.load(f)
os.makedirs(workdir, exist_ok=True)
for row in rows:
    pid, v, expected = row["id"], row["version"], row["sha256"]
    if v != ver:
        print(f"skip {pid} version mismatch manifest {v} vs {ver}", file=sys.stderr)
        continue
    id_lc = pid.lower()
    v_lc = v.lower()
    url = f"https://api.nuget.org/v3-flatcontainer/{id_lc}/{v_lc}/{id_lc}.{v_lc}.nupkg"
    out = os.path.join(workdir, row["fileName"])
    print(f"download {url}")
    urllib.request.urlretrieve(url, out)
    h = hashlib.sha256()
    with open(out, "rb") as bf:
        for chunk in iter(lambda: bf.read(1024 * 1024), b""):
            h.update(chunk)
    got = h.hexdigest()
    if got.lower() != expected.lower():
        print(f"::error::SHA256 mismatch for {pid} {v}: expected {expected} got {got}", file=sys.stderr)
        sys.exit(1)
print("verify-nuget-published-sha256-matches-manifest: OK")
PY
