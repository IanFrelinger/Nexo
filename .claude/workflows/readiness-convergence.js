export const meta = {
  name: 'readiness-convergence',
  description: 'One readiness convergence cycle, container-first: gate → fix in clone worktrees → adversarial verify → integrate in the agent clone → regate → sync-push',
  whenToUse: 'Drive a repo layer toward a green readiness gate (scripts/readiness-gate-local.sh). v2: ALL git and builds run inside the dev container; the host only runs docker exec. args: {layer?, container?, agentClone?, worktreeContainerRoot?, worktreeHostRoot?, checkout?, hostRoot?, containerRoot?, branch?, syncBranch?, maxFixers?, excludeGates?}',
  phases: [
    { title: 'Gate', detail: 'run readiness-gate-local.sh in the agent clone (container FS)' },
    { title: 'Fix', detail: 'one fixer per failing gate, container-git worktrees on the bind mount' },
    { title: 'Verify', detail: 'adversarial refutation of each claimed fix' },
    { title: 'Integrate', detail: 'cherry-pick into the clone, regate, push container/* staging ref' },
  ],
}

const cfg = {
  layer: (args && args.layer) || 'application',
  container: (args && args.container) || 'elated_satoshi',
  // The agent clone: container-native authority for all integration commits.
  agentClone: (args && args.agentClone) || '/workspaces/nexo-agent',
  // Fixer worktrees are created FROM the clone ONTO the bind mount, so the
  // same directory has a container path (for git/builds) and a host path
  // (for the harness Read/Edit/Write tools).
  worktreeContainerRoot: (args && args.worktreeContainerRoot) || '/workspaces/Nexo/.claude/worktrees',
  worktreeHostRoot: (args && args.worktreeHostRoot) || 'C:/Users/icfre/Downloads/Nexo/.claude/worktrees',
  // Orchestration checkout on the host — used ONLY to read role files.
  checkout: (args && args.checkout) || 'C:/Users/icfre/Downloads/Nexo/.claude/worktrees/recursing-franklin-cbb828',
  hostRoot: (args && args.hostRoot) || 'C:/Users/icfre/Downloads/Nexo',
  containerRoot: (args && args.containerRoot) || '/workspaces/Nexo',
  branch: (args && args.branch) || 'claude/recursing-franklin-cbb828',
  syncBranch: (args && args.syncBranch) || 'container/claude/recursing-franklin-cbb828',
  maxFixers: args && Number.isInteger(args.maxFixers) && args.maxFixers >= 0 ? args.maxFixers : 4,
  excludeGates: (args && Array.isArray(args.excludeGates) && args.excludeGates) || [],
}

const ENV_NOTE = `Environment facts (authoritative — container-first pipeline v2):
- EVERYTHING repo-related — git AND builds/tests — runs inside the dev container '${cfg.container}' via: docker exec ${cfg.container} bash -lc "<command>". docker exec enters as root; git identity and safe.directory are preconfigured (scripts/readiness-container-setup.sh).
- NEVER run git on the Windows host. Host tools are only for: docker exec, and Read/Edit/Write on host file paths.
- The agent clone ${cfg.agentClone} (branch ${cfg.branch}) is the integration authority. Its 'origin' is ${cfg.containerRoot} (the bind-mounted host repo). The host repo receives commits ONLY via the staging ref push: git -C ${cfg.agentClone} push origin HEAD:refs/heads/${cfg.syncBranch} — done by the integrator alone. Never push to any other ref or remote; never touch master.
- Fixer worktrees live at ${cfg.worktreeContainerRoot}/<name> in the container, which is the SAME directory as ${cfg.worktreeHostRoot}/<name> on the host (bind mount). Edit files with the harness Edit/Write tools on the HOST path; run git and dotnet on the CONTAINER path via docker exec. Host git does NOT work on these worktrees (their gitdir points into the clone) — do not try.
- The readiness gate runs in the clone: docker exec ${cfg.container} bash -lc "cd ${cfg.agentClone} && bash scripts/readiness-gate-local.sh --layer ${cfg.layer} --json <out>".`

const GATE_SCHEMA = {
  type: 'object',
  properties: {
    commit: { type: 'string', description: 'HEAD sha of the agent clone (container git), or "unknown"' },
    started_at: { type: 'string', description: 'started_at from the gate JSON, verbatim' },
    all_pass: { type: 'boolean' },
    gates: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          gate: { type: 'string' },
          status: { type: 'string', enum: ['PASS', 'FAIL'] },
          evidence: { type: 'string', description: 'For failures: distilled failing tests/first errors, enough for a fixer to reproduce without rediscovery. Empty for passes.' },
        },
        required: ['gate', 'status', 'evidence'],
      },
    },
    infrastructure_failure: { type: 'string', description: 'Non-empty only if the gate script itself crashed rather than reporting gate results; empty string otherwise' },
  },
  required: ['commit', 'started_at', 'all_pass', 'gates', 'infrastructure_failure'],
}

