// src/components/DeckBuilder/CreateDeckModal.tsx

/**
 * CreateDeckModal Component
 * 
 * Modal dialog for creating a new agent deck. Collects deck name, description, and
 * optional tags. Validates that a name is provided before allowing creation.
 */

interface CreateDeckModalProps {
  /** Current deck name input value */
  deckName: string;
  /** Current deck description input value */
  deckDescription: string;
  /** Current deck tags array */
  deckTags: string[];
  /** Callback to update the deck name */
  onSetDeckName: (name: string) => void;
  /** Callback to update the deck description */
  onSetDeckDescription: (description: string) => void;
  /** Callback to update the deck tags */
  onSetDeckTags: (tags: string[]) => void;
  /** Callback to create the deck with current values */
  onCreateDeck: () => void;
  /** Callback to close the modal without creating */
  onClose: () => void;
}

/**
 * CreateDeckModal - Modal for creating a new deck
 * @param props - Component props
 * @returns JSX element
 */
export default function CreateDeckModal({
  deckName,
  deckDescription,
  deckTags,
  onSetDeckName,
  onSetDeckDescription,
  onSetDeckTags,
  onCreateDeck,
  onClose,
}: CreateDeckModalProps) {
  return (
    <div className="fixed inset-0 z-60 bg-black/70 flex items-center justify-center">
      <div className="bg-surface border border-slate-700 rounded-lg shadow-xl w-full max-w-md p-6">
        <h3 className="text-lg font-bold text-white mb-4">Create New Deck</h3>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1">Deck Name</label>
            <input
              type="text"
              value={deckName}
              onChange={(e) => onSetDeckName(e.target.value)}
              placeholder="My Awesome Deck"
              className="w-full px-3 py-2 bg-slate-800 border border-slate-600 rounded text-white"
              autoFocus
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1">Description (optional)</label>
            <textarea
              value={deckDescription}
              onChange={(e) => onSetDeckDescription(e.target.value)}
              placeholder="Describe what this deck is for..."
              rows={3}
              className="w-full px-3 py-2 bg-slate-800 border border-slate-600 rounded text-white"
            />
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={onCreateDeck}
              disabled={!deckName.trim()}
              className="flex-1 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-slate-700 disabled:text-slate-500 text-white rounded font-medium"
            >
              Create Deck
            </button>
            <button
              onClick={onClose}
              className="px-4 py-2 bg-slate-700 hover:bg-slate-600 text-slate-300 rounded"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

