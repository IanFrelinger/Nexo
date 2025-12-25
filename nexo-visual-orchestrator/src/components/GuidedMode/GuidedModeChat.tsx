// src/components/GuidedMode/GuidedModeChat.tsx

import { useState, useRef, useEffect } from 'react';
import type { 
  RoleDefinition, 
  Relationship, 
  GuidedStep, 
  GuidedAnswers, 
  AutonomyLevel, 
  ConflictResolution,
  OrgPattern,
  ScalingPreference,
} from '../../types/workflow';
import { PROJECT_TEMPLATES, ORG_PATTERNS, generateWorkflow } from '../../data/roleTemplates';
import { 
  HiChat, HiChevronRight, HiX,
  HiOfficeBuilding, HiUserGroup, HiGlobeAlt, HiSparkles,
  HiShieldCheck, HiScale, HiLightningBolt,
  HiTrendingDown, HiSwitchHorizontal, HiTrendingUp,
} from 'react-icons/hi';

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

const ORG_ICONS: Record<OrgPattern, React.ReactNode> = {
  hierarchical: <HiOfficeBuilding className="w-5 h-5 text-purple-400" />,
  surgical: <HiUserGroup className="w-5 h-5 text-blue-400" />,
  federated: <HiGlobeAlt className="w-5 h-5 text-green-400" />,
  swarm: <HiSparkles className="w-5 h-5 text-yellow-400" />,
};

export default function GuidedModeChat({ onComplete, onSkip }: GuidedModeChatProps) {
  const [step, setStep] = useState<GuidedStep>('welcome');
  const [answers, setAnswers] = useState<GuidedAnswers>({
    projectType: null,
    orgPattern: null,
    autonomyLevel: null,
    scalingPreference: null,
    conflictResolution: null,
  });
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Initial messages
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

  const addMessages = async (newMessages: ChatMessage[]) => {
    setIsTyping(true);
    for (const msg of newMessages) {
      await new Promise(resolve => setTimeout(resolve, 250 + Math.random() * 300));
      setMessages(prev => [...prev, msg]);
    }
    setIsTyping(false);
  };

  const handleOptionSelect = async (optionId: string) => {
    // Echo user selection
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

    // Handle based on step
    switch (step) {
      case 'welcome': {
        const template = PROJECT_TEMPLATES[optionId];
        setAnswers(prev => ({ ...prev, projectType: optionId }));
        setStep('team-structure');
        
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
        break;
      }

      case 'team-structure': {
        const org = ORG_PATTERNS[optionId as OrgPattern];
        setAnswers(prev => ({ ...prev, orgPattern: optionId as OrgPattern }));
        setStep('autonomy-level');

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
        break;
      }

      case 'autonomy-level': {
        setAnswers(prev => ({ ...prev, autonomyLevel: optionId as AutonomyLevel }));
        setStep('scaling-preference');

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
        break;
      }

      case 'scaling-preference': {
        setAnswers(prev => ({ ...prev, scalingPreference: optionId as ScalingPreference }));
        setStep('conflict-resolution');

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
        break;
      }

      case 'conflict-resolution': {
        const finalAnswers = { ...answers, conflictResolution: optionId as ConflictResolution };
        setAnswers(finalAnswers);
        setStep('review');

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
        break;
      }

      case 'review': {
        if (optionId === 'create') {
          const workflow = generateWorkflow(
            answers.projectType!,
            answers.orgPattern!,
            answers.autonomyLevel!,
            answers.scalingPreference!
          );
          onComplete(workflow);
        } else {
          // Restart
          setStep('welcome');
          setAnswers({
            projectType: null,
            orgPattern: null,
            autonomyLevel: null,
            scalingPreference: null,
            conflictResolution: null,
          });
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
        }
        break;
      }
    }
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-slate-700">
        <div className="flex items-center gap-2">
          <HiChat className="w-5 h-5 text-purple-400" />
          <h2 className="font-semibold text-white">Team Setup</h2>
        </div>
        <button
          onClick={onSkip}
          className="p-1 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
        >
          <HiX className="w-5 h-5" />
        </button>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.map(msg => (
          <div key={msg.id}>
            {msg.type === 'assistant' && (
              <div className="flex gap-3">
                <div className="w-8 h-8 rounded-full bg-purple-500/20 flex items-center justify-center flex-shrink-0">
                  <HiChat className="w-4 h-4 text-purple-400" />
                </div>
                <div className="bg-slate-800 rounded-lg px-4 py-2 max-w-[85%]">
                  <p 
                    className="text-sm text-slate-200"
                    dangerouslySetInnerHTML={{
                      __html: msg.content
                        .replace(/\*\*(.*?)\*\*/g, '<strong class="text-white">$1</strong>')
                        .replace(/\n/g, '<br />')
                    }}
                  />
                </div>
              </div>
            )}

            {msg.type === 'user' && (
              <div className="flex justify-end">
                <div className="bg-purple-600 rounded-lg px-4 py-2 max-w-[80%]">
                  <p className="text-sm text-white">{msg.content}</p>
                </div>
              </div>
            )}

            {msg.type === 'options' && (
              <div className="space-y-2 mt-2">
                {msg.options?.map(option => (
                  <button
                    key={option.id}
                    onClick={() => handleOptionSelect(option.id)}
                    className="w-full flex items-center gap-3 p-3 bg-slate-800 hover:bg-slate-700 border border-slate-700 hover:border-purple-500/50 rounded-lg transition-all text-left"
                  >
                    {option.icon && <div className="flex-shrink-0">{option.icon}</div>}
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-white">{option.label}</p>
                      {option.description && (
                        <p className="text-xs text-slate-400 mt-0.5">{option.description}</p>
                      )}
                    </div>
                    <HiChevronRight className="w-5 h-5 text-slate-500 flex-shrink-0" />
                  </button>
                ))}
              </div>
            )}
          </div>
        ))}

        {isTyping && (
          <div className="flex gap-3">
            <div className="w-8 h-8 rounded-full bg-purple-500/20 flex items-center justify-center">
              <HiChat className="w-4 h-4 text-purple-400" />
            </div>
            <div className="bg-slate-800 rounded-lg px-4 py-2">
              <div className="flex gap-1">
                <div className="w-2 h-2 bg-slate-500 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                <div className="w-2 h-2 bg-slate-500 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                <div className="w-2 h-2 bg-slate-500 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
              </div>
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Footer */}
      <div className="px-4 py-3 border-t border-slate-700">
        <p className="text-xs text-slate-500 text-center">
          Roles spawn instances dynamically based on workload • Press Esc to skip
        </p>
      </div>
    </div>
  );
}
