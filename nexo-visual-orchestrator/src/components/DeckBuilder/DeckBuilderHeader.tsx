// src/components/DeckBuilder/DeckBuilderHeader.tsx

import { HiX, HiShare, HiDuplicate } from 'react-icons/hi';
import type { AgentDeck } from '../../types/deck';

interface DeckBuilderHeaderProps {
  currentDeck: AgentDeck | null;
  onSetDeckShared: (deckId: string, isShared: boolean) => void;
  onDuplicateDeck: (deckId: string) => void;
  onClose?: () => void;
}

export default function DeckBuilderHeader({
  currentDeck,
  onSetDeckShared,
  onDuplicateDeck,
  onClose,
}: DeckBuilderHeaderProps) {
  return (
    <div className="px-6 py-4 border-b border-slate-700 flex items-center justify-between">
      <div>
        <h2 className="text-xl font-bold text-white">Deck Builder</h2>
        <p className="text-sm text-slate-400">Build and manage agent decks for your projects</p>
      </div>
      <div className="flex items-center gap-2">
        {currentDeck && (
          <>
            <button
              onClick={() => onSetDeckShared(currentDeck.id, !currentDeck.isShared)}
              className={`px-3 py-1.5 rounded text-sm flex items-center gap-2 ${
                currentDeck.isShared
                  ? 'bg-green-500/20 text-green-400 border border-green-500/30'
                  : 'bg-slate-700 text-slate-300 border border-slate-600'
              }`}
            >
              <HiShare className="w-4 h-4" />
              {currentDeck.isShared ? 'Shared' : 'Private'}
            </button>
            <button
              onClick={() => onDuplicateDeck(currentDeck.id)}
              className="px-3 py-1.5 bg-slate-700 text-slate-300 rounded text-sm flex items-center gap-2 hover:bg-slate-600"
            >
              <HiDuplicate className="w-4 h-4" />
              Duplicate
            </button>
          </>
        )}
        {onClose && (
          <button
            onClick={onClose}
            className="p-2 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
          >
            <HiX className="w-5 h-5" />
          </button>
        )}
      </div>
    </div>
  );
}

