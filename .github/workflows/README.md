# GitHub Actions — trigger policy

Workflows in this directory are **manual-first** to reduce duplicate CI load, surprise
minute costs, and branch-noise on `cursor/**` and other integration branches.

- **Default:** run a workflow from the Actions tab or with  
  `gh workflow run "<Workflow name>" --ref <branch>`.
- **Tag-driven releases** stay automatic where required (for example `release.yml` on
  version tags).
- **Schedules** are kept only where a periodic signal is still useful without per-push
  cost (for example long-running playground suites). Most path-gated weekly cron jobs
  were removed in favour of explicit dispatch.

When you change a workflow file, open a PR and run the relevant workflow(s) manually
before merge if your branch protection expects a green check from that workflow.
