#!/usr/bin/env bash
# Create .env.mesh-lab from docs/config/mesh-lab.env.example when missing (gitignored).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TARGET="$ROOT/.env.mesh-lab"
EXAMPLE="$ROOT/docs/config/mesh-lab.env.example"

if [ -f "$TARGET" ]; then
  echo ".env.mesh-lab already exists"
  exit 0
fi

if [ ! -f "$EXAMPLE" ]; then
  echo "Missing $EXAMPLE" >&2
  exit 1
fi

cp "$EXAMPLE" "$TARGET"
sed -i.bak \
  -e 's/CHANGE_ME_LAB_API_KEY/ashlar-mesh-lab-api-key-local/g' \
  -e 's/CHANGE_ME_LAB_PEER_REGISTRATION_KEY/ashlar-mesh-lab-peer-reg-local/g' \
  -e 's/CHANGE_ME_LAB_COPILOT_SCOPED_KEY/ashlar-mesh-lab-copilot-scoped-local/g' \
  -e 's/CHANGE_ME_LAB_BEARER_OR_SAME_AS_API_KEY/ashlar-mesh-lab-api-key-local/g' \
  -e 's/CHANGE_ME_LAB_BASIC_PASSWORD/ashlar-mesh-lab-basic-local/g' \
  "$TARGET"
rm -f "$TARGET.bak"
echo "Created $TARGET from example (lab-only secrets; not for production)"
