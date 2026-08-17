# User-acceptance tests

These scripts test **claims**, not code. A unit test asks whether a function does what the author
meant; these ask whether the product does what the documentation promises a user. That distinction is
the whole point: every defect this suite found on 2026-08-17 was a claim that used to be true, and in
each case the code was fine.

| tier | file | asks |
|------|------|------|
| 0 | `tier0-2.sh` | Can a tester clone, build and run `doctor` from `docs/TesterQuickstart.md` verbatim? |
| 1 | `tier0-2.sh` | Does a submitted task leave a retrievable record and a trust-log entry under its own id? |
| 2 | `tier0-2.sh` | Does the certification gate admit correct code and reject a weak witness? |
| 4 | `tier4.sh` | Does the reference brick build, and does the API actually fail closed? |
| 5 | `tier5.sh` | Do the golden paths in `docs/DEPLOYMENT.md` describe the compose files an operator would run? |

`tier5.sh` needs the docker CLI and so runs on the CI runner rather than inside the SDK container the
other tiers use. It only calls `docker compose config`, which resolves a stack without building or
starting anything; booting the portal stack and curling `/health` is a deliberate run, noted at the
foot of that script.

Tier 3 (the autonomy loop) is not here. It needs a container engine and a local model server, and a
gate that depends on that infrastructure teaches people to ignore it. Run it deliberately:
`spikes/autonomy-first-flight/run-first-flight.ps1 -SweepLive -Models <model> -MaxObjectives 1`.

## Running

Locally — clones fresh, which is the point of tier 0 (a cold first fifteen minutes):

```bash
docker run --rm -v "$PWD/tests/uat:/uat:ro" -w /work mcr.microsoft.com/dotnet/sdk:10.0 bash /uat/tier0-2.sh
```

Against a checkout you already have (this is how CI runs it, so the gate tests the commit under
review rather than whatever `master` happens to be):

```bash
UAT_REPO_DIR="$PWD" UAT_OUT=/tmp/uat bash tests/uat/tier0-2.sh
```

Each check prints one `RESULT<tab>tier<tab>id<tab>PASS|FAIL<tab>detail` line and appends it to
`$UAT_OUT/results.tsv`. A failing check never aborts the run — one bad claim must not hide the rest —
and the script exits non-zero at the end if anything failed.

## Writing a check

Assert a machine-verifiable outcome, and prefer running the documented thing over re-implementing it.
The strongest check in the suite greps the API command **out of** `docs/TesterQuickstart.md` and runs
it; that is what catches the page going stale, which no amount of testing the API itself would.

Four rules, each of which this suite violated at least once before it stopped lying:

1. **Never hardcode a verdict.** `time-to-first-audited-job` was written as an unconditional PASS and
   cheerfully reported "167s to a `CopilotTask` entry" during a run where no such entry existed. A
   stopwatch reading is not the claim; it now only passes if the audit entry is really there.
2. **Don't scrape a run log for evidence it doesn't contain.** Two gate tests were scored "did not
   run" because `dotnet test` prints test names only for *failures* at default verbosity. Establish
   presence with `--list-tests`, not by grepping output that was never going to say it.
3. **Anchor every parse.** A bare "first `http://` in the log" grep pointed `curl` at the Ollama probe
   URL that precedes the listen line, so three *security* checks reported connection failures as
   `000` — false negatives on the checks where a false negative is least acceptable. Anchor on
   `Now listening on:`.
4. **Separate the environment from the product.** The SDK image sets `ASPNETCORE_HTTP_PORTS=8080`,
   which made the documented `:5000` look like a defect. The scripts unset it so the container behaves
   like a tester's machine; when a check fails, rule out your own environment before filing anything.

And pair every negative with its positive control. "POST without an API key returns 401" proves
nothing on its own — an endpoint that is simply broken also fails to return 200. The paired check
("...and the same POST *with* the key returns 200") is what makes the first one mean *authentication*.
