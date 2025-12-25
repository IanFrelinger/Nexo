// src/types/workflow.ts

// ============================================================================
// MODEL & PROVIDER TYPES
// ============================================================================

export type ModelTier = 'strategic' | 'tactical' | 'execution';

export interface ModelConfig {
  tier: ModelTier;
  preferredProvider: string;           // 'anthropic', 'openai', 'ollama', 'azure'
  preferredModel: string;              // 'claude-3-opus', 'gpt-4', etc.
  fallbackProviders: string[];         // Ordered fallback list
  maxTokens?: number;
  temperature?: number;
}

export const DEFAULT_MODEL_CONFIGS: Record<ModelTier, ModelConfig> = {
  strategic: {
    tier: 'strategic',
    preferredProvider: 'anthropic',
    preferredModel: 'claude-3-opus',
    fallbackProviders: ['openai', 'azure'],
    maxTokens: 4096,
    temperature: 0.7,
  },
  tactical: {
    tier: 'tactical',
    preferredProvider: 'anthropic',
    preferredModel: 'claude-3-sonnet',
    fallbackProviders: ['openai', 'ollama'],
    maxTokens: 2048,
    temperature: 0.5,
  },
  execution: {
    tier: 'execution',
    preferredProvider: 'ollama',
    preferredModel: 'llama3',
    fallbackProviders: ['anthropic', 'openai'],
    maxTokens: 1024,
    temperature: 0.3,
  },
};

// ============================================================================
// SCALING TYPES
// ============================================================================

export interface ScalingConfig {
  minInstances: number;                 // Always maintain at least this many
  maxInstances: number;                 // Never exceed this
  targetQueueDepth: number;             // Spawn new instance if queue exceeds this
  scaleUpCooldown: number;              // Seconds to wait between scale-ups
  scaleDownDelay: number;               // Seconds idle before terminating
  scaleToZero: boolean;                 // Allow scaling to 0 instances when idle
}

export const DEFAULT_SCALING_CONFIGS: Record<ModelTier, ScalingConfig> = {
  strategic: {
    minInstances: 1,
    maxInstances: 1,                    // Singletons - one source of truth
    targetQueueDepth: 5,
    scaleUpCooldown: 60,
    scaleDownDelay: 300,
    scaleToZero: false,
  },
  tactical: {
    minInstances: 1,
    maxInstances: 3,
    targetQueueDepth: 3,
    scaleUpCooldown: 30,
    scaleDownDelay: 120,
    scaleToZero: false,
  },
  execution: {
    minInstances: 0,
    maxInstances: 10,
    targetQueueDepth: 2,
    scaleUpCooldown: 10,
    scaleDownDelay: 60,
    scaleToZero: true,                  // Can scale to zero when no work
  },
};

// ============================================================================
// AUTHORITY TYPES
// ============================================================================

export type AutonomyLevel = 'conservative' | 'balanced' | 'aggressive';

export type ConflictResolution = 'always-escalate' | 'negotiate-first' | 'decide-notify';

export interface AutonomyBound {
  action: string;                       // "Adjust damage values"
  bounds: string;                       // "±15%"
  requiresApproval: boolean;            // Override for specific actions
}

export interface EscalationRule {
  trigger: string;                      // "TTK change exceeds 20%"
  escalateTo: string;                   // Role ID to escalate to
  timeout: number;                      // Seconds before auto-escalating further
}

// ============================================================================
// ROLE DEFINITION (Template)
// ============================================================================

export interface RoleDefinition {
  id: string;
  name: string;
  role: string;                         // Job title: "Combat Systems Designer"
  description: string;                  // Detailed description for system prompt
  icon: string;                         // react-icons name
  color: string;                        // Tailwind color
  
  // What this role owns
  owns: string[];                       // ["Weapon balance", "TTK", "Damage calcs"]
  capabilities: string[];               // ["generate_code", "analyze_balance", "run_tests"]
  
  // Authority
  autonomyBounds: AutonomyBound[];
  escalationRules: EscalationRule[];
  
  // Relationships (template-level)
  reportsTo: string | null;             // Role ID this reports to
  canDelegateTo: string[];              // Role IDs this can delegate work to
  negotiatesWith: {
    roleId: string;
    topics: string[];
  }[];
  
  // Resource configuration
  modelConfig: ModelConfig;
  scalingConfig: ScalingConfig;
  
  // Behavior
  autonomyLevel: AutonomyLevel;
  systemPromptTemplate: string;         // Jinja-style template for the system prompt
}

// ============================================================================
// AGENT INSTANCE (Runtime)
// ============================================================================

export type InstanceStatus = 
  | 'initializing'      // Spawning, loading context
  | 'idle'              // Ready for work
  | 'busy'              // Processing a task
  | 'negotiating'       // In conflict resolution with another instance
  | 'waiting-approval'  // Escalated, waiting for human
  | 'terminating'       // Shutting down
  | 'error';            // Failed, needs attention

export interface TaskAssignment {
  id: string;
  description: string;
  priority: number;
  assignedAt: string;
  deadline?: string;
  parentTaskId?: string;                // For subtasks
  delegatedBy?: string;                 // Instance ID that delegated this
}

