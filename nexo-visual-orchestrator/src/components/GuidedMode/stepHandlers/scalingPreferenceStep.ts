// src/components/GuidedMode/stepHandlers/scalingPreferenceStep.ts

import type { GuidedStep, GuidedAnswers, ScalingPreference } from '../../../types/workflow';

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

