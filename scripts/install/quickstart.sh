#!/usr/bin/env bash
# Ashlar Quickstart — one command to a working portal.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/IanFrelinger/Ashlar/master/scripts/install/quickstart.sh | bash
#
# Or from a cloned repo:
#   bash scripts/install/quickstart.sh
#
# What it does:
#   1. Checks for Docker (preferred) or .NET SDK
#   2. Clones the repo if not already in one
#   3. Builds and starts the portal
#   4. Opens the browser
#
# The portal starts with ASHLAR_ALLOW_MOCK=1 so it works without any API keys.
# Add a real provider later via the setup wizard.
set -euo pipefail

ASHLAR_PORT="${ASHLAR_PORT:-8080}"
REPO_URL="https://github.com/IanFrelinger/Nexo.git"
INSTALL_DIR="${ASHLAR_INSTALL_DIR:-${HOME}/Ashlar}"

info()  { echo "→ $*"; }
ok()    { echo "✓ $*"; }
fail()  { echo "✗ $*" >&2; }

open_browser() {
  local url="$1"
  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "$url" 2>/dev/null &
  elif command -v open >/dev/null 2>&1; then
    open "$url" 2>/dev/null &
  elif command -v start >/dev/null 2>&1; then
    start "$url" 2>/dev/null &
  fi
}

# Detect if we're inside a Ashlar repo already
detect_repo_root() {
  local dir="${PWD}"
  while [[ "$dir" != "/" ]]; do
    if [[ -f "$dir/Ashlar.sln" && -f "$dir/global.json" ]]; then
      echo "$dir"
      return
    fi
    dir="$(dirname "$dir")"
  done
  echo ""
}

REPO_ROOT="$(detect_repo_root)"

# ── Docker path (preferred) ──────────────────────────────────────────
if command -v docker >/dev/null 2>&1; then
  info "Docker found — using container path (fastest)"

  if [[ -n "$REPO_ROOT" ]]; then
    info "Building from local repo at ${REPO_ROOT}"
    cd "$REPO_ROOT"
    docker build -f .docker/Dockerfile.quickstart -t ashlar:quickstart . || {
      fail "Docker build failed. Falling through to native path."
      REPO_ROOT=""
    }
  fi

  if [[ -z "$REPO_ROOT" ]]; then
    info "Cloning repo to build Docker image..."
    if [[ -d "$INSTALL_DIR" && -f "$INSTALL_DIR/Ashlar.sln" ]]; then
      info "Using existing repo at ${INSTALL_DIR}"
    else
      git clone --depth 1 "$REPO_URL" "$INSTALL_DIR"
    fi
    cd "$INSTALL_DIR"
    docker build -f .docker/Dockerfile.quickstart -t ashlar:quickstart .
    REPO_ROOT="$INSTALL_DIR"
  fi

  info "Starting Ashlar portal on port ${ASHLAR_PORT}..."
  # Loopback only: the quickstart image runs with no auth (README.md "Security Defaults").
  docker run -d --rm \
    --name ashlar-quickstart \
    -p "127.0.0.1:${ASHLAR_PORT}:8080" \
    ashlar:quickstart

  ok "Portal running at http://localhost:${ASHLAR_PORT}"
  info "Stop with: docker stop ashlar-quickstart"
  open_browser "http://localhost:${ASHLAR_PORT}"
  exit 0
fi

# ── Native path (fallback) ───────────────────────────────────────────
info "Docker not found — using native .NET path"

# Install .NET SDK if missing
if ! command -v dotnet >/dev/null 2>&1; then
  info "Installing .NET SDK 10..."
  if command -v curl >/dev/null 2>&1; then
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 10.0
    export PATH="${HOME}/.dotnet:${PATH}"
    rm -f /tmp/dotnet-install.sh
  elif command -v wget >/dev/null 2>&1; then
    wget -qO /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 10.0
    export PATH="${HOME}/.dotnet:${PATH}"
    rm -f /tmp/dotnet-install.sh
  else
    fail ".NET SDK not found and neither curl nor wget available to install it."
    fail "Install .NET SDK 10 from https://dot.net and rerun."
    exit 1
  fi
fi

# Verify dotnet is now available
if ! command -v dotnet >/dev/null 2>&1; then
  fail ".NET SDK installation failed. Install manually from https://dot.net"
  exit 1
fi
ok ".NET SDK $(dotnet --version) found"

# Clone or find repo
if [[ -z "$REPO_ROOT" ]]; then
  if [[ -d "$INSTALL_DIR" && -f "$INSTALL_DIR/Ashlar.sln" ]]; then
    info "Using existing repo at ${INSTALL_DIR}"
    REPO_ROOT="$INSTALL_DIR"
  else
    info "Cloning Ashlar..."
    git clone --depth 1 "$REPO_URL" "$INSTALL_DIR"
    REPO_ROOT="$INSTALL_DIR"
  fi
fi

cd "$REPO_ROOT"
info "Building API..."
dotnet build application/src/Ashlar.API/Ashlar.API.csproj -f net10.0 -v minimal

info "Starting Ashlar portal on port ${ASHLAR_PORT}..."
ASHLAR_ALLOW_MOCK=1 ASPNETCORE_URLS="http://localhost:${ASHLAR_PORT}" \
  dotnet run --project application/src/Ashlar.API -f net10.0 --no-build &
ASHLAR_PID=$!

# Bounded /health poll (up to 60s). The old `sleep 3` printed "Portal running" even when the
# host had already died (e.g. missing shared runtime), which is exactly the failure this lane
# exists to surface. Poll 127.0.0.1 explicitly: ASPNETCORE_URLS=http://localhost:PORT binds loopback.
health_url="http://127.0.0.1:${ASHLAR_PORT}/health"
probe_health() {
  if command -v curl >/dev/null 2>&1; then
    curl -fsS "$health_url" >/dev/null 2>&1
  else
    wget -qO- "$health_url" >/dev/null 2>&1
  fi
}
if ! command -v curl >/dev/null 2>&1 && ! command -v wget >/dev/null 2>&1; then
  # No HTTP client to probe with: the best we can do is confirm the process survived startup.
  sleep 3
  if ! kill -0 "${ASHLAR_PID}" 2>/dev/null; then
    fail "Portal process exited during startup. See the dotnet output above."
    exit 1
  fi
  info "curl/wget not found; skipped the ${health_url} probe (process is alive)."
else
  info "Waiting for ${health_url} (up to 60s)..."
  health_ok=0
  for _ in $(seq 1 30); do
    if ! kill -0 "${ASHLAR_PID}" 2>/dev/null; then
      break
    fi
    if probe_health; then
      health_ok=1
      break
    fi
    sleep 2
  done
  if [[ "${health_ok}" -ne 1 ]]; then
    fail "Portal did not answer at ${health_url} within 60s (PID: ${ASHLAR_PID}). See the dotnet output above."
    kill "${ASHLAR_PID}" 2>/dev/null || true
    exit 1
  fi
fi
ok "Portal running at http://localhost:${ASHLAR_PORT} (PID: ${ASHLAR_PID})"
info "Stop with: kill ${ASHLAR_PID}"
open_browser "http://localhost:${ASHLAR_PORT}"

wait "${ASHLAR_PID}" 2>/dev/null || true
