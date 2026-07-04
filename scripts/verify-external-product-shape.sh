#!/usr/bin/env bash
# Verifies the external product consumption shape: authored brick + thin host + HTTP client,
# restoring only from a temp local Nexo.* feed (+ nuget.org) with no repo project references.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${NEXO_EXTERNAL_PRODUCT_VERIFY_VERSION:-9.9.9-local}"
WORK="${NEXO_EXTERNAL_PRODUCT_VERIFY_WORK:-$(mktemp -d)}"
FEED="${WORK}/feed"
TOOL_PATH="${WORK}/tools"
CONSUMER_ROOT="${WORK}/consumer"
CFG="${WORK}/NuGet.Config"
HOST_PORT="${NEXO_EXTERNAL_PRODUCT_VERIFY_PORT:-0}"
WAIT_SECS="${NEXO_EXTERNAL_PRODUCT_VERIFY_WAIT_SECS:-120}"
SOURCE_KEY="${NEXO_VERIFY_SOURCE_KEY:-nexo-local}"
ISOL_CLEANUP=""
USE_PUBLISHED_FEED=0
PROBE_BRICK_SOURCE="${NEXO_EXTERNAL_PRODUCT_PROBE_BRICK_SOURCE:-}"
if [[ -n "${NEXO_EXTERNAL_PRODUCT_PROBE_BRICK:-}" && -z "${PROBE_BRICK_SOURCE}" ]]; then
  PROBE_BRICK_SOURCE="${ROOT}/spikes/portability/generated/ErrorSummaryExtractorBrick"
fi
USE_PROBE_BRICK=0
if [[ -n "${PROBE_BRICK_SOURCE}" ]]; then
  USE_PROBE_BRICK=1
fi

if [[ -n "${NEXO_EXTERNAL_PRODUCT_PACKAGE_FEED:-}" ]]; then
  USE_PUBLISHED_FEED=1
  FEED="${NEXO_EXTERNAL_PRODUCT_PACKAGE_FEED}"
fi

mkdir -p "${TOOL_PATH}" "${CONSUMER_ROOT}"
if [[ "${USE_PUBLISHED_FEED}" -eq 0 ]]; then
  mkdir -p "${FEED}"
fi

pack() {
  local project="$1"
  echo "==> dotnet pack ${project}"
  dotnet pack "${ROOT}/${project}" \
    -c Release \
    -o "${FEED}" \
    -p:PackageVersion="${VERSION}" \
    -p:IncludeTestProjectReferences=false \
    -v minimal
}

echo "==> Packing consumer surface as version ${VERSION} into ${FEED}"
if [[ "${USE_PUBLISHED_FEED}" -eq 1 ]]; then
  echo "==> Using published feed at ${FEED}; skipping local pack."
else
bash "${ROOT}/scripts/pack-nexo-hosting-graph.sh" "${VERSION}" "${FEED}"
pack src/Nexo.Authoring/Nexo.Authoring.csproj
pack src/Nexo.Sdk/Nexo.Sdk.csproj
pack src/Nexo.Client/Nexo.Client.csproj

# CLI + dependencies for `nexo new brick` (same extras as verify-standalone-brick-authoring.sh).
if [[ "${USE_PROBE_BRICK}" -eq 0 ]]; then
pack src/Nexo.Adapters.Models/Nexo.Adapters.Models.csproj
pack src/Nexo.Bricks.Owasp/Nexo.Bricks.Owasp.csproj
pack src/Nexo.BackgroundAgents.HostRunners/Nexo.BackgroundAgents.HostRunners.csproj
pack src/Nexo.Policies.Dev/Nexo.Policies.Dev.csproj
pack application/src/Nexo.CLI/Nexo.CLI.csproj
fi
fi

if [[ -z "${NEXO_EXTERNAL_PRODUCT_VERIFY_NO_ISOLATED_CACHE:-}" ]]; then
  ISOL_BASE="${NEXO_EXTERNAL_PRODUCT_VERIFY_ISOLATED_ROOT:-$(mktemp -d "${WORK}/nuget-cache-XXXXXX")}"
  ISOL_CLEANUP="${ISOL_BASE}"
  mkdir -p "${ISOL_BASE}/packages" "${ISOL_BASE}/cli-home"
  export NUGET_PACKAGES="${ISOL_BASE}/packages"
  export DOTNET_CLI_HOME="${ISOL_BASE}/cli-home"
  echo "Isolated restore: NUGET_PACKAGES=${NUGET_PACKAGES} DOTNET_CLI_HOME=${DOTNET_CLI_HOME}"
