## ✅ Checklist

- [ ] Unity 6 compiles locally (no errors)
- [ ] `scripts/unity-playmode-run.sh` produces a non-empty `playmode-results.xml` **OR**
- [ ] `scripts/unity-smoke-fallback.sh` passes with `"ok": true` and `interactionsTriggered > 0`
- [ ] No generated assets or `UserSettings/` changes committed
- [ ] Scene budgets reasonable for CI (smoke uses tiny layout)
- [ ] Logs/artifacts not committed (only uploaded by CI if used)

### Notes
- Summary of changes:
- Risks / roll-back plan:
