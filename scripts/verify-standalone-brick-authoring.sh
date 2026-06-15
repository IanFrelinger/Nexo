#!/usr/bin/env bash
# Verifies `nexo new brick` works from a tool install outside a Nexo checkout.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${NEXO_AUTHORING_VERIFY_VERSION:-9.9.9-local}"
WORK="${NEXO_AUTHORING_VERIFY_WORK:-$(mktemp -d)}"
FEED="${WORK}/feed"
TOOL_PATH="${WORK}/tools"
BRICK_OUT="${WORK}/standalone"

mkdir -p "${FEED}" "${TOOL_PATH}" "${BRICK_OUT}"

pack() {
  local project="$1"
  dotnet pack "${ROOT}/${project}" \
    -c Release \
    -o "${FEED}" \
    -p:PackageVersion="${VERSION}" \
    -p:IncludeTestProjectReferences=false \
    -v minimal
}

bash "${ROOT}/scripts/pack-nexo-hosting-graph.sh" "${VERSION}" "${FEED}"
pack src/Nexo.Adapters.Models/Nexo.Adapters.Models.csproj
pack src/Nexo.Bricks.Owasp/Nexo.Bricks.Owasp.csproj
pack src/Nexo.BackgroundAgents.HostRunners/Nexo.BackgroundAgents.HostRunners.csproj
pack src/Nexo.Policies.Dev/Nexo.Policies.Dev.csproj
pack src/Nexo.Authoring/Nexo.Authoring.csproj
pack application/src/Nexo.CLI/Nexo.CLI.csproj

dotnet tool install \
  --tool-path "${TOOL_PATH}" \
  Nexo.CLI \
  --version "${VERSION}" \
  --add-source "${FEED}" \
  --ignore-failed-sources

"${TOOL_PATH}/nexo" new brick SampleThing \
  --output "${BRICK_OUT}" \
  --nexo-version "${VERSION}" \
  --json

if rg "Nexo\\.Core\\.Domain\\.csproj|/workspace|src/Nexo" "${BRICK_OUT}" >/dev/null; then
  echo "Generated brick contains repo-relative Nexo paths." >&2
  rg "Nexo\\.Core\\.Domain\\.csproj|/workspace|src/Nexo" "${BRICK_OUT}" >&2
  exit 1
fi

dotnet restore "${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj" \
  --source "${FEED}" \
  --source https://api.nuget.org/v3/index.json

dotnet build "${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj" \
  --no-restore \
  -v minimal

dotnet test "${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj" \
  --no-build \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo "verify-standalone-brick-authoring: OK (${WORK})"
