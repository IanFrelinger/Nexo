// src/utils/behaviorRegistry.ts

import type { Behavior, Command } from '../types/agents';
import * as HiIcons from 'react-icons/hi';

// Common command definitions
const COMMANDS: Record<string, Command> = {
  // Design & Planning
  'decompose_request': { id: 'decompose_request', label: 'Decompose Request', description: 'Break down high-level requests into specifications' },
  'create_spec': { id: 'create_spec', label: 'Create Specification', description: 'Generate detailed specifications' },
  'design_system': { id: 'design_system', label: 'Design System', description: 'Design complete systems' },
  'generate_workflow': { id: 'generate_workflow', label: 'Generate Workflow', description: 'Create execution workflows' },
  
  // Generation
  'generate_behavior_tree': { id: 'generate_behavior_tree', label: 'Generate Behavior Tree', description: 'Create AI behavior trees' },
  'generate_code': { id: 'generate_code', label: 'Generate Code', description: 'Produce executable code' },
  'generate_assets': { id: 'generate_assets', label: 'Generate Assets', description: 'Create visual/audio assets' },
  'generate_level': { id: 'generate_level', label: 'Generate Level', description: 'Create level layouts' },
  'generate_narrative': { id: 'generate_narrative', label: 'Generate Narrative', description: 'Create story content' },
  
  // Analysis
  'analyze_difficulty': { id: 'analyze_difficulty', label: 'Analyze Difficulty', description: 'Evaluate game difficulty' },
  'analyze_balance': { id: 'analyze_balance', label: 'Analyze Balance', description: 'Check game balance' },
  'simulate_ai': { id: 'simulate_ai', label: 'Simulate AI', description: 'Test AI behavior' },
  'validate_design': { id: 'validate_design', label: 'Validate Design', description: 'Verify design correctness' },
  
  // Adjustment
  'adjust_parameters': { id: 'adjust_parameters', label: 'Adjust Parameters', description: 'Tune system parameters' },
  'optimize_performance': { id: 'optimize_performance', label: 'Optimize Performance', description: 'Improve system performance' },
  'refine_design': { id: 'refine_design', label: 'Refine Design', description: 'Polish existing designs' },
  
  // Execution
  'build_project': { id: 'build_project', label: 'Build Project', description: 'Compile and build project' },
  'run_tests': { id: 'run_tests', label: 'Run Tests', description: 'Execute test suites' },
  'deploy_build': { id: 'deploy_build', label: 'Deploy Build', description: 'Deploy to target platform' },
  
  // Testing
  'playtest': { id: 'playtest', label: 'Playtest', description: 'Execute playtesting sessions' },
  'collect_feedback': { id: 'collect_feedback', label: 'Collect Feedback', description: 'Gather user feedback' },
  'synthesize_insights': { id: 'synthesize_insights', label: 'Synthesize Insights', description: 'Analyze and combine feedback' },
};

