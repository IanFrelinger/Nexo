---
name: nexo-trust-inspector
description: Inspect Nexo trust policy packs — list available packs and describe the active pack.
license: Apache-2.0
compatibility: Nexo 0.x
allowed-tools: run_skill_script read_skill_resource
---

# Nexo Trust Inspector

Use this skill to inspect trust policy packs configured for the local Nexo runtime.

## Workflow

1. Load this skill for full instructions.
2. Read `references/pack-schema.md` when you need the JSON schema.
3. Run `scripts/list-packs.sh` with an optional trust-packs directory path argument.

## Safety

This skill only reads JSON policy pack files. It does not modify policy state.
