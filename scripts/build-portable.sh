#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "==> Building portable (netstandard2.0) targets"
echo "Root: $ROOT"
echo

projects=(
  "$ROOT/src/Nexo.Abstractions/Nexo.Abstractions.csproj"
  "$ROOT/src/Nexo.Core.Domain/Nexo.Core.Domain.csproj"
  "$ROOT/src/Nexo.Core/Nexo.Core.csproj"
  "$ROOT/src/Nexo.Core.Application/Nexo.Core.Application.csproj"
  "$ROOT/src/Nexo.Policies/Nexo.Policies.csproj"
  "$ROOT/src/Nexo.Runtime/Nexo.Runtime.csproj"
  "$ROOT/src/Nexo.Adapters.Models/Nexo.Adapters.Models.csproj"
  "$ROOT/src/Nexo.Core.UI/Nexo.Core.UI.csproj"
  "$ROOT/src/Nexo.Core.UI.Unity/Nexo.Core.UI.Unity.csproj"
)

for p in "${projects[@]}"; do
  echo "==> $p"
  dotnet build "$p" -c Release -f netstandard2.0 -v minimal
done

echo
echo "✅ Portable build complete"

