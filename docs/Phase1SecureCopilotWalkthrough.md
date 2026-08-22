# Phase 1 secure copilot walkthrough

This walkthrough demonstrates the Phase 1 product pilot flow on top of the existing Ashlar API portal.

## Prerequisites

- Start the API host (`application/src/Ashlar.API`) with a workspace mounted via `deploy/compose/docker-compose.agent-server.yml`, or run it locally with equivalent environment.
- Ensure trust services are enabled if you want live boundary controls and audit history:
  - `ASHLAR_TRUST_ENABLED=1`
  - optional persistence settings for trust/audit stores.

## First success flow

1. Open the portal at `/` and verify API connectivity pill is green.
2. Submit a task from **Secure copilot task flow**:
   - Enter a concrete coding task.
   - Click `Run task`.
3. Confirm response includes:
   - Task success + summary.
   - Trust pause state.
   - Recent audit events.
4. Validate **Trust + runtime controls**:
   - Pause and resume trust observation.
   - Add an allow/deny category or source rule.
   - Confirm changes reflected in trust dashboard.
5. Validate **Knowledge query**:
   - Run with default sources (`Adaptation,Pattern,UserKnowledge`).
   - Confirm paginated results include provenance metadata.
6. Validate **Background agents** summary:
   - Confirm mode + total/running counts render.

## API surfaces used by this flow

- `POST /api/copilot/task`
- `GET /api/trust/dashboard`
- `POST /api/trust/pause`
- `POST /api/trust/rule`
- `GET /api/knowledge/query`
- `GET /api/background-agents/summary`
- `GET /api/status`

