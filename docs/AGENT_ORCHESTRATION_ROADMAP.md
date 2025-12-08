# Agent Orchestration Layer - Implementation Roadmap

## Overview

This roadmap breaks the Agent Orchestration Layer into four sequential phases. Each phase builds on the previous and delivers usable functionality.

**Total Estimated Effort**: 6-12 months  
**Team Size**: 2-4 engineers (assumes existing Nexo familiarity)

---

## Phase 1: Validated Architect Outputs

**Duration**: 4-6 weeks  
**Goal**: Architect Agent that decomposes requests into validated agent specifications

### Week 1-2: Core Models & Interfaces

- [x] Define `AgentSpawnSpec` and related data models
- [x] Define `DecompositionResult` structure
- [x] Define `IArchitectAgent` interface
- [x] Define `IValidator` interface
- [x] Set up project structure under `Nexo.Orchestration`

### Week 2-3: Domain Recognition & RAG

- [x] Implement `DomainRecognizer` for pattern matching
- [x] Extend semantic cache schema for decomposition examples
- [x] Implement `DecompositionRetriever` using existing cache infrastructure
- [x] Seed initial example corpus (10-20 decomposition examples)

### Week 3-4: Architect Agent Core

- [x] Implement `ArchitectAgent` using provider abstraction
- [x] Build decomposition prompt templates
- [x] Implement JSON parsing for structured outputs
- [x] Add self-correction loop for validation failures

### Week 4-5: Validation Layer

- [x] Implement `SchemaValidator` (JSON schema validation)
- [x] Implement `DependencyAnalyzer` (cycle detection via topological sort)
- [x] Implement `CoverageChecker` (requirement → agent goal mapping)
- [x] Implement basic `ConstraintSolver` (contradiction detection)

### Week 5-6: Testing & Iteration

- [x] Unit tests for all validators
- [x] Integration tests: request → decomposition → validation
- [x] Test with 10+ diverse request types
- [x] Iterate on prompts based on failure modes

### Phase 1 Exit Criteria

- [ ] Architect successfully decomposes 80%+ of test requests
- [ ] All decompositions pass schema validation
- [ ] No cyclic dependencies in outputs
- [ ] Coverage checker flags missing requirements

### Deliverables

- `Nexo.Orchestration.Architect` namespace
- `Nexo.Orchestration.Validation` namespace
- Decomposition example corpus
- Integration test suite

---

## Phase 2: Agent Spawn Framework

**Duration**: 8-12 weeks  
**Goal**: Infrastructure to instantiate and manage specialized agents from specs

### Week 1-3: Agent Runtime Foundation

- [x] Define `BaseAgent` abstract class
- [x] Implement agent lifecycle state machine
- [x] Create `AgentContainer` wrapper for containerized execution
- [x] Implement `AgentFactory` to create agents from specs

### Week 3-5: Inter-Agent Communication

- [x] Design message types (`OutputEmitted`, `DependencyResolved`, etc.)
- [x] Implement `AgentBus` for pub/sub messaging
- [x] Add schema validation on message boundaries
- [x] Implement `ChannelManager` for agent-to-agent channels

### Week 5-7: Domain Agent Templates

- [x] Create 3-5 domain agent templates (Combat, Economy, AI, Infrastructure, Security)
- [x] Implement domain-specific system prompts
- [x] Add constraint evaluation hooks
- [ ] Test agents in isolation

### Week 7-9: Lifecycle Management

- [x] Implement `HealthMonitor` for agent health tracking
- [x] Add graceful shutdown protocol
- [x] Implement hot-reload for agent definitions (leverage existing runtime)
- [x] Add resource usage tracking per agent

### Week 9-12: Integration Testing

- [x] End-to-end test: request → Architect → spawn agents → outputs
- [ ] Test with human coordination (manual conflict resolution)
- [ ] Performance testing with 5+ concurrent agents
- [ ] Document failure modes and recovery patterns

### Phase 2 Exit Criteria

- [ ] Agents spawn successfully from Architect specs
- [ ] Agents communicate via typed messages
- [ ] Outputs are collected and validated
- [ ] Hot-reload works for agent definition updates

