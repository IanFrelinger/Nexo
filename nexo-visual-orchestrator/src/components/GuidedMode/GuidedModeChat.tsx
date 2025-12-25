import { useState, useRef, useEffect } from 'react';
import { HiPaperAirplane, HiSparkles } from 'react-icons/hi';
import type { AgentType } from '../../types/agents';

interface Message {
  id: string;
  role: 'assistant' | 'user';
  content: string;
  timestamp: number;
}

interface GuidedModeChatProps {
  onComplete: (workflow: { nodes: any[]; edges: any[] }) => void;
  onSkip: () => void;
}

const INITIAL_QUESTIONS = [
  {
    id: 'game-type',
    text: "What type of game are you building? (e.g., extraction shooter, roguelike, RPG, platformer, city builder)",
    key: 'gameType',
  },
  {
    id: 'scope',
    text: "What's the scope of your project? (small, medium, or large)",
    key: 'scope',
  },
  {
    id: 'focus',
    text: "What's your main focus? (combat, economy, narrative, level design, or all of the above)",
    key: 'focus',
  },
  {
    id: 'assets',
    text: "Do you need asset generation? (images, audio, 3D models, or none)",
    key: 'assets',
  },
];

export default function GuidedModeChat({ onComplete, onSkip }: GuidedModeChatProps) {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      role: 'assistant',
      content: "👋 Welcome! I'm here to help you set up your agent orchestration workflow. Let me ask you a few questions to understand what you're building.",
      timestamp: Date.now(),
    },
  ]);
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [userResponses, setUserResponses] = useState<Record<string, string>>({});
  const [inputValue, setInputValue] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Ask first question on mount
  useEffect(() => {
    if (messages.length === 1) {
      const question = INITIAL_QUESTIONS[0];
      const newMessage: Message = {
        id: `msg-${Date.now()}`,
        role: 'assistant',
        content: question.text,
        timestamp: Date.now(),
      };
      setMessages((prev) => [...prev, newMessage]);
      inputRef.current?.focus();
    }
  }, []);

  const askNextQuestion = () => {
    if (currentQuestionIndex >= INITIAL_QUESTIONS.length) {
      return;
    }

    const question = INITIAL_QUESTIONS[currentQuestionIndex];
    const newMessage: Message = {
      id: `msg-${Date.now()}`,
      role: 'assistant',
      content: question.text,
      timestamp: Date.now(),
    };

    setMessages((prev) => [...prev, newMessage]);
    inputRef.current?.focus();
  };

  const handleSend = () => {
    if (!inputValue.trim() || isGenerating) return;

    const userMessage: Message = {
      id: `msg-${Date.now()}`,
      role: 'user',
      content: inputValue.trim(),
      timestamp: Date.now(),
    };

    const question = INITIAL_QUESTIONS[currentQuestionIndex];
    setUserResponses((prev) => ({
      ...prev,
      [question.key]: inputValue.trim(),
    }));

    setMessages((prev) => [...prev, userMessage]);
    setInputValue('');
    const newIndex = currentQuestionIndex + 1;
    setCurrentQuestionIndex(newIndex);

    // Ask next question after a short delay
    if (newIndex < INITIAL_QUESTIONS.length) {
      setTimeout(() => {
        askNextQuestion();
      }, 500);
    } else {
      // All questions answered, generate workflow
      setTimeout(() => {
        generateWorkflow();
      }, 500);
    }
  };

  const generateWorkflow = async () => {
    setIsGenerating(true);

    const generatingMessage: Message = {
      id: `msg-${Date.now()}`,
      role: 'assistant',
      content: 'Great! Let me generate your initial workflow based on your responses...',
      timestamp: Date.now(),
    };

    setMessages((prev) => [...prev, generatingMessage]);

    // Simulate processing time
    await new Promise((resolve) => setTimeout(resolve, 1500));

    const workflow = createWorkflowFromResponses(userResponses);

    const completeMessage: Message = {
      id: `msg-${Date.now()}`,
      role: 'assistant',
      content: "Perfect! I've created an initial workflow for you. Click 'Start Building' to see it on the canvas, or 'Skip' to start from scratch.",
      timestamp: Date.now(),
    };

    setMessages((prev) => [...prev, completeMessage]);
    setIsGenerating(false);

    // Store workflow for when user clicks "Start Building"
    (window as any).__pendingWorkflow = workflow;
  };

  const handleStartBuilding = () => {
    const workflow = (window as any).__pendingWorkflow;
    if (workflow) {
      onComplete(workflow);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="flex flex-col h-full bg-surface-dark">
      {/* Header */}
      <div className="p-4 border-b border-slate-700 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <HiSparkles className="w-5 h-5 text-blue-400" />
          <h2 className="font-semibold text-lg">Guided Setup</h2>
        </div>
        <button
          onClick={onSkip}
          className="px-3 py-1.5 text-sm text-slate-400 hover:text-slate-200 hover:bg-surface-light rounded transition-colors"
        >
          Skip
        </button>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.map((message) => (
          <div
            key={message.id}
            className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}
          >
            <div
              className={`max-w-[80%] rounded-lg px-4 py-2 ${
                message.role === 'user'
                  ? 'bg-blue-600 text-white'
                  : 'bg-surface border border-slate-700 text-slate-200'
              }`}
            >
              <p className="text-sm whitespace-pre-wrap">{message.content}</p>
            </div>
          </div>
        ))}
        {isGenerating && (
          <div className="flex justify-start">
            <div className="bg-surface border border-slate-700 rounded-lg px-4 py-2">
              <div className="flex items-center gap-2">
                <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" />
                <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '0.2s' }} />
                <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '0.4s' }} />
              </div>
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Input Area */}
      <div className="p-4 border-t border-slate-700">
        {currentQuestionIndex < INITIAL_QUESTIONS.length ? (
          <div className="flex gap-2">
            <input
              ref={inputRef}
              type="text"
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              onKeyPress={handleKeyPress}
              placeholder="Type your answer..."
              className="flex-1 px-4 py-2 bg-surface border border-slate-600 rounded-lg text-slate-200 placeholder:text-slate-500 focus:outline-none focus:border-blue-500"
              disabled={isGenerating}
            />
            <button
              onClick={handleSend}
              disabled={!inputValue.trim() || isGenerating}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
            >
              <HiPaperAirplane className="w-4 h-4" />
              <span>Send</span>
            </button>
          </div>
        ) : (
          <div className="flex gap-2">
            <button
              onClick={handleStartBuilding}
              className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors font-medium"
            >
              Start Building
            </button>
            <button
              onClick={onSkip}
              className="px-4 py-2 bg-surface border border-slate-600 text-slate-300 rounded-lg hover:bg-surface-light transition-colors"
            >
              Skip
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

function createWorkflowFromResponses(responses: Record<string, string>): { nodes: any[]; edges: any[] } {
  const nodes: any[] = [];
  const edges: any[] = [];
  const nodePositions: Record<string, { x: number; y: number }> = {};
  let xOffset = 0;
  let yOffset = 0;

  // Always start with Architect
  const architectId = 'architect_1';
  nodePositions[architectId] = { x: 100, y: 200 };
  nodes.push({
    id: architectId,
    type: 'agentNode',
    position: nodePositions[architectId],
    data: {
      agentType: 'architect',
      label: 'Architect',
      config: {
        request: responses.gameType || 'game',
        gameType: responses.gameType?.toLowerCase().replace(/\s+/g, '-') || 'custom',
        maxAgents: 10,
      },
      status: 'idle',
    },
  });

  xOffset = 400;
  yOffset = 100;

  // Add domain agents based on focus
  const focus = responses.focus?.toLowerCase() || '';
  const domainAgents: AgentType[] = [];

  if (focus.includes('combat') || focus.includes('all')) {
    domainAgents.push('combat');
  }
  if (focus.includes('economy') || focus.includes('all')) {
    domainAgents.push('economy');
  }
  if (focus.includes('narrative') || focus.includes('all')) {
    domainAgents.push('narrative');
  }
  if (focus.includes('level') || focus.includes('all')) {
    domainAgents.push('level-design');
  }
  if (domainAgents.length === 0) {
    // Default to combat if nothing specified
    domainAgents.push('combat');
  }

  domainAgents.forEach((agentType, index) => {
    const nodeId = `${agentType}_${index + 1}`;
    nodePositions[nodeId] = { x: xOffset, y: yOffset + index * 150 };
    nodes.push({
      id: nodeId,
      type: 'agentNode',
      position: nodePositions[nodeId],
      data: {
        agentType,
        label: getAgentLabel(agentType),
        config: {},
        status: 'idle',
      },
    });

    // Connect to architect
    edges.push({
      id: `edge_${architectId}_${nodeId}`,
      source: architectId,
      target: nodeId,
      sourceHandle: 'specs',
      targetHandle: 'spec',
      type: 'dataFlowEdge',
      data: {
        sourcePort: 'specs',
        targetPort: 'spec',
      },
    });
  });

  xOffset = 700;
  yOffset = 200;

  // Add asset generation agents
  const assets = responses.assets?.toLowerCase() || '';
  const assetAgents: AgentType[] = [];

  if (assets.includes('image') || assets.includes('all')) {
    assetAgents.push('image-gen');
  }
  if (assets.includes('audio') || assets.includes('all')) {
    assetAgents.push('audio-gen');
  }
  if (assets.includes('3d') || assets.includes('model') || assets.includes('all')) {
    assetAgents.push('model-3d-gen');
  }

  assetAgents.forEach((agentType, index) => {
    const nodeId = `${agentType}_${index + 1}`;
    nodePositions[nodeId] = { x: xOffset, y: yOffset + index * 150 };
    nodes.push({
      id: nodeId,
      type: 'agentNode',
      position: nodePositions[nodeId],
      data: {
        agentType,
        label: getAgentLabel(agentType),
        config: {},
        status: 'idle',
      },
    });

    // Connect to first domain agent if available (asset agents use 'prompt' input)
    if (domainAgents.length > 0) {
      const sourceNodeId = `${domainAgents[0]}_1`;
      edges.push({
        id: `edge_${sourceNodeId}_${nodeId}`,
        source: sourceNodeId,
        target: nodeId,
        sourceHandle: 'design',
        targetHandle: 'prompt',
        type: 'dataFlowEdge',
        data: {
          sourcePort: 'design',
          targetPort: 'prompt',
        },
      });
    }
  });

  // Add build and playtest agents
  xOffset = 1000;
  yOffset = 300;

  const buildId = 'unity-build_1';
  nodePositions[buildId] = { x: xOffset, y: yOffset };
  nodes.push({
    id: buildId,
    type: 'agentNode',
    position: nodePositions[buildId],
    data: {
      agentType: 'unity-build',
      label: 'Unity Build',
      config: {},
      status: 'idle',
    },
  });

  // Connect build to domain agents
  domainAgents.forEach((agentType, index) => {
    const sourceNodeId = `${agentType}_${index + 1}`;
    edges.push({
      id: `edge_${sourceNodeId}_${buildId}`,
      source: sourceNodeId,
      target: buildId,
      sourceHandle: 'design',
      targetHandle: 'design',
      type: 'dataFlowEdge',
      data: {
        sourcePort: 'design',
        targetPort: 'design',
      },
    });
  });

  const playtestId = 'ai-player_1';
  nodePositions[playtestId] = { x: xOffset, y: yOffset + 150 };
  nodes.push({
    id: playtestId,
    type: 'agentNode',
    position: nodePositions[playtestId],
    data: {
      agentType: 'ai-player',
      label: 'AI Player',
      config: {},
      status: 'idle',
    },
  });

  edges.push({
    id: `edge_${buildId}_${playtestId}`,
    source: buildId,
    target: playtestId,
    sourceHandle: 'build',
    targetHandle: 'build',
    type: 'dataFlowEdge',
    data: {
      sourcePort: 'build',
      targetPort: 'build',
    },
  });

  // Add feedback synthesizer
  const synthId = 'feedback-synthesizer_1';
  nodePositions[synthId] = { x: xOffset, y: yOffset + 300 };
  nodes.push({
    id: synthId,
    type: 'agentNode',
    position: nodePositions[synthId],
    data: {
      agentType: 'feedback-synthesizer',
      label: 'Feedback Synthesizer',
      config: {},
      status: 'idle',
    },
  });

  edges.push({
    id: `edge_${playtestId}_${synthId}`,
    source: playtestId,
    target: synthId,
    sourceHandle: 'telemetry',
    targetHandle: 'analysis',
    type: 'dataFlowEdge',
    data: {
      sourcePort: 'telemetry',
      targetPort: 'analysis',
    },
  });

  return { nodes, edges };
}

function getAgentLabel(agentType: AgentType): string {
  const labels: Record<AgentType, string> = {
    architect: 'Architect',
    combat: 'Combat',
    economy: 'Economy',
    'ai-behavior': 'AI Behavior',
    'level-design': 'Level Design',
    narrative: 'Narrative',
    'image-gen': 'Image Generator',
    'audio-gen': 'Audio Generator',
    'model-3d-gen': '3D Model Generator',
    'unity-build': 'Unity Build',
    'ai-player': 'AI Player',
    'balance-analyzer': 'Balance Analyzer',
    'feedback-synthesizer': 'Feedback Synthesizer',
  };
  return labels[agentType] || agentType;
}

