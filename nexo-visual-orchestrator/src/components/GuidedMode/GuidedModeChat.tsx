// src/components/GuidedMode/GuidedModeChat.tsx

/**
 * GuidedModeChat Component
 * 
 * Main chat interface for the guided workflow setup wizard. Guides users through
 * a conversational flow to define their agent team structure by asking questions
 * about project type, organization pattern, autonomy level, and scaling preferences.
 * 
 * Features:
 * - Typing indicators for realistic chat experience
 * - Step-by-step question flow
 * - Interactive option selection
 * - Automatic workflow generation upon completion
 */

import { useState, useRef, useEffect } from 'react';
import type { 
  RoleDefinition, 
  Relationship, 
} from '../../types/workflow';
import { PROJECT_TEMPLATES, generateWorkflow } from '../../data/roleTemplates';
import GuidedModeChatHeader from './GuidedModeChatHeader';
import GuidedModeChatMessages from './GuidedModeChatMessages';
import GuidedModeChatOptions from './GuidedModeChatOptions';
import { useGuidedModeSteps } from './useGuidedModeSteps';

interface GuidedModeChatProps {
  onComplete: (workflow: { roles: RoleDefinition[]; relationships: Relationship[] }) => void;
  onSkip: () => void;
}

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

export default function GuidedModeChat({ onComplete, onSkip }: GuidedModeChatProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const addMessages = async (newMessages: ChatMessage[]) => {
    setIsTyping(true);
    for (const msg of newMessages) {
      await new Promise(resolve => setTimeout(resolve, 250 + Math.random() * 300));
      setMessages(prev => [...prev, msg]);
    }
    setIsTyping(false);
  };

  const { answers, handleOptionSelect } = useGuidedModeSteps(
    addMessages,
    setMessages,
    messages
  );

  useEffect(() => {
    addMessages([
      {
        id: 'welcome-1',
        type: 'assistant',
        content: "Welcome to Nexo! Let's build your agent team.",
      },
      {
        id: 'welcome-2',
        type: 'assistant',
        content: "We'll define **roles** (templates for what agents can do) that spawn **instances** dynamically based on workload.",
      },
      {
        id: 'welcome-options',
        type: 'options',
        content: 'What are you building?',
        options: Object.values(PROJECT_TEMPLATES).map(t => ({
          id: t.id,
          label: t.name,
          description: t.description,
        })),
      },
    ]);
  }, []);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleOptionClick = async (optionId: string) => {
    const shouldCreate = await handleOptionSelect(optionId);
    
    if (shouldCreate) {
          const workflow = generateWorkflow(
            answers.projectType!,
            answers.orgPattern!,
            answers.autonomyLevel!,
            answers.scalingPreference!
          );
          onComplete(workflow);
    }
  };

  return (
    <div className="flex flex-col h-full">
      <GuidedModeChatHeader onSkip={onSkip} />

      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        <GuidedModeChatMessages messages={messages} isTyping={isTyping} />
        
        {messages.map(msg => (
          msg.type === 'options' && (
            <GuidedModeChatOptions
              key={msg.id}
              options={msg.options || []}
              onSelect={handleOptionClick}
            />
          )
        ))}

        <div ref={messagesEndRef} />
      </div>

      <div className="px-4 py-3 border-t border-slate-700">
        <p className="text-xs text-slate-500 text-center">
          Roles spawn instances dynamically based on workload • Press Esc to skip
        </p>
      </div>
    </div>
  );
}