// Behavior definitions by agent type
export const BEHAVIOR_REGISTRY: Record<string, Behavior[]> = {
  architect: [
    {
      id: 'orchestration',
      label: 'Orchestration',
      icon: 'HiViewGrid',
      commands: [
        COMMANDS.decompose_request,
        COMMANDS.create_spec,
        COMMANDS.generate_workflow,
      ],
    },
    {
      id: 'coordination',
      label: 'Coordination',
      icon: 'HiUsers',
      commands: [
        COMMANDS.design_system,
        COMMANDS.validate_design,
      ],
    },
  ],

  combat: [
    {
      id: 'design',
      label: 'Design',
      icon: 'HiPencil',
      commands: [
        COMMANDS.design_system,
        COMMANDS.create_spec,
        COMMANDS.refine_design,
      ],
    },
    {
      id: 'analysis',
      label: 'Analysis',
      icon: 'HiChartBar',
      commands: [
        COMMANDS.analyze_balance,
        COMMANDS.validate_design,
      ],
    },
    {
      id: 'adjustment',
      label: 'Adjustment',
      icon: 'HiAdjustments',
      commands: [
        COMMANDS.adjust_parameters,
        COMMANDS.optimize_performance,
      ],
    },
  ],

  economy: [
    {
      id: 'design',
      label: 'Design',
      icon: 'HiPencil',
      commands: [
        COMMANDS.design_system,
        COMMANDS.create_spec,
        COMMANDS.refine_design,
      ],
    },
    {
      id: 'analysis',
      label: 'Analysis',
      icon: 'HiChartBar',
      commands: [
        COMMANDS.analyze_balance,
        COMMANDS.validate_design,
      ],
    },
  ],

  'ai-behavior': [
    {
      id: 'generation',
      label: 'Generation',
      icon: 'HiSparkles',
      commands: [
        COMMANDS.generate_behavior_tree,
        COMMANDS.generate_code,
      ],
    },
    {
      id: 'simulation',
      label: 'Simulation',
      icon: 'HiPlay',
      commands: [
        COMMANDS.simulate_ai,
        COMMANDS.analyze_difficulty,
      ],
    },
    {
      id: 'adjustment',
      label: 'Adjustment',
      icon: 'HiAdjustments',
      commands: [
        COMMANDS.adjust_parameters,
        COMMANDS.refine_design,
      ],
    },
  ],

  'level-design': [
    {
      id: 'generation',
      label: 'Generation',
      icon: 'HiSparkles',
      commands: [
        COMMANDS.generate_level,
        COMMANDS.design_system,
      ],
    },
    {
      id: 'refinement',
      label: 'Refinement',
      icon: 'HiPencil',
      commands: [
        COMMANDS.refine_design,
        COMMANDS.validate_design,
      ],
    },
  ],

  narrative: [
    {
      id: 'generation',
      label: 'Generation',
      icon: 'HiSparkles',
      commands: [
        COMMANDS.generate_narrative,
        COMMANDS.create_spec,
      ],
    },
    {
      id: 'refinement',
      label: 'Refinement',
      icon: 'HiPencil',
      commands: [
        COMMANDS.refine_design,
        COMMANDS.validate_design,
      ],
    },
  ],

  'image-gen': [
    {
      id: 'generation',
      label: 'Generation',
      icon: 'HiSparkles',
      commands: [
        COMMANDS.generate_assets,
      ],
    },
  ],

  'audio-gen': [
    {
      id: 'generation',
      label: 'Generation',
      icon: 'HiSparkles',
      commands: [
        COMMANDS.generate_assets,
      ],
    },
  ],

  'model-3d-gen': [
    {
      id: 'generation',
      label: 'Generation',
      icon: 'HiSparkles',
      commands: [
        COMMANDS.generate_assets,
      ],
    },
  ],

  'unity-build': [
    {
      id: 'build',
      label: 'Build',
      icon: 'HiCog',
      commands: [
        COMMANDS.build_project,
        COMMANDS.run_tests,
        COMMANDS.deploy_build,
      ],
    },
  ],

  'ai-player': [
    {
      id: 'testing',
      label: 'Testing',
      icon: 'HiPlay',
      commands: [
        COMMANDS.playtest,
        COMMANDS.simulate_ai,
      ],
    },
  ],

  'balance-analyzer': [
    {
      id: 'analysis',
      label: 'Analysis',
      icon: 'HiChartBar',
      commands: [
        COMMANDS.analyze_balance,
        COMMANDS.analyze_difficulty,
        COMMANDS.validate_design,
      ],
    },
  ],

  'feedback-synthesizer': [
    {
      id: 'collection',
      label: 'Collection',
      icon: 'HiCollection',
      commands: [
        COMMANDS.collect_feedback,
      ],
    },
    {
      id: 'synthesis',
      label: 'Synthesis',
      icon: 'HiLightBulb',
      commands: [
        COMMANDS.synthesize_insights,
        COMMANDS.analyze_balance,
      ],
    },
  ],
};

// Get behaviors for an agent type
export function getBehaviorsForAgent(agentType: string): Behavior[] {
  return BEHAVIOR_REGISTRY[agentType] || [];
}