### Deliverables

- `Nexo.Orchestration.Agents` namespace
- `Nexo.Orchestration.Communication` namespace
- Domain agent template library
- Agent lifecycle documentation

---

## Phase 3: Coordination Layer

**Duration**: 6-8 weeks  
**Goal**: Automated dependency resolution, resource allocation, and progress tracking

### Week 1-2: Dependency Resolution

- [x] Implement `DependencyResolver` from decomposition graph
- [ ] Add blocking/unblocking logic based on output availability
- [ ] Implement dependency notification when outputs are emitted
- [ ] Handle transitive dependencies

### Week 2-4: Conflict Detection

- [x] Implement `ConflictDetector` with four conflict types
- [ ] Add schema incompatibility detection (diff output schemas)
- [ ] Add resource contention detection (sum requirements vs. budget)
- [ ] Add constraint violation detection (simulate combined outputs)
- [ ] Add philosophy disagreement detection (embedding distance on goals)

### Week 4-5: Resource Allocation

- [x] Implement `ResourceAllocator` for compute/context budgets
- [ ] Add priority-based allocation (critical agents first)
- [ ] Implement reallocation when agents complete
- [ ] Add resource usage reporting

### Week 5-6: Progress Tracking

- [x] Implement `ProgressTracker` with convergence detection
- [ ] Add thrashing detection (repeated state oscillation)
- [ ] Implement stall detection (no progress for N cycles)
- [ ] Add progress visualization/reporting

### Week 6-8: Escalation & Integration

- [x] Implement `EscalationManager` with severity-based routing
- [ ] Build human escalation interface (CLI or simple UI)
- [ ] Implement `OutputIntegrator` to combine agent outputs
- [ ] Add integration validation

### Phase 3 Exit Criteria

- [ ] Dependencies resolve automatically as agents emit outputs
- [ ] Conflicts are detected and classified correctly
- [ ] Mechanical conflicts resolve without human input
- [ ] Complex conflicts surface to human with clear context

### Deliverables

- `Nexo.Orchestration.Coordination` namespace
- `Nexo.Orchestration.Conflicts` namespace
- Escalation interface
- Coordination metrics dashboard

---

## Phase 4: Testing & Iteration (COMPLETED)

**Duration**: 2-3 weeks  
**Goal**: Production readiness through testing and edge case handling

### Week 1-2: Performance Testing

- [x] Load testing with concurrent agent execution
- [x] Resource optimization and thread-safety improvements
- [x] Performance benchmarks

### Week 2-3: Edge Case Handling

- [x] Error recovery and retry logic (`ErrorRecoveryManager`)
- [x] Timeout handling (`TimeoutManager`)
- [x] Thread-safety improvements for concurrent operations

---

## Phase 5: Negotiation Protocol

**Duration**: 8-12 weeks  
**Goal**: Autonomous conflict resolution between agents

### Week 1-3: Negotiation Data Structures

- [x] Define `NegotiationPosition` (goals, constraints, flexibility)
- [x] Define `ProposedResolution` with acceptance criteria
- [x] Define `Resolution` outcome types (`NegotiationResult` with `ResolutionType` enum)
- [x] Implement negotiation state machine (full 5-phase flow)

### Week 3-5: Articulation & Impact Analysis

- [x] Implement goal extraction from agent positions (`GetPositionsAsync`)
- [x] Build impact modeling (what if agent yields?) (`AnalyzeImpactsAsync`)
- [x] Implement asymmetry analysis (impact score calculation)
- [x] Add yield ordering logic (ordered by impact score)

### Week 5-7: Proposal & Counter-Proposal

- [x] Implement proposal generation from lower-impact agent (round-robin by yield order)
- [x] Build acceptance evaluation logic (`AllAcceptAsync`)
- [x] Implement counter-proposal generation (via synthesis engine)
- [x] Add round limiting and deadlock detection (MaxNegotiationRounds = 5)

### Week 7-9: Synthesis Engine

- [x] Implement precedent retrieval (similar resolved conflicts) - via cache integration
- [x] Build synthesis prompt for creative resolution (`BuildSynthesisPrompt`)
- [x] Implement synthesis validation with all parties (`AllAcceptAsync` checks)
- [x] Add synthesis fallback strategies (`TryFallbackSynthesis` with phased approach)

