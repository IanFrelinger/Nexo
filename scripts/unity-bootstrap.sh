#!/usr/bin/env bash
set -euo pipefail

# Bootstrap a new Unity project and link local Nexo UPM packages (offline)
# Usage:
#   scripts/unity-bootstrap.sh /path/to/NewUnityProject \
#     [/Applications/Unity/Hub/Editor/2021.3.41f1/Unity.app/Contents/MacOS/Unity]

PROJECT_PATH=${1:-}
UNITY_PATH=${2:-${UNITY_PATH:-}}

if [[ -z "${PROJECT_PATH}" ]]; then
  echo "Usage: $0 <PROJECT_PATH> [UNITY_PATH]" >&2
  exit 1
fi

if [[ -z "${UNITY_PATH}" ]]; then
  echo "UNITY_PATH not provided. Set as arg2 or env UNITY_PATH. Example:" >&2
  echo "/Applications/Unity/Hub/Editor/2021.3.41f1/Unity.app/Contents/MacOS/Unity" >&2
  exit 1
fi

if [[ ! -x "${UNITY_PATH}" ]]; then
  echo "UNITY_PATH not executable: ${UNITY_PATH}" >&2
  exit 1
fi

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)
PACKAGES_DIR="${REPO_ROOT}/Packages"

echo "[1/4] Creating Unity project at: ${PROJECT_PATH}"
"${UNITY_PATH}" -batchmode -nographics -quit -createProject "${PROJECT_PATH}" || true

MANIFEST="${PROJECT_PATH}/Packages/manifest.json"
if [[ ! -f "${MANIFEST}" ]]; then
  echo "manifest.json not found at ${MANIFEST}" >&2
  exit 1
fi

echo "[2/4] Linking local Nexo packages via file: dependencies"
PYTHON_SCRIPT=$(cat << 'PY'
import json, os, sys
manifest_path = sys.argv[1]
repo_packages = sys.argv[2]

with open(manifest_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

deps = data.setdefault('dependencies', {})

def add_dep(name, folder):
    path = os.path.join(repo_packages, folder)
    deps[name] = f"file:{path}"

add_dep('com.nexo.agent.unity', 'com.nexo.agent.unity')
add_dep('com.nexo.agent.tools', 'com.nexo.agent.tools')
add_dep('com.nexo.agent.validation', 'com.nexo.agent.validation')

# Ensure testables includes validation package
testables = data.setdefault('testables', [])
if 'com.nexo.agent.validation' not in testables:
    testables.append('com.nexo.agent.validation')

with open(manifest_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2)
    f.write('\n')
PY
)

python3 - <<PY
${PYTHON_SCRIPT}
PY
"${MANIFEST}" "${PACKAGES_DIR}"

echo "[3/4] Setting NEXO_AI_MODE=off in ProjectSettings to ensure offline runs"
# Create a simple ProjectSettings file for environment hint (Unity doesn’t read env here, but kept for docs)
mkdir -p "${PROJECT_PATH}/ProjectSettings"
echo "# Nexo offline hint\nNEXO_AI_MODE=off" > "${PROJECT_PATH}/ProjectSettings/Nexo.env"

echo "[4/4] Opening project to import packages and capture logs"
LOG_DIR="${PROJECT_PATH}/Logs"
mkdir -p "${LOG_DIR}"
BOOT_LOG="${LOG_DIR}/Import.log"
"${UNITY_PATH}" -batchmode -nographics -quit -projectPath "${PROJECT_PATH}" -logFile "${BOOT_LOG}" || true

echo "--- Parent/import summary ---"
if grep -E "(Failed to resolve packages|Error adding package|Package Manager|[Ee]rror:)" -n "${BOOT_LOG}" >/dev/null 2>&1; then
  echo "Detected package/import errors. Attempting automatic recovery (reset packages cache)."
  # Attempt a recovery: delete Library/PackageCache and Packages/packages-lock.json then reimport
  rm -rf "${PROJECT_PATH}/Library/PackageCache" || true
  rm -f "${PROJECT_PATH}/Packages/packages-lock.json" || true
  echo "Re-importing project after cleanup..."
  "${UNITY_PATH}" -batchmode -nographics -quit -projectPath "${PROJECT_PATH}" -logFile "${BOOT_LOG}.retry" || true
  if grep -E "(Failed to resolve packages|Error adding package|Package Manager|[Ee]rror:)" -n "${BOOT_LOG}.retry" >/dev/null 2>&1; then
    echo "Import still shows errors. See logs: ${BOOT_LOG} and ${BOOT_LOG}.retry"
    echo "Tail (retry):"
    tail -n 200 "${BOOT_LOG}.retry" || true
    exit 2
  else
    echo "Recovery succeeded. Import completed without detected package errors."
  fi
else
  echo "Import completed without detected package errors."
fi

echo "Done. Open the project in Unity and go to Nexo → Agent Workbench."