export interface AgentInstance {
  id: string;                           // Unique instance ID: "combat-abc123"
  roleId: string;                       // Which role this instantiates
  instanceNumber: number;               // 1, 2, 3... for display: "Combat-1"
  
  // Lifecycle
  status: InstanceStatus;
  spawnedAt: string;
  lastActiveAt: string;
  
  // Current work
  currentTask: TaskAssignment | null;
  taskQueue: TaskAssignment[];
  
  // Metrics (for scaling decisions)
  metrics: {
    tasksCompleted: number;
    tasksEscalated: number;
    avgLatencyMs: number;
    errorCount: number;
    negotiationsParticipated: number;
  };
  
  // Context (for UI display)
  shortTermMemory: string[];            // Recent actions/decisions
  activeNegotiations: string[];         // IDs of ongoing negotiations
}

// ============================================================================
// NEGOTIATION TYPES
// ============================================================================

export type NegotiationStatus = 
  | 'initiated'
  | 'proposing'
  | 'counter-proposing'
  | 'converging'
  | 'agreed'
  | 'deadlocked'
  | 'escalated';

export interface Negotiation {
  id: string;
  topic: string;                        // "Loot value vs TTK balance"
  participants: string[];               // Instance IDs
  status: NegotiationStatus;
  rounds: {
    proposerId: string;
    proposal: string;
    timestamp: string;
  }[];
  resolution?: {
    outcome: string;
    agreedBy: string[];
    timestamp: string;
  };
  escalatedTo?: string;                 // Instance ID if escalated
  startedAt: string;
  updatedAt: string;
}

// ============================================================================
// RELATIONSHIP TYPES
// ============================================================================

export type RelationshipType = 
  | 'delegates'         // Authority flows down
  | 'reports-to'        // Accountability flows up
  | 'negotiates'        // Peer conflict resolution
  | 'observes';         // Read-only access to another's work

export interface Relationship {
  id: string;
  sourceRoleId: string;
  targetRoleId: string;
  type: RelationshipType;
  metadata?: {
    topics?: string[];                  // For negotiations
    permissions?: string[];             // For observes
    label?: string;
  };
}

// ============================================================================
// WORKFLOW / TEAM TYPES
// ============================================================================

export interface WorkflowSettings {
  name: string;
  description?: string;
  defaultAutonomyLevel: AutonomyLevel;
  conflictResolution: ConflictResolution;
  requireApprovalFor: string[];
  globalScalingMultiplier: number;      // 0.5 = half capacity, 2.0 = double
}

export interface Workflow {
  id: string;
  settings: WorkflowSettings;
  roles: RoleDefinition[];
  relationships: Relationship[];
  createdAt: string;
  updatedAt: string;
}

// ============================================================================
// RUNTIME STATE
// ============================================================================

export interface RuntimeState {
  workflowId: string;
  instances: AgentInstance[];
  negotiations: Negotiation[];
  taskQueue: TaskAssignment[];          // Unassigned tasks
  metrics: {
    totalTasksCompleted: number;
    avgLatencyMs: number;
    activeInstances: number;
    peakInstances: number;
    costEstimate: number;               // Based on token usage
  };
}

// ============================================================================
// VISUAL/UI TYPES
// ============================================================================

// For ReactFlow node rendering
export interface RoleNodeData {
  role: RoleDefinition;
  instances: AgentInstance[];
  isExpanded: boolean;
  onToggleExpand: (id: string) => void;
  onEditRole: (id: string) => void;
  onDeleteRole: (id: string) => void;
  onSpawnInstance: (roleId: string) => void;
  onTerminateInstance: (instanceId: string) => void;
}

export interface RelationshipEdgeData {
  relationship: Relationship;
  isActive: boolean;                    // Currently being used
  trafficVolume: number;                // For edge thickness
}

// ============================================================================
// GUIDED MODE TYPES
// ============================================================================

export type GuidedStep = 
  | 'welcome'
  | 'project-type'
  | 'team-structure'                    // NEW: Choose org pattern
  | 'autonomy-level'
  | 'scaling-preference'                // NEW: How aggressive to scale
  | 'conflict-resolution'
  | 'review';

export type OrgPattern = 
  | 'hierarchical'      // Military/corporate chain of command
  | 'surgical'          // One lead + support specialists
  | 'federated'         // Independent services with message passing
  | 'swarm';            // Emergent behavior, handoff-based

export type ScalingPreference = 
  | 'minimal'           // Keep instances low, accept latency
  | 'balanced'          // Scale moderately
  | 'aggressive';       // Scale fast, optimize for speed

export interface GuidedAnswers {
  projectType: string | null;
  orgPattern: OrgPattern | null;
  autonomyLevel: AutonomyLevel | null;
  scalingPreference: ScalingPreference | null;
  conflictResolution: ConflictResolution | null;
}

// ============================================================================
// VALIDATION TYPES
// ============================================================================

export interface WorkflowValidationError {
  code: string;
  message: string;
  severity: 'error' | 'warning' | 'info';
  roleId?: string;
  relationshipId?: string;
}