fi

if [[ -n "${NEXO_NUGET_USERNAME:-}" && -n "${NEXO_NUGET_PASSWORD:-}" ]]; then
  cat > "${CFG}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="${SOURCE_KEY}" value="${FEED}" protocolVersion="3" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials>
    <${SOURCE_KEY}>
      <add key="Username" value="${NEXO_NUGET_USERNAME}" />
      <add key="ClearTextPassword" value="${NEXO_NUGET_PASSWORD}" />
    </${SOURCE_KEY}>
  </packageSourceCredentials>
</configuration>
EOF
else
  cat > "${CFG}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="${SOURCE_KEY}" value="${FEED}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF
fi

if [[ "${USE_PROBE_BRICK}" -eq 0 ]]; then
dotnet tool install \
  --tool-path "${TOOL_PATH}" \
  Nexo.CLI \
  --version "${VERSION}" \
  --add-source "${FEED}" \
  --ignore-failed-sources
fi

BRICK_OUT="${CONSUMER_ROOT}/brick"
mkdir -p "${BRICK_OUT}"

BRICK_ID="intensity"
BRICK_CLASS="IntensityBrick"
BRICK_NAMESPACE="IntensityBrick"
BRICK_PROJECT_NAME="IntensityBrick"
BRICK_PROJECT_DIR="${BRICK_OUT}/IntensityBrick"

if [[ "${USE_PROBE_BRICK}" -eq 1 ]]; then
  BRICK_ID="error-summary-extractor"
  BRICK_CLASS="ErrorSummaryExtractorBrick"
  BRICK_NAMESPACE="ErrorSummaryExtractorBrick"
  BRICK_PROJECT_NAME="ErrorSummaryExtractorBrick"
  BRICK_PROJECT_DIR="${BRICK_OUT}/ErrorSummaryExtractorBrick"
  if [[ ! -f "${PROBE_BRICK_SOURCE}/ErrorSummaryExtractorBrick.cs" ]]; then
    echo "Probe brick source not found at ${PROBE_BRICK_SOURCE}" >&2
    exit 1
  fi
  echo "==> Copying generated probe brick from ${PROBE_BRICK_SOURCE}"
  mkdir -p "${BRICK_PROJECT_DIR}"
  cp "${PROBE_BRICK_SOURCE}/ErrorSummaryExtractorBrick.cs" "${BRICK_PROJECT_DIR}/ErrorSummaryExtractorBrick.cs"
  cat > "${BRICK_PROJECT_DIR}/ErrorSummaryExtractorBrick.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Nexo.Brick.Contracts" Version="${VERSION}" />
    <PackageReference Include="Nexo.Authoring" Version="${VERSION}" />
  </ItemGroup>
</Project>
EOF
else
echo "==> Scaffolding authored brick via nexo new brick"
"${TOOL_PATH}/nexo" new brick Intensity \
  --output "${BRICK_OUT}" \
  --nexo-version "${VERSION}" \
  --json >/dev/null

INTENSITY_BRICK_CS="${BRICK_OUT}/IntensityBrick/IntensityBrick.cs"
cat > "${INTENSITY_BRICK_CS}" <<'CS'
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace IntensityBrick;

/// <summary>
/// Throwaway intensity echo/transform brick for external product-shape verification.
/// </summary>
public sealed class IntensityBrick : Brick
{
    public IntensityBrick()
    {
        Id = "intensity";
        Name = "Intensity Brick";
        Version = "1.0.0";
        Icon = "📈";
        Category = BrickCategory.Transform;
        Description = "Deterministic intensity transform (input * 2).";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("intensity", "int", "Input intensity level")
            ],
            Outputs =
            [
                new BrickOutputDefinition("result", "int", "Transformed intensity")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var intensity = Convert.ToInt32(input.ToDictionary()["intensity"]);
        var transformed = intensity * 2;
        var output = new BrickOutput
        {
            Summary = $"Transformed intensity {intensity} -> {transformed}"
        };
        output.Set("result", transformed);
        return Task.FromResult(output);
    }
}
CS
fi

HOST_DIR="${CONSUMER_ROOT}/host/ExternalProductHost"
CLIENT_DIR="${CONSUMER_ROOT}/client/ExternalProductClient"
mkdir -p "${HOST_DIR}" "${CLIENT_DIR}"

