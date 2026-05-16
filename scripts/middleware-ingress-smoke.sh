#!/usr/bin/env bash
# Smoke checks for middleware ingress (correlation, catalog, Swagger, optional SMS lab, optional SNS webhook).
# Usage from repo root against a running Nexo.API:
#   NEXO_BASE_URL=http://127.0.0.1:8080 ./scripts/middleware-ingress-smoke.sh
# Optional: RUN_SMS_SMOKE=1 and/or RUN_SNS_SMOKE=1 when the matching feature flags are enabled on the server.
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

echo "== GET /api/middleware/ingress-context (with X-Correlation-Id) =="
curl -fsS -H "X-Correlation-Id: $CID" "$BASE/api/middleware/ingress-context"
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

if [[ "${RUN_SNS_SMOKE:-}" == "1" ]]; then
  echo "== POST /api/ingress/sms/sns (requires EnableAwsSnsSmsWebhook; valid SNS signature unless Testing skip is enabled) =="
  MSG_ID="SM-sns-smoke-$(date +%s)"
  curl -fsS -X POST "$BASE/api/ingress/sms/sns" \
    -H "Content-Type: application/json" \
    -d "{\"Type\":\"Notification\",\"MessageId\":\"$MSG_ID\",\"TopicArn\":\"arn:aws:sns:us-east-1:123456789012:smoke\",\"Message\":\"YES smoke-sns-token\",\"Timestamp\":\"2024-01-01T00:00:00.000Z\",\"SignatureVersion\":\"2\",\"Signature\":\"dGVzdA==\",\"SigningCertURL\":\"https://sns.us-east-1.amazonaws.com/SimpleNotificationService-nonexistent.pem\"}"
  echo ""
fi

echo "Done."
