export const meta = {
  name: 'readiness-convergence',
  description: 'One readiness convergence cycle: gate → fix in isolated worktrees → adversarial verify → integrate → regate',
  whenToUse: 'Drive a repo layer toward a green readiness gate (scripts/readiness-gate-local.sh). args: {layer?, checkout?, container?, containerCheckout?, hostRoot?, containerRoot?, branch?, maxFixers?, excludeGates?}',
  phases: [
    { title: 'Gate', detail: 'run readiness-gate-local.sh in the dev container' },
    { title: 'Fix', detail: 'one fixer per failing gate, isolated worktrees' },
    { title: 'Verify', detail: 'adversarial refutation of each claimed fix' },
    { title: 'Integrate', detail: 'cherry-pick confirmed fixes, regate' },
  ],
}

const cfg = {
  layer: (args && args.layer) || 'application',
  checkout: (args && args.checkout) || 'C:/Users/icfre/Downloads/Nexo/.claude/worktrees/recursing-franklin-cbb828',
  container: (args && args.container) || 'elated_satoshi',
  containerCheckout: (args && args.containerCheckout) || '/workspaces/Nexo/.claude/worktrees/recursing-franklin-cbb828',
  hostRoot: (args && args.hostRoot) || 'C:/Users/icfre/Downloads/Nexo',
  containerRoot: (args && args.containerRoot) || '/workspaces/Nexo',
  branch: (args && args.branch) || 'claude/recursing-franklin-cbb828',
  maxFixers: args && Number.isInteger(args.maxFixers) && args.maxFixers >= 0 ? args.maxFixers : 4,
  excludeGates: (args && Array.isArray(args.excludeGates) && args.excludeGates) || [],
}

const ENV_NOTE = `Environment facts (authoritative for this repo):
- Builds/tests run ONLY inside the dev container '${cfg.container}' via: docker exec ${cfg.container} bash -lc "cd <container-path> && <command>". Host dotnet results do not count.
- Host path ${cfg.hostRoot} maps to ${cfg.containerRoot} in the container (this applies to worktrees under .claude/worktrees/<name> too — replace the host prefix to get the container path).
- Git works ONLY on the host (a linked worktree's gitdir points at a host path the container cannot resolve).
- The integration checkout is ${cfg.checkout} (container path ${cfg.containerCheckout}), expected branch ${cfg.branch}.`

const GATE_SCHEMA = {
  type: 'object',
  properties: {
    commit: { type: 'string', description: 'HEAD sha of the checkout (host-side git), or "unknown"' },
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
    worktree_path: { type: 'string', description: 'Host path of the worktree the commit lives in' },
    files_changed: { type: 'array', items: { type: 'string' } },
    test_evidence: { type: 'string', description: 'Exact container commands run and their pass/fail counts' },
    parked_reason: { type: 'string', description: 'Non-empty only when fixed=false: why this needs a human (product decision, cannot reproduce, out of scope)' },
  },
  required: ['fixed', 'root_cause', 'commit_sha', 'worktree_path', 'files_changed', 'test_evidence', 'parked_reason'],
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
    picks_landed: { type: 'array', items: { type: 'string' } },
    picks_dropped: { type: 'array', items: { type: 'string' }, description: 'sha: reason' },
    notes: { type: 'string' },
  },
  required: ['branch', 'final_sha', 'gate_after', 'picks_landed', 'picks_dropped', 'notes'],
}

const SHA_RE = /^[0-9a-f]{7,40}$/i

// Custom agent types register at session start; a session older than the
// .claude/agents/*.md files throws on them. Fall back to general-purpose
// reading the role file itself, so the role text stays single-source.
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
docker exec ${cfg.container} bash -lc "cd ${cfg.containerCheckout} && bash scripts/readiness-gate-local.sh --layer ${cfg.layer} --json /tmp/readiness-${label}.json"
Then read /tmp/readiness-${label}.json back (docker exec ${cfg.container} cat /tmp/readiness-${label}.json) and distill each failure's log_tail into reproduction-grade evidence (failing test FQNs, first build error per project). Report the JSON's started_at verbatim. Capture the checkout's HEAD sha with host-side git in ${cfg.checkout}. Use a 10-minute timeout for the gate command. Do not fix anything; do not rerun a failing gate.`
}

