// src/components/GuidedMode/stepHandlers/autonomyLevelStep.ts

import type { GuidedStep, GuidedAnswers, AutonomyLevel } from '../../../types/workflow';
import { HiTrendingDown, HiSwitchHorizontal, HiTrendingUp } from 'react-icons/hi';

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

