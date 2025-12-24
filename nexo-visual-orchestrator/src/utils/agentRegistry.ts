import type { AgentDefinition, AgentType, AgentCategory } from '../types/agents';

export const AGENT_REGISTRY: Record<AgentType, AgentDefinition> = {
  architect: {
    type: 'architect',
    category: 'architect',
    label: 'Architect',
    description: 'Entry point that decomposes requests into agent specifications and orchestrates the workflow.',
    icon: '[ARCH]',
    color: '#6366F1',
    inputs: [
      { id: 'request', label: 'Request', dataType: 'text', required: true, multiple: false },
    ],
    outputs: [
      { id: 'specs', label: 'Agent Specs', dataType: 'json', required: true, multiple: true },
    ],
    configSchema: {
      properties: {
        request: {
          type: 'textarea',
          label: 'Request',
          description: 'Describe what you want to build',
          default: '',
        },
        gameType: {
          type: 'select',
          label: 'Game Type',
          options: [
            { value: 'extraction-shooter', label: 'Extraction Shooter' },
            { value: 'roguelike', label: 'Roguelike' },
            { value: 'city-builder', label: 'City Builder' },
            { value: 'rpg', label: 'RPG' },
            { value: 'platformer', label: 'Platformer' },
            { value: 'custom', label: 'Custom' },
          ],
          default: 'extraction-shooter',
        },
        maxAgents: {
          type: 'number',
          label: 'Max Agents',
          description: 'Maximum number of agents to spawn',
          default: 10,
          min: 1,
          max: 50,
        },
      },
      required: ['request'],
    },
    defaultConfig: {
      request: '',
      gameType: 'extraction-shooter',
      maxAgents: 10,
    },
  },

  combat: {
    type: 'combat',
    category: 'domain',
    label: 'Combat',
    description: 'Designs combat systems, weapons, damage models, TTK, and PvP/PvE mechanics.',
    icon: '[COMBAT]',
    color: '#EF4444',
    inputs: [
      { id: 'spec', label: 'Spec', dataType: 'json', required: true, multiple: false },
      { id: 'constraints', label: 'Constraints', dataType: 'json', required: false, multiple: true },
    ],
    outputs: [
      { id: 'design', label: 'Combat Design', dataType: 'json', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        ttk: {
          type: 'string',
          label: 'Time-to-Kill Range',
          description: 'Target TTK range in seconds',
          default: '0.5-2.0s',
        },
        permadeath: {
          type: 'boolean',
          label: 'Permadeath',
          description: 'Enable permanent death mechanics',
          default: true,
        },
        friendlyFire: {
          type: 'boolean',
          label: 'Friendly Fire',
          default: false,
        },
        healingMechanics: {
          type: 'select',
          label: 'Healing',
          options: [
            { value: 'none', label: 'None' },
            { value: 'items', label: 'Items Only' },
            { value: 'regen', label: 'Regeneration' },
            { value: 'both', label: 'Items + Regen' },
          ],
          default: 'items',
        },
      },
      required: [],
    },
    defaultConfig: {
      ttk: '0.5-2.0s',
      permadeath: true,
      friendlyFire: false,
      healingMechanics: 'items',
    },
  },

  economy: {
    type: 'economy',
    category: 'domain',
    label: 'Economy',
    description: 'Designs in-game economies, loot tables, trading systems, currencies, and resource flows.',
    icon: '[ECON]',
    color: '#F59E0B',
    inputs: [
      { id: 'spec', label: 'Spec', dataType: 'json', required: true, multiple: false },
      { id: 'constraints', label: 'Constraints', dataType: 'json', required: false, multiple: true },
    ],
    outputs: [
      { id: 'design', label: 'Economy Design', dataType: 'json', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        currencyTypes: {
          type: 'number',
          label: 'Currency Types',
          default: 1,
          min: 1,
          max: 10,
        },
        lootPersistence: {
          type: 'boolean',
          label: 'Loot Persistence',
          description: 'Items persist between sessions',
          default: true,
        },
        trading: {
          type: 'boolean',
          label: 'Player Trading',
          default: true,
        },
        inflationControl: {
          type: 'boolean',
          label: 'Inflation Control',
          description: 'Implement currency sinks',
          default: true,
        },
      },
      required: [],
    },
    defaultConfig: {
      currencyTypes: 1,
      lootPersistence: true,
      trading: true,
      inflationControl: true,
    },
  },

  'ai-behavior': {
    type: 'ai-behavior',
    category: 'domain',
    label: 'AI Behavior',
    description: 'Designs NPC AI, behavior trees, threat assessment, patrol patterns, and squad tactics.',
    icon: '[AI]',
    color: '#8B5CF6',
    inputs: [
      { id: 'spec', label: 'Spec', dataType: 'json', required: true, multiple: false },
    ],
    outputs: [
      { id: 'design', label: 'AI Design', dataType: 'json', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        complexity: {
          type: 'select',
          label: 'Behavior Complexity',
          options: [
            { value: 'simple', label: 'Simple (State Machine)' },
            { value: 'moderate', label: 'Moderate (Behavior Tree)' },
            { value: 'complex', label: 'Complex (GOAP/Utility)' },
          ],
          default: 'moderate',
        },
        difficultyScaling: {
          type: 'boolean',
          label: 'Difficulty Scaling',
          default: true,
        },
        squadBehavior: {
          type: 'boolean',
          label: 'Squad Coordination',
          default: true,
        },
      },
      required: [],
    },
    defaultConfig: {
      complexity: 'moderate',
      difficultyScaling: true,
      squadBehavior: true,
    },
  },

  'level-design': {
    type: 'level-design',
    category: 'domain',
    label: 'Level Design',
    description: 'Designs map layouts, spawn points, extraction zones, and environmental hazards.',
    icon: '[MAP]',
    color: '#10B981',
    inputs: [
      { id: 'spec', label: 'Spec', dataType: 'json', required: true, multiple: false },
    ],
    outputs: [
      { id: 'design', label: 'Level Design', dataType: 'json', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        mapSize: {
          type: 'select',
          label: 'Map Size',
          options: [
            { value: 'small', label: 'Small (1-4 players)' },
            { value: 'medium', label: 'Medium (4-8 players)' },
            { value: 'large', label: 'Large (8-16 players)' },
          ],
          default: 'medium',
        },
        extractionPoints: {
          type: 'number',
          label: 'Extraction Points',
          default: 3,
          min: 1,
          max: 10,
        },
        verticalLayers: {
          type: 'number',
          label: 'Vertical Layers',
          description: 'Number of floor levels',
          default: 2,
          min: 1,
          max: 5,
        },
      },
      required: [],
    },
    defaultConfig: {
      mapSize: 'medium',
      extractionPoints: 3,
      verticalLayers: 2,
    },
  },

  narrative: {
    type: 'narrative',
    category: 'domain',
    label: 'Narrative',
    description: 'Designs story elements, lore, quest structures, and dialogue systems.',
    icon: '[STORY]',
    color: '#A855F7',
    inputs: [
      { id: 'spec', label: 'Spec', dataType: 'json', required: true, multiple: false },
    ],
    outputs: [
      { id: 'design', label: 'Narrative Design', dataType: 'json', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        narrativeDepth: {
          type: 'select',
          label: 'Narrative Depth',
          options: [
            { value: 'minimal', label: 'Minimal (Environmental)' },
            { value: 'moderate', label: 'Moderate (Quests + Lore)' },
            { value: 'deep', label: 'Deep (Branching Story)' },
          ],
          default: 'moderate',
        },
        dialogueSystem: {
          type: 'boolean',
          label: 'Dialogue System',
          default: false,
        },
      },
      required: [],
    },
    defaultConfig: {
      narrativeDepth: 'moderate',
      dialogueSystem: false,
    },
  },

  'image-gen': {
    type: 'image-gen',
    category: 'asset',
    label: 'Image Generator',
    description: 'Generates 2D images, textures, UI elements, and concept art using AI.',
    icon: '[IMG]',
    color: '#EC4899',
    inputs: [
      { id: 'prompt', label: 'Prompt', dataType: 'text', required: true, multiple: false },
      { id: 'style', label: 'Style Guide', dataType: 'json', required: false, multiple: false },
    ],
    outputs: [
      { id: 'image', label: 'Generated Image', dataType: 'binary', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        provider: {
          type: 'select',
          label: 'Provider',
          options: [
            { value: 'dalle', label: 'DALL-E 3' },
            { value: 'stable-diffusion', label: 'Stable Diffusion' },
            { value: 'ollama', label: 'Ollama (Local)' },
          ],
          default: 'dalle',
        },
        size: {
          type: 'select',
          label: 'Size',
          options: [
            { value: '512x512', label: '512×512' },
            { value: '1024x1024', label: '1024×1024' },
            { value: '1792x1024', label: '1792×1024 (Wide)' },
          ],
          default: '1024x1024',
        },
        style: {
          type: 'select',
          label: 'Style',
          options: [
            { value: 'vivid', label: 'Vivid' },
            { value: 'natural', label: 'Natural' },
          ],
          default: 'vivid',
        },
      },
      required: ['provider'],
    },
    defaultConfig: {
      provider: 'dalle',
      size: '1024x1024',
      style: 'vivid',
    },
  },

  'audio-gen': {
    type: 'audio-gen',
    category: 'asset',
    label: 'Audio Generator',
    description: 'Generates sound effects, music, and voice using AI audio synthesis.',
    icon: '[AUDIO]',
    color: '#F472B6',
    inputs: [
      { id: 'prompt', label: 'Prompt', dataType: 'text', required: true, multiple: false },
    ],
    outputs: [
      { id: 'audio', label: 'Generated Audio', dataType: 'binary', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        provider: {
          type: 'select',
          label: 'Provider',
          options: [
            { value: 'elevenlabs', label: 'ElevenLabs (Voice)' },
            { value: 'suno', label: 'Suno (Music)' },
            { value: 'bark', label: 'Bark (Local)' },
          ],
          default: 'suno',
        },
        duration: {
          type: 'number',
          label: 'Duration (seconds)',
          default: 30,
          min: 1,
          max: 300,
        },
      },
      required: ['provider'],
    },
    defaultConfig: {
      provider: 'suno',
      duration: 30,
    },
  },

  'model-3d-gen': {
    type: 'model-3d-gen',
    category: 'asset',
    label: '3D Model Generator',
    description: 'Generates 3D models, characters, and environments using AI.',
    icon: '[3D]',
    color: '#D946EF',
    inputs: [
      { id: 'prompt', label: 'Prompt', dataType: 'text', required: true, multiple: false },
      { id: 'reference', label: 'Reference Image', dataType: 'binary', required: false, multiple: false },
    ],
    outputs: [
      { id: 'model', label: 'Generated Model', dataType: 'binary', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        provider: {
          type: 'select',
          label: 'Provider',
          options: [
            { value: 'meshy', label: 'Meshy' },
            { value: 'tripo', label: 'Tripo3D' },
            { value: 'local', label: 'Local Model' },
          ],
          default: 'meshy',
        },
        format: {
          type: 'select',
          label: 'Output Format',
          options: [
            { value: 'fbx', label: 'FBX' },
            { value: 'glb', label: 'GLB' },
            { value: 'obj', label: 'OBJ' },
          ],
          default: 'fbx',
        },
      },
      required: ['provider'],
    },
    defaultConfig: {
      provider: 'meshy',
      format: 'fbx',
    },
  },

  'unity-build': {
    type: 'unity-build',
    category: 'build',
    label: 'Unity Build',
    description: 'Executes Unity Editor builds with specified configuration and platform targets.',
    icon: '[BUILD]',
    color: '#10B981',
    inputs: [
      { id: 'project', label: 'Project Path', dataType: 'text', required: true, multiple: false },
      { id: 'assets', label: 'Assets', dataType: 'binary', required: false, multiple: true },
      { id: 'design', label: 'Design Data', dataType: 'json', required: false, multiple: true },
    ],
    outputs: [
      { id: 'build', label: 'Build Artifact', dataType: 'binary', required: true, multiple: false },
      { id: 'report', label: 'Build Report', dataType: 'json', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        platform: {
          type: 'select',
          label: 'Target Platform',
          options: [
            { value: 'StandaloneWindows64', label: 'Windows 64-bit' },
            { value: 'StandaloneOSX', label: 'macOS' },
            { value: 'StandaloneLinux64', label: 'Linux 64-bit' },
            { value: 'WebGL', label: 'WebGL' },
          ],
          default: 'StandaloneWindows64',
        },
        development: {
          type: 'boolean',
          label: 'Development Build',
          default: true,
        },
        scenes: {
          type: 'string',
          label: 'Scenes',
          description: 'Comma-separated scene paths',
          default: '',
        },
      },
      required: ['platform'],
    },
    defaultConfig: {
      platform: 'StandaloneWindows64',
      development: true,
      scenes: '',
    },
  },

  'ai-player': {
    type: 'ai-player',
    category: 'playtest',
    label: 'AI Player',
    description: 'LLM-driven AI player that tests gameplay, makes decisions, and generates telemetry.',
    icon: '[PLAY]',
    color: '#06B6D4',
    inputs: [
      { id: 'build', label: 'Game Build', dataType: 'binary', required: true, multiple: false },
      { id: 'instructions', label: 'Test Instructions', dataType: 'text', required: false, multiple: false },
    ],
    outputs: [
      { id: 'session', label: 'Play Session', dataType: 'json', required: true, multiple: false },
      { id: 'telemetry', label: 'Telemetry', dataType: 'json', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        profile: {
          type: 'select',
          label: 'Player Profile',
          options: [
            { value: 'aggressive', label: 'Aggressive' },
            { value: 'passive', label: 'Passive/Defensive' },
            { value: 'explorer', label: 'Explorer' },
            { value: 'speedrunner', label: 'Speedrunner' },
            { value: 'completionist', label: 'Completionist' },
          ],
          default: 'aggressive',
        },
        sessionDuration: {
          type: 'number',
          label: 'Session Duration (min)',
          default: 10,
          min: 1,
          max: 60,
        },
        iterations: {
          type: 'number',
          label: 'Number of Sessions',
          default: 3,
          min: 1,
          max: 100,
        },
      },
      required: ['profile'],
    },
    defaultConfig: {
      profile: 'aggressive',
      sessionDuration: 10,
      iterations: 3,
    },
  },

  'balance-analyzer': {
    type: 'balance-analyzer',
    category: 'playtest',
    label: 'Balance Analyzer',
    description: 'Analyzes playtest telemetry to detect balance issues and suggest statistical fixes.',
    icon: '[ANALYZE]',
    color: '#F97316',
    inputs: [
      { id: 'telemetry', label: 'Telemetry', dataType: 'json', required: true, multiple: true },
      { id: 'design', label: 'Design Data', dataType: 'json', required: false, multiple: true },
    ],
    outputs: [
      { id: 'analysis', label: 'Analysis Report', dataType: 'json', required: true, multiple: false },
      { id: 'suggestions', label: 'Suggested Fixes', dataType: 'json', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        categories: {
          type: 'multiselect',
          label: 'Analysis Categories',
          options: [
            { value: 'combat', label: 'Combat Balance' },
            { value: 'economy', label: 'Economy Balance' },
            { value: 'progression', label: 'Progression Curve' },
            { value: 'difficulty', label: 'Difficulty Scaling' },
            { value: 'pacing', label: 'Pacing' },
          ],
          default: ['combat', 'economy', 'progression'],
        },
        confidenceThreshold: {
          type: 'number',
          label: 'Confidence Threshold',
          description: 'Minimum confidence for suggestions',
          default: 0.7,
          min: 0,
          max: 1,
          step: 0.1,
        },
      },
      required: [],
    },
    defaultConfig: {
      categories: ['combat', 'economy', 'progression'],
      confidenceThreshold: 0.7,
    },
  },

  'feedback-synthesizer': {
    type: 'feedback-synthesizer',
    category: 'analysis',
    label: 'Feedback Synthesizer',
    description: 'Combines analysis results into actionable iteration recommendations.',
    icon: '[SYNTH]',
    color: '#14B8A6',
    inputs: [
      { id: 'analysis', label: 'Analysis Reports', dataType: 'json', required: true, multiple: true },
    ],
    outputs: [
      { id: 'recommendations', label: 'Recommendations', dataType: 'json', required: true, multiple: false },
      { id: 'iteration-plan', label: 'Iteration Plan', dataType: 'json', required: true, multiple: false },
    ],
    configSchema: {
      properties: {
        autoIterate: {
          type: 'boolean',
          label: 'Auto-Iterate Minor Issues',
          description: 'Automatically apply low-risk fixes',
          default: false,
        },
        maxIterations: {
          type: 'number',
          label: 'Max Auto-Iterations',
          default: 3,
          min: 1,
          max: 10,
        },
        prioritize: {
          type: 'select',
          label: 'Prioritization Strategy',
          options: [
            { value: 'severity', label: 'By Severity' },
            { value: 'confidence', label: 'By Confidence' },
            { value: 'effort', label: 'By Effort (Low First)' },
          ],
          default: 'severity',
        },
      },
      required: [],
    },
    defaultConfig: {
      autoIterate: false,
      maxIterations: 3,
      prioritize: 'severity',
    },
  },
};

// Helper functions
export function getAgentsByCategory(category: AgentCategory): AgentDefinition[] {
  return Object.values(AGENT_REGISTRY).filter((agent) => agent.category === category);
}

export function getAgentDefinition(type: AgentType): AgentDefinition {
  return AGENT_REGISTRY[type];
}

export const AGENT_CATEGORIES: { id: AgentCategory; label: string; icon: string }[] = [
  { id: 'architect', label: 'Architect', icon: '[ARCH]' },
  { id: 'domain', label: 'Domain Agents', icon: '[DOMAIN]' },
  { id: 'asset', label: 'Asset Generation', icon: '[ASSET]' },
  { id: 'build', label: 'Build Pipeline', icon: '[BUILD]' },
  { id: 'playtest', label: 'Playtest', icon: '[PLAY]' },
  { id: 'analysis', label: 'Analysis', icon: '[ANALYZE]' },
];