function fixPrompt(failure, priorAttempt) {
  const retryNote = priorAttempt
    ? `
A previous attempt at this fix was refuted by the verifier.
- Previous worktree (host path): ${priorAttempt.fix.worktree_path}
- Previous commit: ${priorAttempt.fix.commit_sha}
- Verifier's guidance: ${priorAttempt.verdict.retry_guidance}
Inspect the previous diff with host-side git (git -C <previous-worktree> show <previous-commit>); re-apply what was right, correct what was refuted. Do not blindly repeat it.
`
    : ''
  return `${ENV_NOTE}

You are in your own isolated git worktree (your working directory) — commit your fix there.
Fix this failing readiness gate from layer '${cfg.layer}':

Gate: ${failure.gate}
Evidence from the gate run:
${failure.evidence}
${retryNote}
Reproduce in the container first (map YOUR worktree path into the container by replacing ${cfg.hostRoot} with ${cfg.containerRoot}). Fix the root cause, prove it with container test runs, commit on your worktree's branch, and report per your role.`
}

function verifyPrompt(failure, fix) {
  return `${ENV_NOTE}

A fixer claims this readiness-gate failure is resolved. Refute it if you can.

Gate: ${failure.gate}
Original evidence:
${failure.evidence}

Fixer's claim:
- Root cause: ${fix.root_cause}
- Worktree (host path): ${fix.worktree_path}
- Commit: ${fix.commit_sha}
- Files changed: ${fix.files_changed.join(', ')}
- Fixer's test evidence: ${fix.test_evidence}

Run your refutation checklist. The container path of the fixer's worktree is its host path with ${cfg.hostRoot} replaced by ${cfg.containerRoot}. Inspect the diff with host-side git (git -C <worktree> show ${fix.commit_sha}).`
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

// ---- Fix + Verify (pipelined per failure; one bounded retry on refutation) ----
async function fixAndVerify(failure, priorAttempt) {
  const fix = await roleAgent('readiness-fixer', fixPrompt(failure, priorAttempt), {
    label: `fix:${failure.gate}`, phase: 'Fix',
    isolation: 'worktree', schema: FIX_SCHEMA,
  })
  if (!fix) return { failure, fix: null, verdict: null }
  if (!fix.fixed || !SHA_RE.test(fix.commit_sha)) {
    if (fix.fixed) log(`fix:${failure.gate} reported fixed but returned no valid commit sha — treating as unresolved`)
    return { failure, fix, verdict: null }
  }
  const verdict = await roleAgent('readiness-verifier', verifyPrompt(failure, fix), {
    label: `verify:${failure.gate}`, phase: 'Verify', schema: VERDICT_SCHEMA,
  })
  return { failure, fix, verdict }
}

const outcomes = await pipeline(
  work,
  (first, item) => fixAndVerify(first || item, null),
  async (outcome, f) => {
    if (!outcome) return null
    if (outcome.verdict && outcome.verdict.verdict === 'refuted') {
      log(`fix for ${f.gate} refuted — one retry with verifier guidance`)
      const retry = await fixAndVerify(f, outcome)
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

// ---- Integrate ----
const integration = await roleAgent('readiness-integrator', `${ENV_NOTE}

Integrate this cycle's verified fixes into the integration checkout at ${cfg.checkout}. Its branch must be exactly '${cfg.branch}' (git -C ${cfg.checkout} branch --show-current on the host) — if it is anything else, stop and report.

Verified fixes, in gate order:
${confirmed.map(o => `- ${o.failure.gate}: ${o.fix.commit_sha} in ${o.fix.worktree_path} — ${o.fix.root_cause}`).join('\n')}

After cherry-picking, rerun the layer gate once:
docker exec ${cfg.container} bash -lc "cd ${cfg.containerCheckout} && bash scripts/readiness-gate-local.sh --layer ${cfg.layer} --json /tmp/readiness-integration.json"
Report per your role. Never push.`, {
  label: 'integrate', phase: 'Integrate', schema: INTEGRATION_SCHEMA,
})

base.fixes = confirmed.map(o => ({ gate: o.failure.gate, sha: o.fix.commit_sha, root_cause: o.fix.root_cause }))
base.integration = integration
return { ...base, converged: integration ? integration.gate_after === 'PASS' : false }
