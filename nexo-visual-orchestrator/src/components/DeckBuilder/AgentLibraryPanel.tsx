// src/components/DeckBuilder/AgentLibraryPanel.tsx

/**
 * AgentLibraryPanel Component
 * 
 * Right sidebar panel displaying available agents that can be added to the current deck.
 * Supports toggling between built-in agents and custom user-created agents, with search
 * functionality to filter the list. Shows visual indicators for agents already in the
 * deck and allows adding new agents or increasing counts of existing ones.
 */

import { HiPlus } from 'react-icons/hi';
import * as HiIcons from 'react-icons/hi';
import type { AgentType } from '../../types/agents';
import type { AgentDeck } from '../../types/deck';
import { getColorClasses } from './utils';

interface AgentItem {
  type: AgentType;
  definition: any;
  template: any;
  isCustom?: boolean;
  customAgent?: any;
  customId?: string;
}

interface AgentLibraryPanelProps {
  /** Source of agents to display: 'built-in' or 'custom' */
  agentSource: 'built-in' | 'custom';
  /** Number of custom agents available */
  customAgentCount: number;
  /** Current search query for filtering agents */
  searchQuery: string;
  /** Filtered list of agents to display */
  filteredAgents: AgentItem[];
  /** Currently selected deck, used to check if agents are already in deck */
  currentDeck: AgentDeck | null;
  /** Callback to switch between built-in and custom agent sources */
  onSetAgentSource: (source: 'built-in' | 'custom') => void;
  /** Callback to update the search query */
  onSetSearchQuery: (query: string) => void;
  /** Callback to add a new agent to the current deck */
  onAddAgent: (agentType: AgentType, roleId: string) => void;
  /** Callback to increase the count of an agent already in the deck */
  onSetCount: (agentType: AgentType, count: number) => void;
}

/**
 * AgentLibraryPanel - Displays available agents for adding to decks
 * @param props - Component props
 * @returns JSX element
 */
export default function AgentLibraryPanel({
  agentSource,
  customAgentCount,
  searchQuery,
  filteredAgents,
  currentDeck,
  onSetAgentSource,
  onSetSearchQuery,
  onAddAgent,
  onSetCount,
}: AgentLibraryPanelProps) {
  return (
    <div className="w-80 border-l border-slate-700 flex flex-col bg-surface-dark">
      <div className="p-3 border-b border-slate-700 space-y-2">
        {/* Agent Source Toggle */}
        <div className="flex gap-1 bg-slate-800 rounded p-1">
          <button
            onClick={() => onSetAgentSource('built-in')}
            className={`flex-1 px-2 py-1 rounded text-xs font-medium transition-colors ${
              agentSource === 'built-in'
                ? 'bg-indigo-600 text-white'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Built-in
          </button>
          <button
            onClick={() => onSetAgentSource('custom')}
            className={`flex-1 px-2 py-1 rounded text-xs font-medium transition-colors ${
              agentSource === 'custom'
                ? 'bg-indigo-600 text-white'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            My Agents ({customAgentCount})
          </button>
        </div>
        
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => onSetSearchQuery(e.target.value)}
          placeholder="Search agents..."
          className="w-full px-3 py-2 bg-slate-800 border border-slate-600 rounded text-sm text-white placeholder:text-slate-500 focus:outline-none focus:border-indigo-500"
        />
      </div>
      <div className="flex-1 overflow-y-auto p-3 space-y-2">
        {filteredAgents.map((item) => {
          const IconComponent = (HiIcons as any)[item.template?.icon] || HiIcons.HiCube;
          const isInDeck = currentDeck?.agents.some((a) => 
            item.isCustom && item.customId
              ? a.agentType === item.type
              : a.agentType === item.type
          );
          const deckAgent = currentDeck?.agents.find((a) => a.agentType === item.type);
          const displayName = item.isCustom && item.customAgent
            ? item.customAgent.name
            : item.definition.label;
          
          return (
            <div
              key={item.type}
              className={`p-3 rounded border transition-colors ${
                isInDeck
                  ? 'bg-indigo-500/10 border-indigo-500/30'
                  : 'bg-slate-800 border-slate-700 hover:border-slate-600'
              }`}
            >
              <div className="flex items-start gap-3">
                <div className={`p-2 rounded flex-shrink-0 ${getColorClasses(item.definition.color).accent}`}>
                  <IconComponent className={`w-5 h-5 ${getColorClasses(item.definition.color).text}`} />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between mb-1">
                    <div className="flex items-center gap-1.5">
                      <h5 className="font-semibold text-white text-sm">{displayName}</h5>
                      {item.isCustom && (
                        <span className="text-xs bg-purple-500/20 text-purple-300 px-1.5 py-0.5 rounded">
                          Custom
                        </span>
                      )}
                    </div>
                    {isInDeck && (
                      <span className="text-xs bg-indigo-500/20 text-indigo-300 px-2 py-0.5 rounded">
                        {deckAgent?.count || 1}x
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-slate-400 mb-2 line-clamp-2">
                    {item.isCustom && item.customAgent?.description
                      ? item.customAgent.description
                      : item.definition.description}
                  </p>
                  <button
                    onClick={() => {
                      if (isInDeck) {
                        onSetCount(item.type, (deckAgent?.count || 1) + 1);
                      } else {
                        onAddAgent(item.type, item.type);
                      }
                    }}
                    className={`w-full px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                      isInDeck
                        ? 'bg-indigo-600 hover:bg-indigo-700 text-white'
                        : 'bg-slate-700 hover:bg-slate-600 text-slate-300'
                    }`}
                  >
                    {isInDeck ? (
                      <>
                        <HiPlus className="w-3 h-3 inline mr-1" />
                        Add Another
                      </>
                    ) : (
                      <>
                        <HiPlus className="w-3 h-3 inline mr-1" />
                        Add to Deck
                      </>
                    )}
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

