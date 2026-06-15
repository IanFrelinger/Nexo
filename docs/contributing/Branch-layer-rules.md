# Branch layer rules (runtime vs application)

CI workflow **`.github/workflows/layer-boundary.yml`** enforces:

| Base branch | Rule |
|-------------|------|
| `master`, `main`, or `runtime/*` | PR must **not** change files under **`application/`**, unless the PR also changes **`commercial/`** (vertical integration merge) or coordinates **`Nexo.Authoring`** distribution (`src/Nexo.Authoring/` or `scripts/verify-standalone-brick-authoring.sh` alongside `application/`) |
| `application/*` | PR must **not** change files under **`src/`** (kernel) |
| `master` / `main` / `runtime/*` | Head branch must **not** be named `application/*` |
| `application/*` | Head branch must **not** be named `runtime/*` |

## Enable required check on GitHub

1. Repository **Settings → Rules → Rulesets** (or **Branches → Branch protection**).
2. Target branches: e.g. `master`, `main`, `application/**`, `runtime/**` as needed.
3. Enable **Require status checks to pass**.
4. Add the check named **`layer-boundary`** (job **`verify`** appears as **`layer-boundary / verify`** in the checks list — require that job).

Until the workflow runs once on a PR, the check may not appear in the search box; merge any PR that touches `.github/workflows/` first or run workflow manually via **Actions**.

## Branch naming convention

- **`application/feature-xyz`** — work that only touches `application/`, docs, tooling outside `src/`.
- **`runtime/feature-xyz`** or topic branches off `master` — kernel changes under `src/`.

If you do not use long-lived `application/*` integration branches, only the **runtime-base** rules apply until you create one.
