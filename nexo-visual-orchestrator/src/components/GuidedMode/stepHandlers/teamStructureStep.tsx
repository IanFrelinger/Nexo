// src/components/GuidedMode/stepHandlers/teamStructureStep.tsx

import React from 'react';
import type { GuidedStep, GuidedAnswers, OrgPattern } from '../../../types/workflow';
import { ORG_PATTERNS } from '../../../data/roleTemplates';
import { HiShieldCheck, HiScale, HiLightningBolt } from 'react-icons/hi';

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

