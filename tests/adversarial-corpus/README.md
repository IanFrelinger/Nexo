# Adversarial corpus

Every attack that beat the gate, or that this round's fences exist to stop,
lives here with an `expect.json`. `AdversarialCorpusTests` replays each fixture
inside the cert-gate. A fix that *intentionally* changes a verdict updates
that `expect.json` in the same commit. An unintended change is a regression.

## Layout

```
tests/adversarial-corpus/
  README.md
  ledger.json
  fixtures/<id>/
    expect.json
    project/          # optional; present when phase=load or phase=certify
      *.csproj
      *.cs
      witness.json
```

## expect.json

| Field | Meaning |
|---|---|
| `id` | Fixture id; must match the directory name |
| `class` | `A` judged≠shipped, `B` author code in certifier, `C` mutation, `D` drift |
| `item` | Round-10 item that owns the fence |
| `phase` | `load` (loader/fence throws) or `certify` (gate verdict) |
| `expect` | `refuse` or `admit` |
| `reasonContains` | Substring that must appear in the exception or refusal reason |

## Adding a fixture

1. Reproduce the attack in `fixtures/<id>/`.
2. Write `expect.json` with the verdict the gate *should* produce after the fix.
3. Add a row to `ledger.json`.
4. Confirm `AdversarialCorpusTests` fails at the parent commit and passes here.