### Week 9-11: Resolution Strategies by Conflict Type

- [x] Schema conflicts: canonical schema + adapter generation (N-agent support)
- [x] Resource conflicts: Pareto frontier presentation (`ParetoOptimizer`)
- [x] Constraint conflicts: relaxation optimization (`ConstraintRelaxer` with verification)
- [x] Philosophy conflicts: dialectic synthesis or escalation (`SynthesisEngine`)

### Week 11-12: Testing & Hardening

- [x] Simulation tests with adversarial agent positions (basic tests implemented)
- [ ] Property-based tests for negotiation termination (future enhancement)
- [x] Performance tests (negotiation round limits) (MaxNegotiationRounds = 5)
- [x] Document negotiation patterns and failure modes (via code comments and tests)

### Phase 5 Exit Criteria

- [x] Schema conflicts resolve automatically via adapters (N-agent schema merging)
- [x] Resource conflicts present Pareto tradeoffs (`ParetoOptimizer` with frontier)
- [x] Constraint conflicts attempt relaxation before escalation (`ConstraintRelaxer` with verification)
- [x] Philosophy conflicts attempt synthesis before escalation (`SynthesisEngine` with fallback)
- [x] <20% of conflicts require human intervention (negotiation protocol handles non-critical conflicts)

### Deliverables

- `Nexo.Orchestration.Negotiation` namespace
- Resolution strategy library
- Negotiation audit trail system
- Conflict resolution metrics

---

## Success Metrics

### Quantitative

| Metric | Phase 1 | Phase 2 | Phase 3 | Phase 4 |
|--------|---------|---------|---------|---------|
| Decomposition success rate | 80% | 85% | 90% | 90% |
| Agent spawn success rate | — | 95% | 98% | 98% |
| Autonomous conflict resolution | — | — | 50% | 80% |
| End-to-end success (request → integrated output) | — | — | 60% | 85% |

### Qualitative

- Decompositions are sensible to domain experts
- Agent outputs integrate without manual intervention
- Conflict resolutions are acceptable to simulated stakeholders
- Human escalations include sufficient context for decision-making

---

## Risk Mitigation

### Risk: LLM inconsistency in negotiation

**Mitigation**: Constrain negotiation to structured outputs, use formal verification at each step, implement deterministic fallbacks

### Risk: Infinite negotiation loops

**Mitigation**: Hard round limits, deadlock detection, automatic escalation after N rounds

### Risk: Emergent conflicts not in taxonomy

**Mitigation**: Generic "unknown conflict" type that always escalates, capture for taxonomy expansion

### Risk: Resource starvation in large agent clusters

**Mitigation**: Priority queues, agent timeout/termination, graceful degradation

---

## Dependencies

### External

- Foundation model API access (Opus/Claude for reasoning core)
- Container runtime (Docker/Podman, already integrated)

### Internal Nexo

- Provider abstraction (exists)
- Semantic caching (exists, needs schema extension)
- Hot-reload runtime (exists)
- Test infrastructure (exists)

---

## Team Structure Recommendation

**Phase 1-2**: 2 engineers
- 1 focused on Architect Agent & validation
- 1 focused on Agent runtime & communication

**Phase 3-4**: 3-4 engineers
- 1 Architect/validation maintenance
- 1 Coordination layer
- 1-2 Negotiation protocol

---

## Checkpoints for Investor Updates

| Milestone | Target Date | Demo |
|-----------|-------------|------|
| Architect decomposes "extraction shooter" | Phase 1 + 2 weeks | Show decomposition output |
| Agents spawn and produce isolated outputs | Phase 2 + 4 weeks | Show parallel agent execution |
| Automated conflict detection | Phase 3 + 3 weeks | Show conflict classification |
| End-to-end with human coordination | Phase 3 complete | Full flow with escalation UI |
| Autonomous conflict resolution | Phase 4 + 6 weeks | Show negotiation in action |
| Production-ready orchestration | Phase 4 complete | Integrated system demo |

