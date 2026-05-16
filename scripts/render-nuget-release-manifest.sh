#!/usr/bin/env bash
# Write nuget-publish-manifest.json + per-file .sha256.txt for each *.nupkg in DIR.
set -euo pipefail
DIR="${1:?directory containing *.nupkg}"
VER="${PACKAGE_VERSION:?set PACKAGE_VERSION (semver without v prefix)}"
VER="${VER#v}"
OUT_JSON="${DIR}/nuget-publish-manifest.json"

python3 - "$DIR" "$VER" "$OUT_JSON" <<'PY'
import hashlib, json, os, sys

d, ver, out_path = sys.argv[1], sys.argv[2], sys.argv[3]
rows = []
for name in sorted(os.listdir(d)):
    if not name.endswith(".nupkg"):
        continue
    path = os.path.join(d, name)
    suf = f".{ver}.nupkg"
    if not name.endswith(suf):
        print(f"error: unexpected nupkg name {name} (expected *.{ver}.nupkg)", file=sys.stderr)
        sys.exit(1)
    pid = name[: -len(suf)]
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    digest = h.hexdigest()
    with open(path + ".sha256.txt", "w", encoding="utf-8") as sf:
        sf.write(f"{digest}  {name}\n")
    rows.append({"id": pid, "version": ver, "fileName": name, "sha256": digest})
with open(out_path, "w", encoding="utf-8") as f:
    json.dump(rows, f, indent=2)
print(f"Wrote {out_path} ({len(rows)} packages)")
PY
