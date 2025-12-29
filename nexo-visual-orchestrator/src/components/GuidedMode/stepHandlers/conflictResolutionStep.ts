// src/components/GuidedMode/stepHandlers/conflictResolutionStep.ts

import type { GuidedStep, GuidedAnswers, ConflictResolution } from '../../../types/workflow';
import { PROJECT_TEMPLATES, ORG_PATTERNS } from '../../../data/roleTemplates';

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

