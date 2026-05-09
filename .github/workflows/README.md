# GitHub Actions in this repository

## Branch protection (recommended after workflow changes)

Many workflows are triggered with **`workflow_dispatch`** only. If **branch protection**
still lists required status checks for workflows that no longer run on every `push` or
`pull_request`, either:

1. Remove those required checks, or  
2. Add a small **always-on** workflow (for example `dotnet build` + one smoke test project)
   and require only that check on PRs, or  
3. Keep manual gates: maintainers run the relevant workflow from **Actions** before merge.

## Finding a workflow

Use the GitHub **Actions** tab or the GitHub CLI, for example:

```bash
gh workflow list
gh workflow run "Cross-Platform Tests" --ref <branch> -f scope=smoke
```

Replace `<branch>` with your default branch name (`master`, `main`, or your fork default).

## Forge API and persistence

Forge session state in **Nexo.API** can persist to LiteDB when `Nexo:ForgeSession:LiteDbPath`
is set. See `docs/Persistence.md`.
