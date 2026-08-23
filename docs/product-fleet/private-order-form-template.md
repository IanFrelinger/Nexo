# Private order form template (Phase 1.5)

Use this skeleton for the first annual **Ashlar Private** pilot. Adapt with counsel before signature.

## Order summary

| Field | Value |
|-------|-------|
| Customer legal name | |
| Billing contact | |
| Technical contact | |
| Deployment mode | **Ashlar Private** (single-tenant, customer-controlled host) |
| Contract term | 12 months |
| Renewal | Auto-renew annual unless 60-day written notice |
| Currency | USD |

## SKU line items

| Line | Qty | Unit | Annual price | Notes |
|------|-----|------|--------------|-------|
| Ashlar Private — Team Self-Host seats | | seat | per [`MonetizationProductDesign.md`](../MonetizationProductDesign.md) | Includes copilot + audit baseline |
| Priority support add-on (optional) | 1 | org | | Next-business-day email |
| Professional services — production readiness (optional) | | fixed SOW | | Deploy + policy workshop |

## Entitlements (attach as exhibit)

| Entitlement | Pilot value |
|-------------|-------------|
| Licensed tenant id | |
| Seats | |
| `MaxCopilotSubmissionsPerHour` | 0 = unlimited unless capped |
| License file expiry | |
| BYOK required | Yes (recommended) |

## Billing mechanics

1. **Invoice** — Net 30 from order form execution (Stripe Invoicing, Paddle, or manual wire).
2. **True-up** — Quarterly seat reconciliation against license `seats` field.
3. **Overage** — Not applicable for Private v1 unless PS hours exceed SOW.

## Customer obligations

- Maintain supported Docker / host OS per [`private-reference-deployment.md`](./private-reference-deployment.md)
- Store provider API keys on-host per [`private-byok-security.md`](./private-byok-security.md)
- Execute quarterly backup/restore drill per [`private-backup-restore.md`](./private-backup-restore.md)

## Vendor obligations

- Deliver pinned release artifacts and license file
- Support per [`private-support-boundaries.md`](./private-support-boundaries.md)
