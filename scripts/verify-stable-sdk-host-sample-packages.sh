#!/usr/bin/env bash
# Pack Nexo.Hosting to a local directory, restore StableSdkHostSample.Package.csproj against that feed only (+ nuget.org for dependencies), and build.
#
# Optional: NEXO_SDK_PACKAGE_FEED=/path/to/folder-of-nupkg skips pack-nexo-hosting-graph (e.g. unpacked CI artifact).
#
# By default uses an empty NUGET_PACKAGES + DOTNET_CLI_HOME so restore cannot pick up stale packages from
# your user/global cache (closer to a first-time consumer). Set NEXO_SDK_VERIFY_NO_ISOLATED_CACHE=1 to skip.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${NEXO_SDK_PACKAGE_VERSION:-1.0.0-ci}"
FEED="${NEXO_SDK_PACKAGE_FEED:-}"
OUT="${ROOT}/artifacts/nuget-verify/packages"
CFG_DIR="${ROOT}/artifacts/nuget-verify"
CFG="${CFG_DIR}/NuGet.Config"
ISOL_CLEANUP=""

if [[ -n "${FEED}" ]]; then
  if [[ ! -d "${FEED}" ]]; then
    echo "NEXO_SDK_PACKAGE_FEED is not a directory: ${FEED}" >&2
    exit 1
  fi
  OUT="$(cd "${FEED}" && pwd)"
  echo "Using pre-packed feed at ${OUT} (version ${VERSION}); skipping pack-nexo-hosting-graph."
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
  echo "Isolated restore: NUGET_PACKAGES=${NUGET_PACKAGES} DOTNET_CLI_HOME=${DOTNET_CLI_HOME}"
else
  echo "Skipping isolated package cache (NEXO_SDK_VERIFY_NO_ISOLATED_CACHE is set)."
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

if [[ -n "${ISOL_CLEANUP}" && -d "${ISOL_CLEANUP}" ]]; then
  rm -rf "${ISOL_CLEANUP}"
fi

echo "verify-stable-sdk-host-sample-packages: OK"
