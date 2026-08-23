# Release Manager

Vertical app configuration for release-readiness automation: monitoring the repo, running targeted tests, collecting SLO evidence, and generating readiness summaries.

## Agent set

Background agent definitions live in `config/agent_set.release_manager.json`. The set includes:

- **repo-monitor** (optimizer): watches the repository for changes that affect release gates.
- **test-runner** (tester): runs framework smoke tests on a schedule.
- **slo-collector** (optimizer): gathers SLO and gate artifact paths under `.ashlar/`.
- **report-generator** (optimizer): produces release readiness report output (configured output directory: `.ashlar/release-manager/reports`).

This mirrors the structure of `apps/runtime-studio/config/agent_set.local.json` (roles, schedules, exfiltration policy). Tune intervals and filters for your environment.

## Running

Point the Ashlar background-agent daemon at this config file, for example:

```bash
dotnet run --project application/src/Ashlar.CLI -- background-agent daemon --config apps/release-manager/config/agent_set.release_manager.json
```

Ensure the repo root is correct when starting the daemon so paths in `Parameters` resolve.
