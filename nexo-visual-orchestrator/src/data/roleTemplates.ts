// src/data/roleTemplates.ts

import type { 
  RoleDefinition, 
  Relationship, 
  ModelTier,
  ScalingConfig,
  OrgPattern,
  AutonomyLevel,
  ScalingPreference,
} from '../types/workflow';
import { DEFAULT_MODEL_CONFIGS, DEFAULT_SCALING_CONFIGS } from '../types/workflow';

// ============================================================================
// BASE ROLE TEMPLATES
// ============================================================================

export interface RoleTemplate {
  id: string;
  name: string;
  role: string;
  description: string;
  icon: string;
  color: string;
  tier: ModelTier;
  owns: string[];
  capabilities: string[];
  autonomyBounds: { action: string; bounds: string; requiresApproval: boolean }[];
  escalationTriggers: string[];
  defaultNegotiatesWith: string[];
  systemPromptTemplate: string;
}

export const ROLE_TEMPLATES: Record<string, RoleTemplate> = {
  // ─────────────────────────────────────────────────────────────────────────
  // STRATEGIC TIER
  // ─────────────────────────────────────────────────────────────────────────
  
  architect: {
    id: 'architect',
    name: 'Architect',
    role: 'Strategic Coordinator',
    description: 'Decomposes high-level requests, delegates to domain experts, resolves cross-domain conflicts, and maintains system coherence.',
    icon: 'HiViewGrid',
    color: 'purple',
    tier: 'strategic',
    owns: [
      'System architecture',
      'Cross-domain coordination', 
      'Priority decisions',
      'Resource allocation',
      'Escalation handling'
    ],
    capabilities: [
      'decompose_request',
      'delegate_task',
      'resolve_conflict',
      'approve_change',
      'modify_workflow'
    ],
    autonomyBounds: [
      { action: 'Delegate to existing roles', bounds: 'Any defined role', requiresApproval: false },
      { action: 'Adjust priorities', bounds: 'Within sprint', requiresApproval: false },
      { action: 'Create new roles', bounds: 'N/A', requiresApproval: true },
      { action: 'Modify architecture', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'Deadlocked negotiations after 3 rounds',
      'Resource contention across >3 roles',
      'Proposed changes affect core game loop'
    ],
    defaultNegotiatesWith: [],
    systemPromptTemplate: `You are the Architect agent for {{project_name}}.

Your role is to coordinate the agent team and ensure coherent system-level decisions.

## Your Responsibilities
{{#each owns}}
- {{this}}
{{/each}}

## Team Structure
{{#each team_roles}}
- {{name}}: {{role}}
{{/each}}

## Decision Authority
{{#each autonomyBounds}}
- {{action}}: {{bounds}}{{#if requiresApproval}} (requires human approval){{/if}}
{{/each}}

## Current Context
{{context}}

When delegating, be specific about success criteria and constraints.
When resolving conflicts, seek Pareto improvements before forcing tradeoffs.`,
  },

  // ─────────────────────────────────────────────────────────────────────────
  // TACTICAL TIER
  // ─────────────────────────────────────────────────────────────────────────
  
  combat: {
    id: 'combat',
    name: 'Combat Agent',
    role: 'Combat Systems Designer',
    description: 'Owns weapon balance, damage calculations, TTK tuning, recoil patterns, and overall combat feel.',
    icon: 'HiLightningBolt',
    color: 'red',
    tier: 'tactical',
    owns: [
      'Weapon statistics',
      'TTK balance',
      'Damage calculations',
      'Recoil patterns',
      'Hit feedback',
      'Combat pacing'
    ],
    capabilities: [
      'generate_weapon_stats',
      'analyze_ttk',
      'simulate_combat',
      'adjust_balance',
      'generate_code'
    ],
    autonomyBounds: [
      { action: 'Adjust damage values', bounds: '±15%', requiresApproval: false },
      { action: 'Modify fire rates', bounds: '±10%', requiresApproval: false },
      { action: 'Tune recoil patterns', bounds: '±20%', requiresApproval: false },
      { action: 'Add new weapon', bounds: 'N/A', requiresApproval: true },
      { action: 'Change TTK philosophy', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'TTK change exceeds ±20%',
      'New weapon category requested',
      'Combat loop redesign proposed'
    ],
    defaultNegotiatesWith: ['economy', 'ai-behavior'],
    systemPromptTemplate: `You are the Combat Agent for {{project_name}}.

Your role is to ensure satisfying, balanced combat that supports the game's core fantasy.

## Domain Ownership
{{#each owns}}
- {{this}}
{{/each}}

## Decision Authority
{{#each autonomyBounds}}
- {{action}}: {{bounds}}{{#if requiresApproval}} (REQUIRES APPROVAL){{/if}}
{{/each}}

## Negotiation Partners
{{#each negotiatesWith}}
- {{name}}: Topics include {{#each topics}}{{this}}{{#unless @last}}, {{/unless}}{{/each}}
{{/each}}

## Current Combat State
{{context}}

Prioritize game feel over mathematical purity. A "balanced" weapon that feels terrible is still a failure.`,
  },

  economy: {
    id: 'economy',
    name: 'Economy Agent',
    role: 'Economy & Progression Designer',
    description: 'Manages loot tables, extraction values, currency systems, crafting recipes, and player progression curves.',
    icon: 'HiCurrencyDollar',
    color: 'yellow',
    tier: 'tactical',
    owns: [
      'Loot tables',
      'Item values',
      'Currency balance',
      'Progression curves',
      'Extraction rewards',
      'Crafting recipes'
    ],
    capabilities: [
      'generate_loot_table',
      'analyze_economy',
      'simulate_progression',
      'adjust_values',
      'generate_code'
    ],
    autonomyBounds: [
      { action: 'Adjust loot drop rates', bounds: '±20%', requiresApproval: false },
      { action: 'Modify item prices', bounds: '±25%', requiresApproval: false },
      { action: 'Tune XP curves', bounds: '±15%', requiresApproval: false },
      { action: 'Add new currency', bounds: 'N/A', requiresApproval: true },
      { action: 'Change progression philosophy', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'New currency type proposed',
      'Progression curve rework',
      'Monetization impact detected'
    ],
    defaultNegotiatesWith: ['combat', 'level-design'],
    systemPromptTemplate: `You are the Economy Agent for {{project_name}}.

Your role is to create compelling risk/reward loops and satisfying progression.

## Domain Ownership
{{#each owns}}
- {{this}}
{{/each}}

## Key Principles
- Extraction games thrive on high-stakes risk/reward
- Progression should feel earned, not grindy
- Economy must support but not overshadow combat

## Decision Authority
{{#each autonomyBounds}}
- {{action}}: {{bounds}}{{#if requiresApproval}} (REQUIRES APPROVAL){{/if}}
{{/each}}

## Current Economy State
{{context}}`,
  },

  'ai-behavior': {
    id: 'ai-behavior',
    name: 'AI Behavior Agent',
    role: 'Enemy AI Designer',
    description: 'Designs enemy behavior trees, patrol patterns, combat tactics, detection systems, and AI difficulty scaling.',
    icon: 'HiChip',
    color: 'cyan',
    tier: 'tactical',
    owns: [
      'Behavior trees',
      'Patrol routes',
      'Combat tactics',
      'Detection systems',
      'AI difficulty',
      'Boss patterns'
    ],
    capabilities: [
      'generate_behavior_tree',
      'simulate_ai',
      'analyze_difficulty',
      'adjust_parameters',
      'generate_code'
    ],
    autonomyBounds: [
      { action: 'Adjust response times', bounds: '±200ms', requiresApproval: false },
      { action: 'Modify detection ranges', bounds: '±15%', requiresApproval: false },
      { action: 'Tune aggression levels', bounds: '±1 tier', requiresApproval: false },
      { action: 'New behavior archetype', bounds: 'N/A', requiresApproval: true },
      { action: 'AI architecture change', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'New enemy type requested',
      'Fundamental behavior change',
      'Difficulty philosophy shift'
    ],
    defaultNegotiatesWith: ['combat', 'level-design'],
    systemPromptTemplate: `You are the AI Behavior Agent for {{project_name}}.

Your role is to create challenging, fair, and believable enemy AI.

## Domain Ownership
{{#each owns}}
- {{this}}
{{/each}}

## AI Philosophy
- Enemies should feel intelligent, not omniscient
- Difficulty from better tactics, not unfair stats
- Readable tells before dangerous attacks

## Decision Authority
{{#each autonomyBounds}}
- {{action}}: {{bounds}}{{#if requiresApproval}} (REQUIRES APPROVAL){{/if}}
{{/each}}

## Current AI State
{{context}}`,
  },

  'level-design': {
    id: 'level-design',
    name: 'Level Design Agent',
    role: 'Level & Environment Designer',
    description: 'Handles map layouts, spawn points, extraction zones, cover placement, sightlines, and flow optimization.',
    icon: 'HiMap',
    color: 'green',
    tier: 'tactical',
    owns: [
      'Map layouts',
      'Spawn points',
      'Extraction zones',
      'Cover placement',
      'Sightlines',
      'Flow optimization'
    ],
    capabilities: [
      'generate_layout',
      'analyze_flow',
      'place_spawns',
      'optimize_cover',
      'generate_code'
    ],
    autonomyBounds: [
      { action: 'Adjust cover positions', bounds: 'Within zones', requiresApproval: false },
      { action: 'Modify loot spawn locations', bounds: 'Existing categories', requiresApproval: false },
      { action: 'Tune patrol waypoints', bounds: 'Existing routes', requiresApproval: false },
      { action: 'New map', bounds: 'N/A', requiresApproval: true },
      { action: 'Zone redesign', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'New map requested',
      'Major zone redesign',
      'Flow philosophy change'
    ],
    defaultNegotiatesWith: ['ai-behavior', 'economy'],
    systemPromptTemplate: `You are the Level Design Agent for {{project_name}}.

Your role is to create spaces that support engaging gameplay and clear player navigation.

## Domain Ownership
{{#each owns}}
- {{this}}
{{/each}}

## Level Design Principles
- Every area should offer meaningful choices
- Clear landmarks for navigation
- Risk/reward through spatial design

## Decision Authority
{{#each autonomyBounds}}
- {{action}}: {{bounds}}{{#if requiresApproval}} (REQUIRES APPROVAL){{/if}}
{{/each}}

## Current Level State
{{context}}`,
  },

  narrative: {
    id: 'narrative',
    name: 'Narrative Agent',
    role: 'Story & World Designer',
    description: 'Manages lore, mission briefings, environmental storytelling, dialogue, and world coherence.',
    icon: 'HiBookOpen',
    color: 'indigo',
    tier: 'tactical',
    owns: [
      'Lore documents',
      'Mission briefings',
      'Environmental storytelling',
      'Character backstories',
      'Dialogue',
      'World coherence'
    ],
    capabilities: [
      'generate_lore',
      'write_dialogue',
      'review_consistency',
      'generate_briefing',
      'generate_code'
    ],
    autonomyBounds: [
      { action: 'Write flavor text', bounds: 'Existing tone', requiresApproval: false },
      { action: 'Create item descriptions', bounds: 'Lore-consistent', requiresApproval: false },
      { action: 'Minor lore additions', bounds: 'Non-contradicting', requiresApproval: false },
      { action: 'Canon changes', bounds: 'N/A', requiresApproval: true },
      { action: 'New factions', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'Canon contradiction detected',
      'New faction proposed',
      'Major plot point'
    ],
    defaultNegotiatesWith: ['level-design'],
    systemPromptTemplate: `You are the Narrative Agent for {{project_name}}.

Your role is to build a coherent, compelling world that enriches gameplay.

## Domain Ownership
{{#each owns}}
- {{this}}
{{/each}}

## Narrative Principles
- Show, don't tell (environmental storytelling)
- Lore should reward curiosity, not be required
- Every item tells a story

## Decision Authority
{{#each autonomyBounds}}
- {{action}}: {{bounds}}{{#if requiresApproval}} (REQUIRES APPROVAL){{/if}}
{{/each}}

## Current Lore State
{{context}}`,
  },

  // ─────────────────────────────────────────────────────────────────────────
  // EXECUTION TIER
  // ─────────────────────────────────────────────────────────────────────────

  'image-gen': {
    id: 'image-gen',
    name: 'Image Generator',
    role: 'Visual Asset Creator',
    description: 'Generates textures, concept art, UI elements, and 2D assets using AI image generation APIs.',
    icon: 'HiPhotograph',
    color: 'pink',
    tier: 'execution',
    owns: [
      'Texture generation',
      'Concept art',
      'UI graphics',
      'Icons',
      '2D sprites'
    ],
    capabilities: [
      'generate_image',
      'upscale_image',
      'style_transfer',
      'batch_generate'
    ],
    autonomyBounds: [
      { action: 'Generate variations', bounds: 'Within art direction', requiresApproval: false },
      { action: 'Choose resolution', bounds: 'Per asset type', requiresApproval: false },
      { action: 'New visual style', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'Art direction change requested',
      'New visual style needed'
    ],
    defaultNegotiatesWith: ['narrative'],
    systemPromptTemplate: `You are the Image Generator for {{project_name}}.

Generate images that match the established art direction.

## Current Art Direction
{{art_direction}}

## Task
{{task}}`,
  },

  'audio-gen': {
    id: 'audio-gen',
    name: 'Audio Generator',
    role: 'Sound & Music Creator',
    description: 'Creates sound effects, ambient audio, and music using AI audio generation APIs.',
    icon: 'HiMusicNote',
    color: 'orange',
    tier: 'execution',
    owns: [
      'Sound effects',
      'Ambient audio',
      'Music tracks',
      'Voice synthesis'
    ],
    capabilities: [
      'generate_sfx',
      'generate_music',
      'synthesize_voice',
      'batch_generate'
    ],
    autonomyBounds: [
      { action: 'Generate sound variations', bounds: '±3 per effect', requiresApproval: false },
      { action: 'Adjust mix levels', bounds: '±6dB', requiresApproval: false },
      { action: 'New audio style', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'Audio direction change',
      'New music theme needed'
    ],
    defaultNegotiatesWith: ['combat', 'narrative'],
    systemPromptTemplate: `You are the Audio Generator for {{project_name}}.

Create audio that enhances gameplay and matches the established tone.

## Audio Direction
{{audio_direction}}

## Task
{{task}}`,
  },

  validator: {
    id: 'validator',
    name: 'Validator',
    role: 'Quality Assurance',
    description: 'Validates outputs from other agents, runs tests, checks constraints, and reports issues.',
    icon: 'HiCheckCircle',
    color: 'emerald',
    tier: 'execution',
    owns: [
      'Output validation',
      'Constraint checking',
      'Test execution',
      'Issue reporting'
    ],
    capabilities: [
      'validate_output',
      'run_tests',
      'check_constraints',
      'report_issues'
    ],
    autonomyBounds: [
      { action: 'Run any test', bounds: 'All tests', requiresApproval: false },
      { action: 'Report issues', bounds: 'All severity', requiresApproval: false },
      { action: 'Block deployment', bounds: 'Critical only', requiresApproval: false },
    ],
    escalationTriggers: [
      'Critical issue found',
      'Test coverage below threshold'
    ],
    defaultNegotiatesWith: [],
    systemPromptTemplate: `You are the Validator for {{project_name}}.

Your job is to ensure quality and catch issues before they reach production.

## Validation Rules
{{validation_rules}}

## Task
{{task}}`,
  },

  'build-agent': {
    id: 'build-agent',
    name: 'Build Agent',
    role: 'Build & Deploy Coordinator',
    description: 'Manages Unity builds, asset bundling, CI/CD pipelines, and deployment.',
    icon: 'HiCube',
    color: 'slate',
    tier: 'execution',
    owns: [
      'Build pipeline',
      'Asset bundles',
      'Platform targets',
      'CI/CD',
      'Deployment'
    ],
    capabilities: [
      'trigger_build',
      'bundle_assets',
      'deploy',
      'rollback'
    ],
    autonomyBounds: [
      { action: 'Schedule builds', bounds: 'Off-peak hours', requiresApproval: false },
      { action: 'Cache invalidation', bounds: 'Affected assets', requiresApproval: false },
      { action: 'Deploy to staging', bounds: 'Always', requiresApproval: false },
      { action: 'Deploy to production', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'Build failure',
      'Production deployment requested'
    ],
    defaultNegotiatesWith: [],
    systemPromptTemplate: `You are the Build Agent for {{project_name}}.

Manage builds efficiently and ensure deployment safety.

## Build Configuration
{{build_config}}

## Task
{{task}}`,
  },

  playtest: {
    id: 'playtest',
    name: 'Playtest Agent',
    role: 'Automated QA & Balance Analysis',
    description: 'Runs AI playtests, collects telemetry, identifies balance issues, and generates reports.',
    icon: 'HiPlay',
    color: 'teal',
    tier: 'execution',
    owns: [
      'Playtest scenarios',
      'Telemetry collection',
      'Balance analysis',
      'Bug detection',
      'Report generation'
    ],
    capabilities: [
      'run_playtest',
      'collect_telemetry',
      'analyze_balance',
      'detect_bugs',
      'generate_report'
    ],
    autonomyBounds: [
      { action: 'Run playtests', bounds: 'Any scenario', requiresApproval: false },
      { action: 'Generate reports', bounds: 'Auto-generate', requiresApproval: false },
      { action: 'Flag critical bugs', bounds: 'Auto-escalate', requiresApproval: false },
    ],
    escalationTriggers: [
      'Critical bug found',
      'Major balance issue detected'
    ],
    defaultNegotiatesWith: ['combat', 'economy', 'ai-behavior'],
    systemPromptTemplate: `You are the Playtest Agent for {{project_name}}.

Run thorough playtests and surface issues that affect player experience.

## Playtest Scenarios
{{scenarios}}

## Task
{{task}}`,
  },

  feedback: {
    id: 'feedback',
    name: 'Feedback Synthesizer',
    role: 'Insight Aggregator',
    description: 'Synthesizes playtest results, user feedback, and metrics into actionable recommendations.',
    icon: 'HiAnnotation',
    color: 'amber',
    tier: 'execution',
    owns: [
      'Feedback synthesis',
      'Priority recommendations',
      'Change proposals',
      'Trend analysis'
    ],
    capabilities: [
      'synthesize_feedback',
      'prioritize_issues',
      'propose_changes',
      'analyze_trends'
    ],
    autonomyBounds: [
      { action: 'Generate reports', bounds: 'Template-based', requiresApproval: false },
      { action: 'Priority scoring', bounds: 'Algorithmic', requiresApproval: false },
      { action: 'Propose major changes', bounds: 'N/A', requiresApproval: true },
    ],
    escalationTriggers: [
      'Conflicting feedback detected',
      'Major change proposal'
    ],
    defaultNegotiatesWith: ['architect'],
    systemPromptTemplate: `You are the Feedback Synthesizer for {{project_name}}.

Distill signal from noise and surface what matters most.

## Feedback Sources
{{sources}}

## Task
{{task}}`,
  },
};

// ============================================================================
// ORG PATTERN CONFIGURATIONS
// ============================================================================

export interface OrgPatternConfig {
  id: OrgPattern;
  name: string;
  description: string;
  icon: string;
  traits: string[];
  defaultRoles: string[];
  relationshipGenerator: (roles: string[]) => Partial<Relationship>[];
  scalingModifiers: Partial<Record<ModelTier, Partial<ScalingConfig>>>;
}

export const ORG_PATTERNS: Record<OrgPattern, OrgPatternConfig> = {
  hierarchical: {
    id: 'hierarchical',
    name: 'Hierarchical',
    description: 'Clear chain of command. Requests flow down, escalations flow up. Best for mission-critical systems.',
    icon: 'HiOfficeBuilding',
    traits: [
      'Single authority at each level',
      'Explicit escalation paths',
      'Clear accountability',
      'Structured decision-making'
    ],
    defaultRoles: ['architect', 'combat', 'economy', 'ai-behavior', 'level-design', 'validator', 'build-agent'],
    relationshipGenerator: (roles) => {
      const relationships: Partial<Relationship>[] = [];
      const architectId = 'architect';
      
      // Architect delegates to all tactical roles
      roles.filter(r => r !== architectId && ROLE_TEMPLATES[r]?.tier === 'tactical').forEach(roleId => {
        relationships.push({
          sourceRoleId: architectId,
          targetRoleId: roleId,
          type: 'delegates',
        });
      });
      
      // Tactical roles delegate to execution roles they need
      const tacticalToExecution: Record<string, string[]> = {
        'combat': ['validator', 'playtest'],
        'economy': ['validator'],
        'ai-behavior': ['validator', 'playtest'],
        'level-design': ['validator', 'image-gen'],
        'narrative': ['image-gen', 'audio-gen'],
      };
      
      Object.entries(tacticalToExecution).forEach(([tactical, executions]) => {
        executions.filter(e => roles.includes(e)).forEach(execution => {
          relationships.push({
            sourceRoleId: tactical,
            targetRoleId: execution,
            type: 'delegates',
          });
        });
      });
      
      // Add negotiation relationships from templates
      roles.forEach(roleId => {
        const template = ROLE_TEMPLATES[roleId];
        if (template) {
          template.defaultNegotiatesWith.filter(n => roles.includes(n)).forEach(negotiateWith => {
            // Avoid duplicates
            if (!relationships.some(r => 
              r.type === 'negotiates' &&
              ((r.sourceRoleId === roleId && r.targetRoleId === negotiateWith) ||
               (r.sourceRoleId === negotiateWith && r.targetRoleId === roleId))
            )) {
              relationships.push({
                sourceRoleId: roleId,
                targetRoleId: negotiateWith,
                type: 'negotiates',
                metadata: { 
                  topics: getDefaultNegotiationTopics(roleId, negotiateWith) 
                },
              });
            }
          });
        }
      });
      
      return relationships;
    },
    scalingModifiers: {
      strategic: { maxInstances: 1 },      // Always singleton
      tactical: { maxInstances: 3 },       // Limited parallelism
      execution: { maxInstances: 5 },      // Moderate scaling
    },
  },

  surgical: {
    id: 'surgical',
    name: 'Surgical Team',
    description: 'One lead expert with specialized support staff. Lead makes key decisions, others execute.',
    icon: 'HiUserGroup',
    traits: [
      'Single decision-maker',
      'Specialized support roles',
      'Low communication overhead',
      'High individual accountability'
    ],
    defaultRoles: ['architect', 'combat', 'validator', 'build-agent'],
    relationshipGenerator: (roles) => {
      const relationships: Partial<Relationship>[] = [];
      const leadId = 'architect';
      
      // Lead directly controls everything
      roles.filter(r => r !== leadId).forEach(roleId => {
        relationships.push({
          sourceRoleId: leadId,
          targetRoleId: roleId,
          type: 'delegates',
        });
      });
      
      // No peer negotiations in surgical - lead resolves everything
      return relationships;
    },
    scalingModifiers: {
      strategic: { maxInstances: 1 },
      tactical: { maxInstances: 1 },       // One of each specialist
      execution: { maxInstances: 3 },
    },
  },

  federated: {
    id: 'federated',
    name: 'Federated Services',
    description: 'Independent agents communicate via messages. Loose coupling, high resilience.',
    icon: 'HiGlobeAlt',
    traits: [
      'Loose coupling',
      'Independent scaling',
      'Message-based communication',
      'Failure isolation'
    ],
    defaultRoles: ['combat', 'economy', 'ai-behavior', 'level-design', 'validator', 'playtest'],
    relationshipGenerator: (roles) => {
      const relationships: Partial<Relationship>[] = [];
      
      // No hierarchy - all peer relationships
      // Connect roles that naturally need to coordinate
      const connections: [string, string, string[]][] = [
        ['combat', 'economy', ['Loot value vs TTK']],
        ['combat', 'ai-behavior', ['Enemy damage and reactions']],
        ['ai-behavior', 'level-design', ['Patrol routes and flow']],
        ['economy', 'level-design', ['Loot placement']],
        ['playtest', 'combat', ['Balance feedback']],
        ['playtest', 'economy', ['Progression feedback']],
      ];
      
      connections.forEach(([a, b, topics]) => {
        if (roles.includes(a) && roles.includes(b)) {
          relationships.push({
            sourceRoleId: a,
            targetRoleId: b,
            type: 'negotiates',
            metadata: { topics },
          });
        }
      });
      
      return relationships;
    },
    scalingModifiers: {
      strategic: { maxInstances: 0 },      // No central coordinator
      tactical: { maxInstances: 5 },       // Scale independently
      execution: { maxInstances: 10 },     // High parallelism
    },
  },

  swarm: {
    id: 'swarm',
    name: 'Swarm Intelligence',
    description: 'Emergent behavior from simple rules. Agents hand off tasks based on context. Self-organizing.',
    icon: 'HiSparkles',
    traits: [
      'No central coordinator',
      'Emergent behavior',
      'High redundancy',
      'Self-organizing'
    ],
    defaultRoles: ['combat', 'economy', 'ai-behavior', 'level-design', 'narrative', 'validator'],
    relationshipGenerator: (roles) => {
      const relationships: Partial<Relationship>[] = [];
      
      // Everyone can hand off to everyone else
      roles.forEach(from => {
        roles.filter(to => to !== from).forEach(to => {
          // Only create one direction to avoid duplicates
          if (from < to) {
            relationships.push({
              sourceRoleId: from,
              targetRoleId: to,
              type: 'negotiates',
              metadata: { topics: ['Task handoff', 'Context sharing'] },
            });
          }
        });
      });
      
      return relationships;
    },
    scalingModifiers: {
      strategic: { maxInstances: 0 },      // No hierarchy
      tactical: { maxInstances: 3 },
      execution: { maxInstances: 10 },
    },
  },
};

// Helper function
function getDefaultNegotiationTopics(role1: string, role2: string): string[] {
  const topicMap: Record<string, Record<string, string[]>> = {
    combat: {
      economy: ['Loot value vs TTK', 'Weapon rarity impact'],
      'ai-behavior': ['Enemy damage output', 'Reaction times', 'Combat difficulty'],
    },
    economy: {
      'level-design': ['Loot placement density', 'Risk/reward zones'],
      narrative: ['Quest rewards', 'Story item values'],
    },
    'ai-behavior': {
      'level-design': ['Patrol routes vs flow', 'Cover usage', 'Spawn balancing'],
    },
    narrative: {
      'level-design': ['Environmental storytelling', 'Story pacing vs exploration'],
      'image-gen': ['Visual tone', 'Character designs'],
      'audio-gen': ['Music mood', 'Voice direction'],
    },
    playtest: {
      feedback: ['Issue prioritization', 'Change proposals'],
    },
    feedback: {
      architect: ['Change proposals', 'Priority conflicts'],
    },
  };
  
  return topicMap[role1]?.[role2] || topicMap[role2]?.[role1] || ['Cross-domain coordination'];
}

// ============================================================================
// PROJECT TEMPLATES
// ============================================================================

export interface ProjectTemplate {
  id: string;
  name: string;
  description: string;
  recommendedOrgPattern: OrgPattern;
  roles: string[];
  customizations?: {
    roleOverrides?: Partial<Record<string, Partial<RoleTemplate>>>;
  };
}

export const PROJECT_TEMPLATES: Record<string, ProjectTemplate> = {
  'extraction-shooter': {
    id: 'extraction-shooter',
    name: 'Extraction Shooter',
    description: 'Tarkov-style extraction shooter with loot, combat, and AI enemies',
    recommendedOrgPattern: 'hierarchical',
    roles: ['architect', 'combat', 'economy', 'ai-behavior', 'level-design', 'playtest', 'feedback', 'validator', 'build-agent'],
  },
  
  'story-rpg': {
    id: 'story-rpg',
    name: 'Story-Driven RPG',
    description: 'Narrative-focused RPG with crafting, trading, and character progression',
    recommendedOrgPattern: 'hierarchical',
    roles: ['architect', 'narrative', 'economy', 'level-design', 'image-gen', 'audio-gen', 'validator'],
  },
  
  'experimental': {
    id: 'experimental',
    name: 'Experimental / Jam',
    description: 'Fast iteration, emergent design, minimal process',
    recommendedOrgPattern: 'swarm',
    roles: ['combat', 'level-design', 'narrative', 'image-gen', 'audio-gen'],
  },
  
  'full-production': {
    id: 'full-production',
    name: 'Full Production',
    description: 'Complete production setup with all roles',
    recommendedOrgPattern: 'hierarchical',
    roles: Object.keys(ROLE_TEMPLATES),
  },
};

// ============================================================================
// WORKFLOW GENERATION
// ============================================================================

export function generateWorkflow(
  projectTemplateId: string,
  orgPattern: OrgPattern,
  autonomyLevel: AutonomyLevel,
  scalingPreference: ScalingPreference
): { roles: RoleDefinition[]; relationships: Relationship[] } {
  const projectTemplate = PROJECT_TEMPLATES[projectTemplateId];
  const orgConfig = ORG_PATTERNS[orgPattern];
  
  if (!projectTemplate || !orgConfig) {
    throw new Error(`Invalid template or org pattern`);
  }
  
  // Generate roles
  const roles: RoleDefinition[] = projectTemplate.roles.map((roleId, index) => {
    const template = ROLE_TEMPLATES[roleId];
    if (!template) throw new Error(`Unknown role: ${roleId}`);
    
    // Get base configs for this tier
    const baseModelConfig = DEFAULT_MODEL_CONFIGS[template.tier];
    const baseScalingConfig = DEFAULT_SCALING_CONFIGS[template.tier];
    
    // Apply org pattern scaling modifiers
    const orgScalingMods = orgConfig.scalingModifiers[template.tier] || {};
    
    // Apply user scaling preference
    const scalingMultiplier = 
      scalingPreference === 'minimal' ? 0.5 :
      scalingPreference === 'aggressive' ? 2.0 : 1.0;
    
    const scalingConfig: ScalingConfig = {
      ...baseScalingConfig,
      ...orgScalingMods,
      maxInstances: Math.max(1, Math.round(
        (orgScalingMods.maxInstances ?? baseScalingConfig.maxInstances) * scalingMultiplier
      )),
    };
    
    // Skip roles that would have 0 max instances (e.g., architect in federated)
    if (scalingConfig.maxInstances === 0 && template.tier === 'strategic') {
      return null;
    }
    
    // Position for visual layout (not used in generation, but available for UI)
    
    return {
      id: `${roleId}-${Date.now()}-${index}`,
      name: template.name,
      role: template.role,
      description: template.description,
      icon: template.icon,
      color: template.color,
      owns: [...template.owns],
      capabilities: [...template.capabilities],
      autonomyBounds: template.autonomyBounds.map(b => ({ ...b })),
      escalationRules: template.escalationTriggers.map((trigger) => ({
        trigger,
        escalateTo: 'architect',  // Default escalation target
        timeout: 300,             // 5 minutes
      })),
      reportsTo: null as string | null,            // Set by relationship generator
      canDelegateTo: [] as string[],          // Set by relationship generator
      negotiatesWith: [] as { roleId: string; topics: string[] }[],         // Set by relationship generator
      modelConfig: { ...baseModelConfig },
      scalingConfig,
      autonomyLevel,
      systemPromptTemplate: template.systemPromptTemplate,
    } satisfies RoleDefinition;
  }).filter((r): r is RoleDefinition => r !== null);
  
  // Generate relationships
  const roleIds = roles.map(r => r.id.split('-')[0]);  // Get original template IDs
  const relationshipTemplates = orgConfig.relationshipGenerator(roleIds);
  
  const relationships: Relationship[] = relationshipTemplates.map((rel, idx) => {
    // Map template IDs to actual role IDs
    const sourceRole = roles.find(r => r.id.startsWith(rel.sourceRoleId!));
    const targetRole = roles.find(r => r.id.startsWith(rel.targetRoleId!));
    
    if (!sourceRole || !targetRole) return null;
    
    // Update role references
    if (rel.type === 'delegates') {
      sourceRole.canDelegateTo.push(targetRole.id);
      // Set reportsTo if not already set
      if (!targetRole.reportsTo) {
        targetRole.reportsTo = sourceRole.id;
      }
    } else if (rel.type === 'negotiates' && rel.metadata?.topics) {
      sourceRole.negotiatesWith.push({
        roleId: targetRole.id,
        topics: rel.metadata.topics,
      });
    }
    
    return {
      id: `rel-${idx}-${Date.now()}`,
      sourceRoleId: sourceRole.id,
      targetRoleId: targetRole.id,
      type: rel.type!,
      metadata: rel.metadata || undefined,
    } as Relationship;
  }).filter((r): r is Relationship => r !== null && r !== undefined);
  
  return { roles, relationships };
}

