#!/usr/bin/env bash
# Pack Nexo.Hosting to a local directory, restore StableSdkHostSample.Package.csproj against that feed only (+ nuget.org for dependencies), and build.
#
# Optional: NEXO_SDK_PACKAGE_FEED — folder of *.nupkg (skip pack). By default uses empty NUGET_PACKAGES + DOTNET_CLI_HOME
# (set NEXO_SDK_VERIFY_NO_ISOLATED_CACHE=1 to opt out). Restore uses --force-evaluate.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${NEXO_SDK_PACKAGE_VERSION:-1.0.0-ci}"
FEED="${NEXO_SDK_PACKAGE_FEED:-}"
OUT="${ROOT}/artifacts/nuget-verify/packages"
CFG_DIR="${ROOT}/artifacts/nuget-verify"
CFG="${CFG_DIR}/NuGet.Config"
ISOL_CLEANUP=""

if [[ -n "${FEED}" ]]; then
  [[ -d "${FEED}" ]] || { echo "NEXO_SDK_PACKAGE_FEED not a directory: ${FEED}" >&2; exit 1; }
  OUT="$(cd "${FEED}" && pwd)"
  echo "Using pre-packed feed at ${OUT}; skipping pack."
else
  rm -rf "${OUT}"
  mkdir -p "${OUT}" "${CFG_DIR}"
  echo "Packing Nexo.Hosting dependency graph as version ${VERSION}..."
  bash "${ROOT}/scripts/pack-nexo-hosting-graph.sh" "${VERSION}" "${OUT}"
fi

mkdir -p "${CFG_DIR}"

if [[ -z "${NEXO_SDK_VERIFY_NO_ISOLATED_CACHE:-}" ]]; then
  ISOL_BASE="${NEXO_SDK_VERIFY_ISOLATED_ROOT:-}"
  if [[ -z "${ISOL_BASE}" ]]; then
    ISOL_BASE="$(mktemp -d "${CFG_DIR}/isolated-XXXXXX")"
    ISOL_CLEANUP="${ISOL_BASE}"
  fi
  mkdir -p "${ISOL_BASE}/packages" "${ISOL_BASE}/cli-home"
  export NUGET_PACKAGES="${ISOL_BASE}/packages"
  export DOTNET_CLI_HOME="${ISOL_BASE}/cli-home"
  echo "Isolated restore: NUGET_PACKAGES=${NUGET_PACKAGES}"
else
  echo "Skipping isolated cache (NEXO_SDK_VERIFY_NO_ISOLATED_CACHE)."
fi

cat > "${CFG}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nexo-local" value="${OUT}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

echo "Restoring and building package-consumption sample (NexoSdkPackageVersion=${VERSION})..."
dotnet restore "${ROOT}/docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj" \
  --configfile "${CFG}" \
  --force-evaluate \
  -p:NexoSdkPackageVersion="${VERSION}" \
  -v minimal

dotnet build "${ROOT}/docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj" \
  -c Release \
  --no-restore \
  -v minimal

echo "Running package-consumption sample..."
dotnet run --project "${ROOT}/docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj" \
  -c Release \
  --no-build

[[ -n "${ISOL_CLEANUP}" && -d "${ISOL_CLEANUP}" ]] && rm -rf "${ISOL_CLEANUP}"
echo "verify-stable-sdk-host-sample-packages: OK"
