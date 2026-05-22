# Rollback drill v1

## Procedure

1. Record current deploy: image tag/digest or package version, git SHA.
2. Deploy previous known-good artifact to staging (or local mesh-lab stack).
3. Run smoke: `dotnet run --project application/src/Nexo.CLI -- doctor`, pipeline validate, API `/health`.
4. If stateful: restore LiteDB/volume from backup taken before step 1.
5. Document elapsed time and data loss window.

## Last drill

| Field | Value |
|-------|-------|
| Date | _not yet recorded_ |
| Operator | |
| Environment | staging / mesh-lab / local |
| From version | |
| To version | |
| RPO observed | |
| RTO observed | |
| Result | |

## References

- [Release and promotion](ReleaseAndPromotion.md)
- `make dr-gate-full`
