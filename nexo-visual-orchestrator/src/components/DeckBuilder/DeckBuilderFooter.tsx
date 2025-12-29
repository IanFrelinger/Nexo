// src/components/DeckBuilder/DeckBuilderFooter.tsx

import { HiCheck, HiTrash } from 'react-icons/hi';
import type { AgentDeck } from '../../types/deck';

interface DeckBuilderFooterProps {
  currentDeck: AgentDeck | null;
  onLoadDeck: () => void;
  onDeleteDeck: () => void;
  onClose?: () => void;
}

export default function DeckBuilderFooter({
  currentDeck,
  onLoadDeck,
  onDeleteDeck,
  onClose,
}: DeckBuilderFooterProps) {
  if (!currentDeck) return null;

  return (
    <div className="px-6 py-3 border-t border-slate-700 flex items-center justify-between bg-slate-800/50">
      <div className="flex items-center gap-2">
        <button
          onClick={onLoadDeck}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded font-medium flex items-center gap-2"
        >
          <HiCheck className="w-4 h-4" />
          Load Deck
        </button>
        <button
          onClick={onClose}
          className="px-4 py-2 bg-slate-700 hover:bg-slate-600 text-slate-300 rounded"
        >
          Cancel
        </button>
      </div>
      <button
        onClick={onDeleteDeck}
        className="px-3 py-2 text-red-400 hover:bg-red-500/20 rounded flex items-center gap-2"
      >
        <HiTrash className="w-4 h-4" />
        Delete
      </button>
    </div>
  );
}

