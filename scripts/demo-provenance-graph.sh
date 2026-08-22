#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export NEO4J_URI="${NEO4J_URI:-bolt://localhost:7687}"
export NEO4J_USERNAME="${NEO4J_USERNAME:-neo4j}"
export NEO4J_PASSWORD="${NEO4J_PASSWORD:-provenance-graph}"
# The compose file has no shipped password: it requires NEO4J_AUTH (fails closed if unset).
# Derive it from the demo credentials above unless the caller set it explicitly.
export NEO4J_AUTH="${NEO4J_AUTH:-${NEO4J_USERNAME}/${NEO4J_PASSWORD}}"

echo "==> Starting Neo4j (docker compose)..."
docker compose -f deploy/compose/docker-compose.provenance.yml up -d

echo "==> Waiting for Neo4j bolt port..."
for i in $(seq 1 30); do
  if (echo > /dev/tcp/127.0.0.1/7687) >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

echo "==> Projecting cert artifacts and running ArtifactsUnderPolicy demo..."
dotnet run --project tools/Ashlar.Provenance.Demo/Ashlar.Provenance.Demo.csproj -- "$ROOT"
