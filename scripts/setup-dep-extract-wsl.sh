#!/usr/bin/env bash
# Sets up everything the C++ dependency-extraction + adaptation pipeline needs,
# inside WSL (or any Debian/Ubuntu host): Docker Engine, Ollama + a local code
# model, the .NET SDK, the dep-extract image (built from the vendored copy at
# tools/dep_extract), and a build of the pipeline's own projects. Safe to
# re-run — each step skips itself if already satisfied.
#
# Usage: bash scripts/setup-dep-extract-wsl.sh [--model codellama:7b] [--skip-pull]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODEL="codellama:7b"
SKIP_PULL=0

while [ $# -gt 0 ]; do
  case "$1" in
    --model)     MODEL="$2"; shift 2 ;;
    --skip-pull) SKIP_PULL=1; shift ;;
    -h|--help)   echo "usage: $0 [--model TAG] [--skip-pull]"; exit 0 ;;
    *)           echo "unknown argument: $1" >&2; exit 2 ;;
  esac
done

section() { echo; echo "== $1 =="; }

if ! grep -qi microsoft /proc/version 2>/dev/null && [ "$(uname -s)" != "Linux" ]; then
  echo "warning: this doesn't look like WSL or Linux — continuing anyway, but expect surprises" >&2
fi

section "apt prerequisites"
sudo apt-get update -qq
sudo apt-get install -y -qq curl ca-certificates zstd rsync >/dev/null

section "Docker Engine"
if command -v docker >/dev/null 2>&1 && docker version >/dev/null 2>&1; then
  echo "docker already installed and reachable"
else
  if ! command -v docker >/dev/null 2>&1; then
    curl -fsSL https://get.docker.com -o /tmp/get-docker.sh
    sudo sh /tmp/get-docker.sh
    rm -f /tmp/get-docker.sh
  fi
  sudo usermod -aG docker "$USER"
  sudo service docker start
fi

DOCKER="docker"
if ! docker version >/dev/null 2>&1; then
  echo "note: docker group membership for '$USER' isn't active in THIS shell yet (it needs a new"
  echo "      login session). Using sudo for the rest of this script; open a fresh terminal afterward"
  echo "      to use 'docker' directly without sudo."
  DOCKER="sudo docker"
fi

section "Ollama"
if command -v ollama >/dev/null 2>&1; then
  echo "ollama already installed"
else
  curl -fsSL https://ollama.com/install.sh | sh
fi
sudo service ollama start 2>/dev/null || true
for i in $(seq 1 15); do
  curl -s -m 2 http://localhost:11434/api/version >/dev/null 2>&1 && break
  sleep 1
done
if [ "$SKIP_PULL" -eq 1 ]; then
  echo "skipping model pull (--skip-pull)"
elif ollama list 2>/dev/null | awk '{print $1}' | grep -qx "$MODEL"; then
  echo "model $MODEL already pulled"
else
  ollama pull "$MODEL"
fi

section ".NET SDK 9 + runtime 8 (+ ASP.NET Core, for the GUI)"
export PATH="$HOME/.dotnet:$PATH"
ensure_dotnet_install_sh() {
  [ -x /tmp/dotnet-install.sh ] || { curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh; chmod +x /tmp/dotnet-install.sh; }
}
if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^9\.'; then
  echo ".NET 9 SDK already installed"
else
  ensure_dotnet_install_sh
  /tmp/dotnet-install.sh --channel 9.0 --install-dir "$HOME/.dotnet"
fi
if dotnet --list-runtimes 2>/dev/null | grep -q '^Microsoft\.NETCore\.App 8\.'; then
  echo ".NET 8 runtime already installed"
else
  ensure_dotnet_install_sh
  /tmp/dotnet-install.sh --channel 8.0 --runtime dotnet --install-dir "$HOME/.dotnet"
fi
if dotnet --list-runtimes 2>/dev/null | grep -q '^Microsoft\.AspNetCore\.App 8\.'; then
  echo "ASP.NET Core 8 runtime already installed"
else
  ensure_dotnet_install_sh
  /tmp/dotnet-install.sh --channel 8.0 --runtime aspnetcore --install-dir "$HOME/.dotnet"
fi
grep -q '\.dotnet:\$PATH' "$HOME/.bashrc" 2>/dev/null || echo 'export PATH="$HOME/.dotnet:$PATH"' >> "$HOME/.bashrc"

section "dep-extract Docker image"
$DOCKER build -t dep-extract "$ROOT/tools/dep_extract"

section "build the pipeline projects"
dotnet build "$ROOT/src/Nexo.Bricks.DepExtract/Nexo.Bricks.DepExtract.csproj"
dotnet build "$ROOT/src/Nexo.Bricks.DepExtract.Tests/Nexo.Bricks.DepExtract.Tests.csproj"
if [ -d "$ROOT/src/Nexo.Bricks.DepExtract.Gui" ]; then
  dotnet build "$ROOT/src/Nexo.Bricks.DepExtract.Gui/Nexo.Bricks.DepExtract.Gui.csproj"
fi

section "done"
echo "docker:  $($DOCKER --version 2>/dev/null)"
echo "ollama:  $(ollama --version 2>/dev/null) — model $MODEL"
echo "dotnet:  $(dotnet --version 2>/dev/null)"
echo
echo "Verify with the real test suite:"
echo "  cd $ROOT/src/Nexo.Bricks.DepExtract.Tests && dotnet test --filter \"FullyQualifiedName!~StressTest\""
echo
echo "Start the GUI with:"
echo "  cd $ROOT/src/Nexo.Bricks.DepExtract.Gui && dotnet run"