cat > "${HOST_DIR}/ExternalProductHost.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Nexo.Authoring" Version="${VERSION}" />
    <PackageReference Include="Nexo.Hosting.Bundle" Version="${VERSION}" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../brick/${BRICK_PROJECT_NAME}/${BRICK_PROJECT_NAME}.csproj" />
  </ItemGroup>
</Project>
EOF

cat > "${HOST_DIR}/Program.cs" <<'HOSTCS'
using Nexo.Authoring;
using Nexo.Brick.Contracts;
using Nexo.Core.Application.Bricks;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Hosting;
using Nexo.Infrastructure.Execution;
HOSTCS

if [[ "${USE_PROBE_BRICK}" -eq 1 ]]; then
  cat >> "${HOST_DIR}/Program.cs" <<HOSTCS
using ${BRICK_NAMESPACE};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNexoBrick<${BRICK_NAMESPACE}.${BRICK_CLASS}>();
HOSTCS
else
  cat >> "${HOST_DIR}/Program.cs" <<'HOSTCS'
using IntensityBrick;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNexoBrick<IntensityBrick.IntensityBrick>();
HOSTCS
fi

cat >> "${HOST_DIR}/Program.cs" <<'HOSTCS'
builder.Services.AddNexo(options =>
{
    options.RegisterBackgroundAgentHostedService = false;
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

app.MapPost("/api/bricks/{brickId}/execute", async (
    string brickId,
    BrickExecuteRequestDto request,
    IBrickRegistry brickRegistry,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(brickId))
        return Results.BadRequest(new { title = "brickId is required" });
    if (request is null)
        return Results.BadRequest(new { title = "Request body is required" });
    if (!string.IsNullOrEmpty(request.BrickId) &&
        !string.Equals(request.BrickId, brickId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { title = "BrickId in body must match route" });
    }

    var brick = brickRegistry.GetBrick(brickId);
    if (brick is null)
        return Results.NotFound();

    if (!Enum.TryParse<ImplementationType>(request.Implementation, true, out var implementation))
        implementation = ImplementationType.Deterministic;

    var context = ToExecutionContext(request.ExecutionContext);
    var input = BrickValueSerializer.FromWireToBrickInput(request.Input);
    BrickInputDefaults.Apply(brick, input);

    try
    {
        var output = await brick.ExecuteAsync(input, implementation, context, cancellationToken).ConfigureAwait(false);
        return Results.Ok(new BrickExecuteResponseDto
        {
            Success = true,
            Summary = output.Summary,
            Output = BrickValueSerializer.ToWireDictionary(output)
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new BrickExecuteResponseDto
        {
            Success = false,
            Error = ex.Message
        });
    }
});

app.Run();

static Nexo.Infrastructure.Execution.ExecutionContext ToExecutionContext(ExecutionContextDto? dto)
{
    if (dto is null)
        return new Nexo.Infrastructure.Execution.ExecutionContext();

    return new Nexo.Infrastructure.Execution.ExecutionContext
    {
        AgentId = dto.AgentId ?? string.Empty,
        BehaviorId = dto.BehaviorId ?? string.Empty,
        IsAirGapped = dto.IsAirGapped,
        AuditMode = dto.AuditMode,
        Provider = dto.Provider ?? "openai",
        Variables = dto.Variables is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(dto.Variables)
    };
}
HOSTCS

cat > "${CLIENT_DIR}/ExternalProductClient.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Nexo.Sdk" Version="${VERSION}" />
  </ItemGroup>
</Project>
EOF

if [[ "${USE_PROBE_BRICK}" -eq 1 ]]; then
  cp "${ROOT}/spikes/portability/templates/ExternalProductProbeClient.cs" "${CLIENT_DIR}/Program.cs"
else
cat > "${CLIENT_DIR}/Program.cs" <<'CLIENTCS'
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Client;
using Nexo.Sdk.Client;

if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: ExternalProductClient <hostBaseUrl>");
    Environment.Exit(2);
}

var hostBaseUrl = args[0].TrimEnd('/');
var services = new ServiceCollection();
services.AddNexoClientSdk(hostBaseUrl);
await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<INexoClient>();

var requestBody = new Dictionary<string, object?>
{
    ["implementation"] = "Deterministic",
    ["input"] = new Dictionary<string, object> { ["intensity"] = 21 }
};

var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
using var content = JsonContent.Create(requestBody, options: jsonOptions);

using var response = await client.InvokeAsync(
    HttpMethod.Post,
    "api/bricks/intensity/execute",
    content).ConfigureAwait(false);

response.EnsureSuccessStatusCode();
await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

if (!doc.RootElement.TryGetProperty("success", out var successEl) || !successEl.GetBoolean())
{
    var error = doc.RootElement.TryGetProperty("error", out var errEl) ? errEl.GetString() : "unknown";
    Console.Error.WriteLine($"Brick execute failed: {error}");
    Environment.Exit(1);
}

if (!doc.RootElement.TryGetProperty("output", out var outputEl) ||
    !outputEl.TryGetProperty("result", out var resultEl) ||
    resultEl.ValueKind != JsonValueKind.Number ||
    resultEl.GetInt32() != 42)
{
    Console.Error.WriteLine($"Unexpected brick output: {doc.RootElement.GetRawText()}");
    Environment.Exit(1);
}

Console.WriteLine("external-product-client: brick round-trip OK (intensity 21 -> result 42)");
CLIENTCS
fi

SLN="${CONSUMER_ROOT}/ExternalProduct.sln"
dotnet new sln -n ExternalProduct -o "${CONSUMER_ROOT}" --force
dotnet sln "${SLN}" add \
  "${BRICK_PROJECT_DIR}/${BRICK_PROJECT_NAME}.csproj" \
  "${HOST_DIR}/ExternalProductHost.csproj" \
  "${CLIENT_DIR}/ExternalProductClient.csproj"

echo "==> Restoring consumer solution from temp feed + nuget.org only"
dotnet restore "${SLN}" \
  --configfile "${CFG}" \
  --force-evaluate \
  -v minimal

echo "==> Building consumer solution"
dotnet build "${SLN}" \
  -c Release \
  --no-restore \
  -v minimal

if rg "Nexo\\.Core\\.Domain\\.csproj|/workspace|src/Nexo" "${CONSUMER_ROOT}" >/dev/null; then
  echo "Generated consumer tree contains repo-relative Nexo paths." >&2
  rg "Nexo\\.Core\\.Domain\\.csproj|/workspace|src/Nexo" "${CONSUMER_ROOT}" >&2
  exit 1
fi

if [[ "${HOST_PORT}" == "0" ]]; then
  HOST_PORT="$(python3 - <<'PY'
import socket
s = socket.socket()
s.bind(("127.0.0.1", 0))
print(s.getsockname()[1])
s.close()
PY
)"
fi

