## ✅ Checklist

- [ ] Unity 6 compiles locally (no errors)
- [ ] `scripts/unity-playmode-run.sh` produces a non-empty `playmode-results.xml` **OR**
- [ ] `scripts/unity-smoke-fallback.sh` passes with `"ok": true` and `interactionsTriggered > 0`
- [ ] No generated assets or `UserSettings/` changes committed
- [ ] Scene budgets reasonable for CI (smoke uses tiny layout)
- [ ] Logs/artifacts not committed (only uploaded by CI if used)
- [ ] `scripts/run-with-config.sh` succeeds with the repo's `nexo.pipeline.json`
- [ ] `scripts/ci-verify.sh` uploads a JUnit XML (UTF or smoke)

### Notes
- Summary of changes:
- Risks / roll-back plan:
