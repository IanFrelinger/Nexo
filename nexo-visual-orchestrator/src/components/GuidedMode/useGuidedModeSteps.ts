// src/components/GuidedMode/useGuidedModeSteps.ts

/**
 * useGuidedModeSteps Hook
 * 
 * Custom hook managing the state and flow of the guided workflow setup wizard.
 * Handles step progression, answer collection, and delegates to individual
 * step handlers for each phase of the wizard.
 * 
 * Steps:
 * - welcome: Project type selection
 * - team-structure: Organization pattern selection
 * - autonomy-level: Autonomy level configuration
 * - scaling-preference: Scaling preferences
 * - conflict-resolution: Conflict resolution strategy
 * - review: Review and create workflow
 */

import { useState } from 'react';
import type { 
  GuidedStep, 
  GuidedAnswers, 
} from '../../types/workflow';
import {
  handleWelcomeStep,
  handleTeamStructureStep,
  handleAutonomyLevelStep,
  handleScalingPreferenceStep,
  handleConflictResolutionStep,
  handleReviewStep,
} from './stepHandlers';

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

/**
 * Hook for managing guided mode step progression
 * @param addMessages - Function to add new messages to the chat
 * @param setMessages - Function to set the messages array
 * @param messages - Current messages array
 * @returns Step state, answers, and option selection handler
 */
export function useGuidedModeSteps(
  addMessages: (messages: ChatMessage[]) => Promise<void>,
  setMessages: React.Dispatch<React.SetStateAction<ChatMessage[]>>,
  messages: ChatMessage[]
) {
  const [step, setStep] = useState<GuidedStep>('welcome');
  const [answers, setAnswers] = useState<GuidedAnswers>({
    projectType: null,
    orgPattern: null,
    autonomyLevel: null,
    scalingPreference: null,
    conflictResolution: null,
  });

  const handleOptionSelect = async (optionId: string): Promise<boolean> => {
    const selectedOption = messages
      .filter(m => m.type === 'options')
      .flatMap(m => m.options || [])
      .find(o => o.id === optionId);

    if (selectedOption) {
      setMessages(prev => [...prev, {
        id: `user-${Date.now()}`,
        type: 'user',
        content: selectedOption.label,
      }]);
    }

    switch (step) {
      case 'welcome': {
        const result = await handleWelcomeStep(optionId, addMessages);
        setAnswers(prev => ({ ...prev, ...result.answers }));
        setStep(result.nextStep);
        break;
      }

      case 'team-structure': {
        const result = await handleTeamStructureStep(optionId, addMessages);
        setAnswers(prev => ({ ...prev, ...result.answers }));
        setStep(result.nextStep);
        break;
      }

      case 'autonomy-level': {
        const result = await handleAutonomyLevelStep(optionId, addMessages);
        setAnswers(prev => ({ ...prev, ...result.answers }));
        setStep(result.nextStep);
        break;
      }

      case 'scaling-preference': {
        const result = await handleScalingPreferenceStep(optionId, addMessages);
        setAnswers(prev => ({ ...prev, ...result.answers }));
        setStep(result.nextStep);
        break;
      }

      case 'conflict-resolution': {
        const result = await handleConflictResolutionStep(optionId, answers, addMessages);
        setAnswers(prev => ({ ...prev, ...result.answers }));
        setStep(result.nextStep);
        break;
      }

      case 'review': {
        const result = await handleReviewStep(optionId, addMessages, setMessages);
        if (result.shouldRestart) {
          setStep('welcome');
          setAnswers({
            projectType: null,
            orgPattern: null,
            autonomyLevel: null,
            scalingPreference: null,
            conflictResolution: null,
          });
        }
        return optionId === 'create';
      }
    }

    return false;
  };

  return { step, answers, handleOptionSelect };
}
