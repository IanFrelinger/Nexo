// src/components/GuidedMode/stepHandlers.ts

import type { 
  GuidedStep, 
  GuidedAnswers, 
  AutonomyLevel, 
  ConflictResolution,
  OrgPattern,
  ScalingPreference,
} from '../../types/workflow';
import { PROJECT_TEMPLATES, ORG_PATTERNS } from '../../data/roleTemplates';
import { 
  HiShieldCheck, HiScale, HiLightningBolt,
  HiTrendingDown, HiSwitchHorizontal, HiTrendingUp,
  HiOfficeBuilding, HiUserGroup, HiGlobeAlt, HiSparkles,
} from 'react-icons/hi';

interface ChatMessage {
  id: string;
  type: 'assistant' | 'user' | 'options';
  content: string;
  options?: {
    id: string;
    label: string;
    description?: string;
    icon?: React.ReactNode;
  }[];
}

const ORG_ICONS: Record<OrgPattern, React.ReactNode> = {
  hierarchical: <HiOfficeBuilding className="w-5 h-5 text-purple-400" />,
  surgical: <HiUserGroup className="w-5 h-5 text-blue-400" />,
  federated: <HiGlobeAlt className="w-5 h-5 text-green-400" />,
  swarm: <HiSparkles className="w-5 h-5 text-yellow-400" />,
};

export async function handleWelcomeStep(
  optionId: string,
  addMessages: (messages: ChatMessage[]) => Promise<void>
): Promise<{ nextStep: GuidedStep; answers: Partial<GuidedAnswers> }> {
  const template = PROJECT_TEMPLATES[optionId];
  
  await addMessages([
    {
      id: 'project-confirm',
      type: 'assistant',
      content: `Great! **${template.name}** uses ${template.roles.length} specialized roles.`,
    },
    {
      id: 'org-intro',
      type: 'assistant',
      content: "How should your agents be organized? This affects communication patterns and scaling behavior.",
    },
    {
      id: 'org-options',
      type: 'options',
      content: 'Choose an organization pattern:',
      options: Object.values(ORG_PATTERNS).map(org => ({
        id: org.id,
        label: org.name,
        description: org.description,
        icon: ORG_ICONS[org.id],
      })),
    },
  ]);

  return {
    nextStep: 'team-structure',
    answers: { projectType: optionId },
  };
}

export async function handleTeamStructureStep(
  optionId: string,
  addMessages: (messages: ChatMessage[]) => Promise<void>
): Promise<{ nextStep: GuidedStep; answers: Partial<GuidedAnswers> }> {
  const org = ORG_PATTERNS[optionId as OrgPattern];

  await addMessages([
    {
      id: 'org-confirm',
      type: 'assistant',
      content: `**${org.name}** pattern selected. ${org.traits[0]}.`,
    },
    {
      id: 'autonomy-intro',
      type: 'assistant',
      content: "How much should agents decide on their own?",
    },
    {
      id: 'autonomy-options',
      type: 'options',
      content: 'Choose autonomy level:',
      options: [
        {
          id: 'conservative',
          label: 'Conservative',
          description: 'Agents suggest, you approve everything',
          icon: <HiShieldCheck className="w-5 h-5 text-blue-400" />,
        },
        {
          id: 'balanced',
          label: 'Balanced',
          description: 'Auto-apply within bounds, escalate big decisions',
          icon: <HiScale className="w-5 h-5 text-green-400" />,
        },
        {
          id: 'aggressive',
          label: 'Aggressive',
          description: 'Iterate freely, notify you of changes',
          icon: <HiLightningBolt className="w-5 h-5 text-orange-400" />,
        },
      ],
    },
  ]);

  return {
    nextStep: 'autonomy-level',
    answers: { orgPattern: optionId as OrgPattern },
  };
}