const FIX_SCHEMA = {
  type: 'object',
  properties: {
    fixed: { type: 'boolean' },
    root_cause: { type: 'string' },
    commit_sha: { type: 'string', description: 'Full or abbreviated (>=7 hex chars) sha of the fix commit in your worktree; empty if not fixed' },
    worktree_name: { type: 'string', description: 'Basename of the worktree the commit lives in (under the shared worktree root)' },
    files_changed: { type: 'array', items: { type: 'string' } },
    test_evidence: { type: 'string', description: 'Exact container commands run and their pass/fail counts' },
    parked_reason: { type: 'string', description: 'Non-empty only when fixed=false: why this needs a human (product decision, cannot reproduce, out of scope)' },
  },
  required: ['fixed', 'root_cause', 'commit_sha', 'worktree_name', 'files_changed', 'test_evidence', 'parked_reason'],
}

const VERDICT_SCHEMA = {
  type: 'object',
  properties: {
    verdict: { type: 'string', enum: ['confirmed', 'refuted'] },
    evidence: { type: 'string', description: 'Commands run and counts, or the checklist item that failed' },
    retry_guidance: { type: 'string', description: 'When refuting: precise, actionable guidance for one retry. Empty when confirming.' },
  },
  required: ['verdict', 'evidence', 'retry_guidance'],
}

const INTEGRATION_SCHEMA = {
  type: 'object',
  properties: {
    branch: { type: 'string' },
    final_sha: { type: 'string' },
    gate_after: { type: 'string', enum: ['PASS', 'FAIL', 'NOT_RUN'] },
    sync_pushed: { type: 'boolean', description: 'true when the staging ref push to origin succeeded' },
    picks_landed: { type: 'array', items: { type: 'string' } },
    picks_dropped: { type: 'array', items: { type: 'string' }, description: 'sha: reason' },
    notes: { type: 'string' },
  },
  required: ['branch', 'final_sha', 'gate_after', 'sync_pushed', 'picks_landed', 'picks_dropped', 'notes'],
}

const SHA_RE = /^[0-9a-f]{7,40}$/i

// Custom agent types register at session start; a session older than the
// .claude/agents/*.md files (or started outside the repo) throws on them.
// Fall back to general-purpose reading the role file itself, so the role text
// stays single-source.
async function roleAgent(role, prompt, opts) {
  try {
    return await agent(prompt, { ...opts, agentType: role })
  } catch (e) {
    log(`agent type '${role}' not registered in this session — falling back to general-purpose with the role file`)
    const roleFile = `${cfg.checkout}/.claude/agents/${role}.md`
    return agent(
      `Adopt the role defined in ${roleFile}: Read that file FIRST and follow its body exactly (the frontmatter is registration metadata; honor its tools list as your own restriction).\n\n${prompt}`,
      opts,
    )
  }
}

function gatePrompt(label) {
  return `${ENV_NOTE}

Run the ${label} readiness gate for layer '${cfg.layer}':
docker exec ${cfg.container} bash -lc "cd ${cfg.agentClone} && bash scripts/readiness-gate-local.sh --layer ${cfg.layer} --json /tmp/readiness-${label}.json"
Then read /tmp/readiness-${label}.json back (docker exec ${cfg.container} cat /tmp/readiness-${label}.json) and distill each failure's log_tail into reproduction-grade evidence (failing test FQNs, first build error per project, exact log excerpts). Report the JSON's started_at verbatim. Capture the clone's HEAD sha: docker exec ${cfg.container} git -C ${cfg.agentClone} rev-parse HEAD. Use a 15-minute timeout for the gate command. Do not fix anything; do not rerun a failing gate.`
}

// Deterministic worktree/branch names (no clocks in workflow scripts):
// derived from the gate run's started_at, which is data, stable across resume.
function runSlug(startedAt) {
  const digits = String(startedAt || '').replace(/[^0-9]/g, '')
  return digits ? digits.slice(4, 12) : 'run'
}

