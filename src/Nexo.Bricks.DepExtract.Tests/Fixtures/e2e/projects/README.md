# Mock adapt projects (E2E)

Catalog-driven proprietary parser stubs for DepExtract plan → implement (+ compile gate, optional install/extract cycle).

| ID | Strategy | Compile | Full extract |
|----|----------|---------|--------------|
| P01_SimpleTickPull | scaffold | yes | no |
| P02_NextEventNamed | scaffold | yes | no |
| P03_DualHeaderBody | scaffold | yes | no |
| P04_LegacyRadar | model (LLM) | yes | no |
| P05_SessionFactory | model (LLM) | yes | no |
| P06_ChunkedCursor | model (LLM) | yes | no |
| P07_EvtqTwin | scaffold | yes | `adaptCycle` (compose recreate + extract) when `/api/status` reports `stackIdentityOk`; else ephemeral install path |
| P08_NoPullSurface | model (LLM) | no | no |
| P09_Path With Spaces | scaffold | yes (dir has spaces) | no |
| P10_NeedsCompanion | scaffold | yes (links `.cpp`) | no |

Also keep the original `../MockEvtqParser` for the classic full pipeline e2e.

```bash
# scaffold + compile gate (no Ollama)
bash scripts/e2e-adapt-projects.sh

# include model-strategy projects
INCLUDE_LLM=1 bash scripts/e2e-adapt-projects.sh

# skip compile gate (not recommended)
SKIP_COMPILE=1 bash scripts/e2e-adapt-projects.sh

# one project
PROJECT_FILTER=P07_EvtqTwin bash scripts/e2e-adapt-projects.sh
```
