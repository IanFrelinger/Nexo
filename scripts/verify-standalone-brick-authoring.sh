#!/usr/bin/env bash
# Verifies `ashlar new brick` works from a tool install outside a Ashlar checkout.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${ASHLAR_AUTHORING_VERIFY_VERSION:-9.9.9-local}"
WORK="${ASHLAR_AUTHORING_VERIFY_WORK:-$(mktemp -d)}"
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

bash "${ROOT}/scripts/pack-ashlar-hosting-graph.sh" "${VERSION}" "${FEED}"
pack src/Ashlar.Adapters.Models/Ashlar.Adapters.Models.csproj
pack src/Ashlar.Bricks.Owasp/Ashlar.Bricks.Owasp.csproj
pack src/Ashlar.BackgroundAgents.HostRunners/Ashlar.BackgroundAgents.HostRunners.csproj
pack src/Ashlar.Policies.Dev/Ashlar.Policies.Dev.csproj
pack src/Ashlar.Authoring/Ashlar.Authoring.csproj
pack application/src/Ashlar.CLI/Ashlar.CLI.csproj

dotnet tool install \
  --tool-path "${TOOL_PATH}" \
  Ashlar.CLI \
  --version "${VERSION}" \
  --add-source "${FEED}" \
  --ignore-failed-sources

"${TOOL_PATH}/ashlar" new brick SampleThing \
  --output "${BRICK_OUT}" \
  --ashlar-version "${VERSION}" \
  --json

if rg "Ashlar\\.Core\\.Domain\\.csproj|/workspace|src/Ashlar" "${BRICK_OUT}" >/dev/null; then
  echo "Generated brick contains repo-relative Ashlar paths." >&2
  rg "Ashlar\\.Core\\.Domain\\.csproj|/workspace|src/Ashlar" "${BRICK_OUT}" >&2
  exit 1
fi

dotnet restore "${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj" \
  --source "${FEED}" \
  --source https://api.nuget.org/v3/index.json

dotnet build "${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj" \
  --no-restore \
  -v minimal

# Same class as other distribution-matrix slices: raw `dotnet test` exits 0
# when the generated project has no tests.
python3 "${ROOT}/scripts/run-dotnet-test-counted.py" \
  --project "${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj" \
  --expected-prefix "SampleThingBrick.Tests.SampleThingBrickTests." \
  --min-tests 1 \
  -- \
  --no-build \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo "verify-standalone-brick-authoring: OK (${WORK})"
