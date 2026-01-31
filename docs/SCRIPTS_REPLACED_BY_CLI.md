# Scripts Replaced by C# CLI

All `.sh` scripts have been replaced by C# commands in the Nexo CLI so that the same workflows run on Windows, macOS, Linux, and mobile (e.g. `nexo test portable --scope smoke`). Use the CLI instead of bash.

## Quick reference

| Former script | CLI equivalent |
|---------------|----------------|
| `scripts/portable-test.sh` | `nexo test portable` or `dotnet run --project src/Nexo.CLI -- test portable` |
| `scripts/test-framework-multi-env.sh` | `nexo test multi-env --suite framework --all` |
| `scripts/test-caching-multi-env.sh` | `nexo test multi-env --suite caching --all` |
| `scripts/test-persistence-multi-env.sh` | `nexo test multi-env --suite persistence --all` |
| `scripts/ci-verify.sh` | `nexo ci verify` |
| `scripts/review-summary-md.sh` | `nexo review summary` |
| `scripts/aggregate-junit.sh` | `nexo aggregate junit` |
| `scripts/check-promotion.sh` | `nexo ci check-promotion` |
| `scripts/artifact-diff.sh` | `nexo diff artifacts` |
| `scripts/build-portable.sh` | `nexo build --portable` |
| `scripts/test-local.sh` | `nexo test local` |

## Running without installing the tool

From repo root:

```bash
dotnet run --project src/Nexo.CLI -- test portable --scope smoke
dotnet run --project src/Nexo.CLI -- ci verify
dotnet run --project src/Nexo.CLI -- review summary --input review_summary.json --output REVIEW_SUMMARY.md
```

## Makefile targets

The Makefile uses the CLI (via `dotnet run --project src/Nexo.CLI`) for:

- `make test-portable` → `nexo test portable`
- `make test-multi-env` → `nexo test multi-env --suite framework --all`
- `make ci-verify` → `nexo ci verify`
- `make review-summary` → `nexo review summary`

## Unity / platform-specific

Unity editor, iOS simulator, and Android runs are still platform-specific (Unity executable, Xcode, etc.). The CLI provides `nexo test` with `--platforms` and `nexo test multi-env`; for Unity/iOS/Android native runs, use the same `dotnet test` or platform tooling from the CLI or your CI.
