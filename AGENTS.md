# Agent instructions

## Autonomous release manager

Ashlar's release authority is the repo-local coordinator, not the extracted
product host.

- Skill / Custom Mode: `.cursor/skills/release-manager/SKILL.md`
- Coordinator persona: `.cursor/agents/release-manager.md`
- Specialists: `code-auditor`, `ci-auditor`, `security-auditor`,
  `packaging-auditor`, `documentation-auditor`, `operations-auditor`
- Always-on rule: `.cursor/rules/release-publishing-safety.mdc`

To run or set up the agent, follow the skill. In chat, `/release-manager`
attaches the playbook. Cloud and CLI agents should start with:

```text
Follow .cursor/skills/release-manager/SKILL.md.
Delegate in parallel to code-auditor, ci-auditor, security-auditor,
packaging-auditor, documentation-auditor, and operations-auditor.
Do not publish, tag, or deploy.
```

Validate the committed plan and personas:

```bash
make release-manager-validate
```

Run the deterministic six-lane campaign on a clean commit whose `VERSION`
is already the candidate:

```bash
make release-manager-audit
```

READY is evidence, not authorization to publish. See
`docs/AutonomousReleaseManager.md` and `docs/RELEASE_RUNBOOK.md`.
