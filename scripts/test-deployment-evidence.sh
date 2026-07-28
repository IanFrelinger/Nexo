#!/usr/bin/env bash
set -euo pipefail
ROOT="${NEXO_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
digest="sha256:$(printf '0%.0s' {1..64})"
cat >"$tmp/deployment_evidence.jsonl" <<EOF
{"schema_version":"1.0.0","record_type":"deployment_evidence","deployment_run_id":"test","started_at":"2026-01-01T00:00:00Z","finished_at":"2026-01-01T00:00:01Z","outcome":"PASS","fail_reason":null,"host":{"filesystem":"ext4","disk_free_gb":100,"ram_total_gb":16,"gpu":null,"selected_profile":"test","preflight_pass":true},"images":[{"name":"agent","ref":"example@${digest}","digest":"${digest}"}],"tests":[],"onboarding_run_ids":["p12"]}
EOF
cat >"$tmp/onboarding_runs.jsonl" <<'EOF'
{"schema_version":"1.0.0","record_type":"onboarding_run","onboarding_run_id":"p12","deployment_run_id":"test","project":{"id":"P12"},"timing":{"time_to_valid_rows_s":1},"adapter":{"source":"deterministic"},"output":{"rows_valid":25,"oracle":{"pass":true}},"triage":{"verdict":"qt-peripheral","correct":true},"outcome":"PASS"}
EOF
python3 "$ROOT/scripts/validate_evidence.py" --schema-dir "$ROOT/schemas" "$tmp/deployment_evidence.jsonl" "$tmp/onboarding_runs.jsonl"
python3 "$ROOT/scripts/rollup_onboarding_evidence.py" "$tmp/onboarding_runs.jsonl" --output "$tmp/rollup.json"
grep -q '"oracle_pass_rate": 1.0' "$tmp/rollup.json"
