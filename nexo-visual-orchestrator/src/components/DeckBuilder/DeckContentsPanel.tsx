// src/components/DeckBuilder/DeckContentsPanel.tsx

/**
 * DeckContentsPanel Component
 * 
 * Center panel displaying the contents of the currently selected deck. Shows deck
 * metadata (name, description, total agent count) and a list of all agents in the deck
 * with controls to adjust counts and remove agents. Displays an empty state when no
 * deck is selected.
 */

import { HiPlus, HiMinus, HiTrash, HiCollection } from 'react-icons/hi';
import * as HiIcons from 'react-icons/hi';
import type { AgentDeck } from '../../types/deck';
import type { AgentType } from '../../types/agents';
import { getColorClasses } from './utils';

interface DeckAgentItem {
  agentType: AgentType;
  count: number;
  definition: any;
  template: any;
}

interface DeckContentsPanelProps {
  /** Currently selected deck, or null if none selected */
  deck: AgentDeck | null;
  /** Array of agents in the current deck with their definitions */
  deckAgents: DeckAgentItem[];
  /** Total number of agent instances across all agents in the deck */
  totalAgents: number;
  /** Callback to update deck metadata (name, description, etc.) */
  onUpdateDeck: (updates: Partial<AgentDeck>) => void;
  /** Callback to change the count of a specific agent type in the deck */
  onSetCount: (agentType: AgentType, count: number) => void;
  /** Callback to remove an agent type from the deck */
  onRemoveAgent: (agentType: AgentType) => void;
}

/**
 * DeckContentsPanel - Displays and manages the contents of the selected deck
 * @param props - Component props
 * @returns JSX element
 */
export default function DeckContentsPanel({
  deck,
  deckAgents,
  totalAgents,
  onUpdateDeck,
  onSetCount,
  onRemoveAgent,
}: DeckContentsPanelProps) {
  if (!deck) {
    return (
      <div className="flex-1 flex items-center justify-center text-center text-slate-500">
        <div>
          <HiCollection className="w-16 h-16 mx-auto mb-4 opacity-50" />
          <p className="text-lg mb-2">No deck selected</p>
          <p className="text-sm">Select a deck from the library or create a new one</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      {/* Current Deck Info */}
      <div className="p-4 border-b border-slate-700 bg-slate-800/50">
        <div className="flex items-center justify-between mb-2">
          <div>
            <h3 className="font-bold text-lg text-white">{deck.name}</h3>
            {deck.description && (
              <p className="text-sm text-slate-400 mt-1">{deck.description}</p>
            )}
          </div>
          <div className="text-right">
            <div className="text-sm text-slate-400">Total Agents</div>
            <div className="text-2xl font-bold text-white">{totalAgents}</div>
          </div>
        </div>
        <div className="flex items-center gap-2 mt-3">
          <input
            type="text"
            value={deck.name}
            onChange={(e) => onUpdateDeck({ name: e.target.value })}
            className="flex-1 px-3 py-1.5 bg-slate-700 border border-slate-600 rounded text-sm text-white"
            placeholder="Deck name"
          />
          <input
            type="text"
            value={deck.description || ''}
            onChange={(e) => onUpdateDeck({ description: e.target.value })}
            className="flex-1 px-3 py-1.5 bg-slate-700 border border-slate-600 rounded text-sm text-white"
            placeholder="Description (optional)"
          />
        </div>
      </div>

      {/* Deck Contents */}
      <div className="flex-1 overflow-y-auto p-4">
        <h4 className="text-sm font-semibold text-slate-400 mb-3 uppercase tracking-wide">
          Deck Contents ({deckAgents.length} unique, {totalAgents} total)
        </h4>
        {deckAgents.length > 0 ? (
          <div className="space-y-2">
            {deckAgents.map((deckAgent) => {
              const IconComponent = (HiIcons as any)[deckAgent.template?.icon] || HiIcons.HiCube;
              return (
                <div
                  key={deckAgent.agentType}
                  className="flex items-center gap-3 p-3 bg-slate-800 rounded border border-slate-700"
                >
                  <div className={`p-2 rounded ${getColorClasses(deckAgent.definition.color).accent}`}>
                    <IconComponent className={`w-5 h-5 ${getColorClasses(deckAgent.definition.color).text}`} />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h5 className="font-semibold text-white text-sm">{deckAgent.definition.label}</h5>
                    <p className="text-xs text-slate-400 truncate">{deckAgent.definition.description}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => onSetCount(deckAgent.agentType, deckAgent.count - 1)}
                      className="p-1 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
                    >
                      <HiMinus className="w-4 h-4" />
                    </button>
                    <span className="w-12 text-center font-semibold text-white">{deckAgent.count}</span>
                    <button
                      onClick={() => onSetCount(deckAgent.agentType, deckAgent.count + 1)}
                      className="p-1 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
                    >
                      <HiPlus className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => onRemoveAgent(deckAgent.agentType)}
                      className="p-1 hover:bg-red-500/20 rounded text-slate-400 hover:text-red-400 ml-2"
                    >
                      <HiTrash className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        ) : (
          <div className="text-center text-slate-500 py-12">
            <HiCollection className="w-12 h-12 mx-auto mb-3 opacity-50" />
            <p>Deck is empty</p>
            <p className="text-xs mt-1">Add agents from the library on the right</p>
          </div>
        )}
      </div>
    </div>
  );
}

