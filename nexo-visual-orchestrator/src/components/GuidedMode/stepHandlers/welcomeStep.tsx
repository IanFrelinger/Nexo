// src/components/GuidedMode/stepHandlers/welcomeStep.tsx

/**
 * Welcome Step Handler
 * 
 * Handles the initial step of the guided workflow setup. Processes the user's
 * project type selection and transitions to the organization pattern selection.
 * 
 * @param optionId - Selected project template ID
 * @param addMessages - Function to add messages to the chat
 * @returns Next step and collected answers
 */

import React from 'react';
import type { GuidedStep, GuidedAnswers } from '../../../types/workflow';
import { PROJECT_TEMPLATES, ORG_PATTERNS } from '../../../data/roleTemplates';
import { 
  HiOfficeBuilding, HiUserGroup, HiGlobeAlt, HiSparkles,
} from 'react-icons/hi';
import type { OrgPattern } from '../../../types/workflow';

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

/**
 * Handles the welcome step of guided mode
 * @param optionId - Selected project template ID
 * @param addMessages - Function to add messages to chat
 * @returns Next step and answers
 */
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

