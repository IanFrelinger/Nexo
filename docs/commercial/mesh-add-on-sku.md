# Ashlar Mesh add-on (commercial SKU sketch)

Implementation-oriented outline for **Product E** ([`ProductFleetImplementationRoadmap.md`](../ProductFleetImplementationRoadmap.md) Phase 5). The virtual lab proves technical behavior; this doc maps it to sellable entitlements.

**Status:** landed commercial modules are `Ashlar.Commercial.Fleet.*` and `Ashlar.Commercial.MeshDirector`. See [`../OpenCoreBoundary.md`](../OpenCoreBoundary.md) for the authoritative open/commercial split.

## SKU: `ashlar-mesh-federation`

| Entitlement | Lab coverage | Production note |
|-------------|--------------|-----------------|
| Multi-peer director + placement | `mesh-lab-verify.sh` | Per-tenant director HA separate |
| Trust-tier placement (`trusted-only`) | `mesh-lab-verify-trust.sh` | Align with `PeerTrustPolicy` / `instances.json` |
| Worker executor loop | Worker container + verify | Customer-managed workers |
| CopilotScoped API keys | `mesh-lab-verify-entitlements.sh` | Issue per integration partner |
| Hourly copilot quota | `Ashlar:Entitlements:MaxCopilotSubmissionsPerHour` | Metering / billing hook TBD |
| Deep migrate / checkpoint | `mesh-lab-verify-deep.sh` | Lab: LiteDB on director (`mesh-lab-verify-persistence.sh`) |

## Packaging

- **Attach to Enterprise** as an add-on line item (see roadmap §5.4).
- **Requires** Private or Enterprise base (API + auth); not sold on free Cloud tier by default.
- **Order form**: seat count × mesh peer count cap (enforce in license file / `Ashlar:Product` options — future).

## Support playbook

Point operators to [`docs/runbooks/mesh-lab-operations.md`](../runbooks/mesh-lab-operations.md) for reproducing customer issues locally.
