#!/usr/bin/env bash
# Smoke checks for middleware ingress (correlation, catalog, Swagger, optional SMS lab).
# Usage from repo root against a running Nexo.API:
#   NEXO_BASE_URL=http://127.0.0.1:8080 ./scripts/middleware-ingress-smoke.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASE="${NEXO_BASE_URL:-http://127.0.0.1:8080}"

echo "== Base: $BASE =="

echo "== GET /health =="
curl -fsS "$BASE/health" | head -c 200 || true
echo ""

echo "== GET /api/middleware/ingress-catalog =="
curl -fsS "$BASE/api/middleware/ingress-catalog" | head -c 400 || true
echo ""

CID="smoke-$(date +%s)"
echo "== GET /api/middleware/correlation-echo (X-Correlation-Id: $CID) =="
curl -fsS -H "X-Correlation-Id: $CID" "$BASE/api/middleware/correlation-echo"
echo ""

echo "== GET /swagger/v1/swagger.json (grep IngressCorrelationEcho) =="
curl -fsS "$BASE/swagger/v1/swagger.json" | grep -q "IngressCorrelationEcho" && echo "ok: swagger mentions IngressCorrelationEcho"

if [[ "${RUN_SMS_SMOKE:-}" == "1" ]]; then
  echo "== POST /api/ingress/sms/simulate (requires EnableSmsSimulationIngress on server) =="
  curl -fsS -X POST "$BASE/api/ingress/sms/simulate" \
    -H "Content-Type: application/json" \
    -d '{"from":"+15555550100","body":"YES smoke-token","messageSid":"SM-smoke-1"}'
  echo ""
fi

echo "Done."