export async function handleAutonomyLevelStep(
  optionId: string,
  addMessages: (messages: ChatMessage[]) => Promise<void>
): Promise<{ nextStep: GuidedStep; answers: Partial<GuidedAnswers> }> {
  await addMessages([
    {
      id: 'autonomy-confirm',
      type: 'assistant',
      content: `Got it—**${optionId}** autonomy.`,
    },
    {
      id: 'scaling-intro',
      type: 'assistant',
      content: "How aggressively should roles spawn new instances when workload increases?",
    },
    {
      id: 'scaling-options',
      type: 'options',
      content: 'Choose scaling preference:',
      options: [
        {
          id: 'minimal',
          label: 'Minimal',
          description: 'Keep instances low, accept some latency',
          icon: <HiTrendingDown className="w-5 h-5 text-blue-400" />,
        },
        {
          id: 'balanced',
          label: 'Balanced',
          description: 'Scale moderately based on queue depth',
          icon: <HiSwitchHorizontal className="w-5 h-5 text-green-400" />,
        },
        {
          id: 'aggressive',
          label: 'Aggressive',
          description: 'Scale fast, optimize for speed over cost',
          icon: <HiTrendingUp className="w-5 h-5 text-orange-400" />,
        },
      ],
    },
  ]);

  return {
    nextStep: 'scaling-preference',
    answers: { autonomyLevel: optionId as AutonomyLevel },
  };
}

export async function handleScalingPreferenceStep(
  optionId: string,
  addMessages: (messages: ChatMessage[]) => Promise<void>
): Promise<{ nextStep: GuidedStep; answers: Partial<GuidedAnswers> }> {
  await addMessages([
    {
      id: 'scaling-confirm',
      type: 'assistant',
      content: `**${optionId}** scaling it is.`,
    },
    {
      id: 'conflict-intro',
      type: 'assistant',
      content: "When agents disagree (like Combat wanting high loot but Economy wanting scarcity), how should they handle it?",
    },
    {
      id: 'conflict-options',
      type: 'options',
      content: 'Choose conflict resolution:',
      options: [
        { id: 'always-escalate', label: 'Always Escalate', description: 'Bring all conflicts to you' },
        { id: 'negotiate-first', label: 'Negotiate First', description: 'Try to resolve, escalate if stuck' },
        { id: 'decide-notify', label: 'Decide & Notify', description: 'Resolve autonomously, inform you after' },
      ],
    },
  ]);

  return {
    nextStep: 'conflict-resolution',
    answers: { scalingPreference: optionId as ScalingPreference },
  };
}

export async function handleConflictResolutionStep(
  optionId: string,
  answers: GuidedAnswers,
  addMessages: (messages: ChatMessage[]) => Promise<void>
): Promise<{ nextStep: GuidedStep; answers: Partial<GuidedAnswers> }> {
  const finalAnswers = { ...answers, conflictResolution: optionId as ConflictResolution };
  const template = PROJECT_TEMPLATES[finalAnswers.projectType!];
  const org = ORG_PATTERNS[finalAnswers.orgPattern!];

  await addMessages([
    {
      id: 'summary',
      type: 'assistant',
      content: `Here's your team:\n\n• **${template.roles.length} roles** (${org.name} structure)\n• **${finalAnswers.autonomyLevel}** autonomy\n• **${finalAnswers.scalingPreference}** scaling\n• **${optionId.replace('-', ' ')}** conflict handling`,
    },
    {
      id: 'review-options',
      type: 'options',
      content: 'Ready?',
      options: [
        { id: 'create', label: 'Create Team', description: 'Generate roles and start orchestrating' },
        { id: 'restart', label: 'Start Over', description: 'Change my answers' },
      ],
    },
  ]);

  return {
    nextStep: 'review',
    answers: { conflictResolution: optionId as ConflictResolution },
  };
}

export async function handleReviewStep(
  optionId: string,
  addMessages: (messages: ChatMessage[]) => Promise<void>,
  setMessages: React.Dispatch<React.SetStateAction<ChatMessage[]>>
): Promise<{ shouldRestart: boolean }> {
  if (optionId === 'restart') {
    setMessages([]);
    await addMessages([
      { id: 'restart', type: 'assistant', content: "Let's start fresh." },
      {
        id: 'welcome-options-2',
        type: 'options',
        content: 'What are you building?',
        options: Object.values(PROJECT_TEMPLATES).map(t => ({
          id: t.id,
          label: t.name,
          description: t.description,
        })),
      },
    ]);
    return { shouldRestart: true };
  }
  return { shouldRestart: false };
}

