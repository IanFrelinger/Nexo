// src/components/GuidedMode/stepHandlers/reviewStep.ts

import { PROJECT_TEMPLATES } from '../../../data/roleTemplates';

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

