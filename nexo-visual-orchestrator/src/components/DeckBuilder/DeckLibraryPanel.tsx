// src/components/DeckBuilder/DeckLibraryPanel.tsx

import { HiPlus, HiShare, HiCollection } from 'react-icons/hi';
import type { AgentDeck } from '../../types/deck';

interface DeckLibraryPanelProps {
  decks: AgentDeck[];
  currentDeck: AgentDeck | null;
  onSelectDeck: (deckId: string) => void;
  onCreateDeck: () => void;
}

export default function DeckLibraryPanel({
  decks,
  currentDeck,
  onSelectDeck,
  onCreateDeck,
}: DeckLibraryPanelProps) {
  return (
    <div className="w-64 border-r border-slate-700 flex flex-col bg-surface-dark">
      <div className="p-3 border-b border-slate-700">
        <button
          onClick={onCreateDeck}
          className="w-full px-3 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded text-sm font-medium flex items-center justify-center gap-2"
        >
          <HiPlus className="w-4 h-4" />
          New Deck
        </button>
      </div>
      <div className="flex-1 overflow-y-auto p-2 space-y-1">
        {decks.map((deck) => (
          <div
            key={deck.id}
            onClick={() => onSelectDeck(deck.id)}
            className={`p-3 rounded cursor-pointer transition-colors ${
              currentDeck?.id === deck.id
                ? 'bg-indigo-500/20 border border-indigo-500/30'
                : 'bg-slate-800 hover:bg-slate-700 border border-slate-700'
            }`}
          >
            <div className="flex items-start justify-between mb-1">
              <h3 className="font-semibold text-sm text-white truncate">{deck.name}</h3>
              {deck.isShared && (
                <HiShare className="w-3 h-3 text-green-400 flex-shrink-0 ml-1" />
              )}
            </div>
            {deck.description && (
              <p className="text-xs text-slate-400 mb-2 line-clamp-2">{deck.description}</p>
            )}
            <div className="flex items-center gap-2 text-xs text-slate-500">
              <span>{deck.agents.length} agents</span>
              <span>•</span>
              <span>{deck.agents.reduce((sum, a) => sum + a.count, 0)} total</span>
            </div>
            {deck.tags.length > 0 && (
              <div className="flex flex-wrap gap-1 mt-2">
                {deck.tags.slice(0, 2).map((tag) => (
                  <span
                    key={tag}
                    className="text-xs bg-slate-700 text-slate-300 px-1.5 py-0.5 rounded"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            )}
          </div>
        ))}
        {decks.length === 0 && (
          <div className="text-center text-slate-500 text-sm py-8">
            <HiCollection className="w-8 h-8 mx-auto mb-2 opacity-50" />
            <p>No decks yet</p>
            <p className="text-xs mt-1">Create your first deck to get started</p>
          </div>
        )}
      </div>
    </div>
  );
}