function fixPrompt(failure, baseSha, slug, attempt, priorAttempt) {
  const wtName = `agent-fix-${failure.gate}-${slug}${attempt > 1 ? `-r${attempt}` : ''}`
  const fixBranch = `fix/${failure.gate}-${slug}${attempt > 1 ? `-r${attempt}` : ''}`
  const wtContainer = `${cfg.worktreeContainerRoot}/${wtName}`
  const wtHost = `${cfg.worktreeHostRoot}/${wtName}`
  const retryNote = priorAttempt
    ? `
A previous attempt at this fix was refuted by the verifier.
- Previous worktree (container path): ${cfg.worktreeContainerRoot}/${priorAttempt.fix.worktree_name}
- Previous commit: ${priorAttempt.fix.commit_sha}
- Verifier's guidance: ${priorAttempt.verdict.retry_guidance}
Inspect the previous diff with container git (docker exec ${cfg.container} git -C ${cfg.worktreeContainerRoot}/${priorAttempt.fix.worktree_name} show ${priorAttempt.fix.commit_sha}); re-apply what was right, correct what was refuted. Do not blindly repeat it — your fresh worktree does not contain that attempt.
`
    : ''
  return `${ENV_NOTE}

Fix this failing readiness gate from layer '${cfg.layer}':

Gate: ${failure.gate}
Evidence from the gate run (at clone commit ${baseSha}):
${failure.evidence}
${retryNote}
Work in your own worktree. Create it first (exact command):
docker exec ${cfg.container} git -C ${cfg.agentClone} worktree add -b ${fixBranch} ${wtContainer} ${baseSha}
Your worktree is then:
- container path (git + dotnet): ${wtContainer}
- host path (Read/Edit/Write tools): ${wtHost}
Reproduce the failure in the container first (cd ${wtContainer}). Fix the root cause, prove it with container test runs, then commit IN THE CONTAINER:
docker exec ${cfg.container} bash -lc "cd ${wtContainer} && git add -A && git commit -m '<root-cause message ending with the standard Claude co-author trailer>'"
Report worktree_name=${wtName} and the commit sha per your role.`
}

function verifyPrompt(failure, fix) {
  const wtContainer = `${cfg.worktreeContainerRoot}/${fix.worktree_name}`
  const wtHost = `${cfg.worktreeHostRoot}/${fix.worktree_name}`
  return `${ENV_NOTE}

A fixer claims this readiness-gate failure is resolved. Refute it if you can.

Gate: ${failure.gate}
Original evidence:
${failure.evidence}

Fixer's claim:
- Root cause: ${fix.root_cause}
- Worktree: container path ${wtContainer} (host path ${wtHost})
- Commit: ${fix.commit_sha}
- Files changed: ${fix.files_changed.join(', ')}
- Fixer's test evidence: ${fix.test_evidence}

Run your refutation checklist. Inspect the diff with container git: docker exec ${cfg.container} git -C ${wtContainer} show ${fix.commit_sha}. Run tests in the container at ${wtContainer}.`
}

// ---- Gate (grouping via per-agent phase opts throughout; no global phase() calls,
// so pipelined Fix/Verify agents never race a shared phase state) ----
const gate = await roleAgent('readiness-gate-runner', gatePrompt('initial'), {
  label: 'gate:initial', phase: 'Gate', schema: GATE_SCHEMA,
})
if (!gate) return { converged: false, error: 'gate runner died or was skipped' }

const infra = (gate.infrastructure_failure || '').trim()
if (infra && !/^(none|n\/?a|-)$/i.test(infra) && gate.gates.length === 0) {
  return { converged: false, infrastructure_failure: infra, started_at: gate.started_at, commit: gate.commit, gates: gate.gates }
}

const base = {
  started_at: gate.started_at,
  commit: gate.commit,
  gates: gate.gates,
  excluded: [],
  fixes: [],
  parked: [],
  unresolved: [],
  dropped: [],
  deferred: [],
  integration: null,
}

const allFailures = gate.gates.filter(g => g.status === 'FAIL')
const excluded = allFailures.filter(f => cfg.excludeGates.includes(f.gate)).map(f => f.gate)
const failures = allFailures.filter(f => !cfg.excludeGates.includes(f.gate))
base.excluded = excluded
if (excluded.length) log(`excluding ledger-parked gate(s) from dispatch: ${excluded.join(', ')}`)

if (allFailures.length === 0) {
  if (!gate.all_pass) {
    log('inconsistent gate report: no FAIL rows but all_pass=false — treating as not converged')
    return { ...base, converged: false, inconsistent_gate_report: true }
  }
  log('gate fully green — nothing to fix this cycle')
  return { ...base, converged: true }
}
if (failures.length === 0) {
  log('all remaining failures are ledger-parked for the human — nothing to dispatch')
  return { ...base, converged: false }
}

const work = failures.slice(0, cfg.maxFixers)
base.deferred = failures.slice(cfg.maxFixers).map(f => f.gate)
if (base.deferred.length) {
  log(`gate: ${failures.length} dispatchable failures; dispatching ${work.length}, deferring ${base.deferred.length} to the next cycle`)
} else {
  log(`gate: ${failures.length} failure(s); dispatching one fixer per failure`)
}

