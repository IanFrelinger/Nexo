#!/usr/bin/env bash
# Pack Nexo.Hosting to a local directory, restore StableSdkHostSample.Package.csproj against that feed only (+ nuget.org for dependencies), and build.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${NEXO_SDK_PACKAGE_VERSION:-1.0.0-ci}"
OUT="${ROOT}/artifacts/nuget-verify/packages"
CFG_DIR="${ROOT}/artifacts/nuget-verify"
CFG="${CFG_DIR}/NuGet.Config"

rm -rf "${OUT}"
mkdir -p "${OUT}" "${CFG_DIR}"

echo "Packing Nexo.Hosting dependency graph as version ${VERSION}..."
bash "${ROOT}/scripts/pack-nexo-hosting-graph.sh" "${VERSION}" "${OUT}"

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

echo "verify-stable-sdk-host-sample-packages: OK"