HOST_URL="http://127.0.0.1:${HOST_PORT}"
HOST_LOG="${WORK}/host.log"
HOST_PID=""

cleanup() {
  if [[ -n "${HOST_PID}" ]] && kill -0 "${HOST_PID}" 2>/dev/null; then
    kill "${HOST_PID}" 2>/dev/null || true
    wait "${HOST_PID}" 2>/dev/null || true
  fi
  if [[ -n "${ISOL_CLEANUP}" && -d "${ISOL_CLEANUP}" ]]; then
    rm -rf "${ISOL_CLEANUP}"
  fi
}
trap cleanup EXIT

echo "==> Starting thin host at ${HOST_URL}"
ASPNETCORE_URLS="${HOST_URL}" \
  dotnet run --project "${HOST_DIR}/ExternalProductHost.csproj" \
    -c Release \
    --no-build \
    >"${HOST_LOG}" 2>&1 &
HOST_PID=$!

start_ts=$(date +%s)
until curl -fsS "${HOST_URL}/health" >/dev/null 2>&1; do
  if ! kill -0 "${HOST_PID}" 2>/dev/null; then
    echo "Host process exited before /health became ready." >&2
    cat "${HOST_LOG}" >&2 || true
    exit 1
  fi
  now_ts=$(date +%s)
  if (( now_ts - start_ts > WAIT_SECS )); then
    echo "Timeout waiting for ${HOST_URL}/health after ${WAIT_SECS}s" >&2
    cat "${HOST_LOG}" >&2 || true
    exit 1
  fi
  sleep 1
done

echo "GET ${HOST_URL}/health OK"

echo "==> Running HTTP client against hosted brick"
dotnet run --project "${CLIENT_DIR}/ExternalProductClient.csproj" \
  -c Release \
  --no-build \
  -- "${HOST_URL}"

echo "verify-external-product-shape: OK (${WORK})"