const slug = runSlug(gate.started_at)
const baseSha = SHA_RE.test(gate.commit) ? gate.commit : cfg.branch

// ---- Fix + Verify (pipelined per failure; one bounded retry on refutation) ----
async function fixAndVerify(failure, attempt, priorAttempt) {
  const fix = await roleAgent('readiness-fixer', fixPrompt(failure, baseSha, slug, attempt, priorAttempt), {
    label: `fix:${failure.gate}${attempt > 1 ? ':r2' : ''}`, phase: 'Fix', schema: FIX_SCHEMA,
  })
  if (!fix) return { failure, fix: null, verdict: null }
  if (!fix.fixed || !SHA_RE.test(fix.commit_sha)) {
    if (fix.fixed) log(`fix:${failure.gate} reported fixed but returned no valid commit sha — treating as unresolved`)
    return { failure, fix, verdict: null }
  }
  const verdict = await roleAgent('readiness-verifier', verifyPrompt(failure, fix), {
    label: `verify:${failure.gate}${attempt > 1 ? ':r2' : ''}`, phase: 'Verify', schema: VERDICT_SCHEMA,
  })
  return { failure, fix, verdict }
}

const outcomes = await pipeline(
  work,
  (first, item) => fixAndVerify(first || item, 1, null),
  async (outcome, f) => {
    if (!outcome) return null
    if (outcome.verdict && outcome.verdict.verdict === 'refuted') {
      log(`fix for ${f.gate} refuted — one retry with verifier guidance`)
      const retry = await fixAndVerify(f, 2, outcome)
      return { ...retry, firstAttempt: outcome }
    }
    return outcome
  },
)

base.dropped = work.filter((w, i) => !outcomes[i]).map(w => w.gate)
if (base.dropped.length) log(`dropped by stage errors (still failing, unaccounted by fixers): ${base.dropped.join(', ')}`)

const done = outcomes.filter(Boolean)
const confirmed = done.filter(o => o.fix && o.fix.fixed && SHA_RE.test(o.fix.commit_sha) && o.verdict && o.verdict.verdict === 'confirmed')
base.parked = done.filter(o => o.fix && !o.fix.fixed).map(p => ({ gate: p.failure.gate, reason: p.fix.parked_reason }))
const accounted = new Set([...confirmed.map(o => o.failure.gate), ...base.parked.map(p => p.gate)])
base.unresolved = work.map(w => w.gate).filter(g => !accounted.has(g) && !base.dropped.includes(g))

log(`fix round: ${confirmed.length} confirmed, ${base.parked.length} parked for human, ${base.unresolved.length} unresolved, ${base.dropped.length} dropped`)

if (confirmed.length === 0) {
  return { ...base, converged: false }
}

// ---- Integrate (container clone is the authority; sync-push staging ref) ----
const integration = await roleAgent('readiness-integrator', `${ENV_NOTE}

Integrate this cycle's verified fixes into the agent clone ${cfg.agentClone}. Verify first (container git) that its branch is exactly '${cfg.branch}' and the tree is clean — if not, stop and report.

Verified fixes, in gate order (worktree names are under ${cfg.worktreeContainerRoot}):
${confirmed.map(o => `- ${o.failure.gate}: ${o.fix.commit_sha} in worktree ${o.fix.worktree_name} — ${o.fix.root_cause}`).join('\n')}

Cherry-pick each into the clone (the fix commits are in worktrees OF the clone, so plain docker exec ${cfg.container} git -C ${cfg.agentClone} cherry-pick <sha> works). After cherry-picking, rerun the layer gate once:
docker exec ${cfg.container} bash -lc "cd ${cfg.agentClone} && bash scripts/readiness-gate-local.sh --layer ${cfg.layer} --json /tmp/readiness-integration.json"
If the regate passes, push the staging ref to the host repo (the ONLY push you may ever run):
docker exec ${cfg.container} git -C ${cfg.agentClone} push origin HEAD:refs/heads/${cfg.syncBranch}
Then remove ONLY the worktrees whose picks landed:
docker exec ${cfg.container} git -C ${cfg.agentClone} worktree remove --force ${cfg.worktreeContainerRoot}/<name>
(keep worktrees of dropped/parked fixes for inspection). Report per your role, including sync_pushed.`, {
  label: 'integrate', phase: 'Integrate', schema: INTEGRATION_SCHEMA,
})

base.fixes = confirmed.map(o => ({ gate: o.failure.gate, sha: o.fix.commit_sha, root_cause: o.fix.root_cause }))
base.integration = integration
return { ...base, converged: integration ? integration.gate_after === 'PASS' : false }
