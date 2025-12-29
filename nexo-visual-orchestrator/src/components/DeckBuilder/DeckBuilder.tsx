// src/components/DeckBuilder/DeckBuilder.tsx

import { useState } from 'react';
import { useDeckStore } from '../../stores/deckStore';
import { useCustomAgentStore } from '../../stores/customAgentStore';
import type { AgentType } from '../../types/agents';
import { HiX, HiSave, HiDuplicate, HiTrash, HiShare, HiCheck } from 'react-icons/hi';
import DeckLibraryPanel from './DeckLibraryPanel';
import DeckContentsPanel from './DeckContentsPanel';
import AgentLibraryPanel from './AgentLibraryPanel';
import CreateDeckModal from './CreateDeckModal';
import { useAvailableAgents, useFilteredAgents, useDeckAgents } from './hooks';

interface DeckBuilderProps {
  onClose?: () => void;
  onDeckLoad?: (deckId: string) => void;
}

export default function DeckBuilder({ onClose, onDeckLoad }: DeckBuilderProps) {
  const {
    decks,
    currentDeck,
    createDeck,
    updateDeck,
    deleteDeck,
    duplicateDeck,
    addAgentToDeck,
    removeAgentFromDeck,
    setAgentCount,
    selectDeck,
    loadDeck,
    setDeckShared,
  } = useDeckStore();

  const { library: customAgentLibrary } = useCustomAgentStore();

  const [searchQuery, setSearchQuery] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newDeckName, setNewDeckName] = useState('');
  const [newDeckDescription, setNewDeckDescription] = useState('');
  const [newDeckTags, setNewDeckTags] = useState<string[]>([]);
  const [agentSource, setAgentSource] = useState<'built-in' | 'custom'>('built-in');

  const availableAgents = useAvailableAgents(agentSource);
  const filteredAgents = useFilteredAgents(availableAgents, searchQuery);
  const deckAgents = useDeckAgents(currentDeck);
  const totalAgents = deckAgents.reduce((sum, agent) => sum + agent.count, 0);

  const handleCreateDeck = () => {
    if (!newDeckName.trim()) return;
    
    const deck = createDeck(newDeckName.trim(), newDeckDescription.trim() || undefined, newDeckTags);
    setNewDeckName('');
    setNewDeckDescription('');
    setNewDeckTags([]);
    setShowCreateModal(false);
  };

  const handleAddAgent = (agentType: AgentType, roleId: string) => {
    if (!currentDeck) {
      const deck = createDeck('New Deck');
      addAgentToDeck(deck.id, { agentType, roleId, count: 1 });
    } else {
      addAgentToDeck(currentDeck.id, { agentType, roleId, count: 1 });
    }
  };

  const handleRemoveAgent = (agentType: AgentType) => {
    if (!currentDeck) return;
    removeAgentFromDeck(currentDeck.id, agentType);
  };

  const handleSetCount = (agentType: AgentType, count: number) => {
    if (!currentDeck) return;
    setAgentCount(currentDeck.id, agentType, count);
  };

  const handleLoadDeck = (deckId: string) => {
    loadDeck(deckId);
    if (onDeckLoad) {
      onDeckLoad(deckId);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
      <div className="bg-surface border border-slate-700 rounded-lg shadow-2xl w-full max-w-6xl h-[90vh] flex flex-col">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-700 flex items-center justify-between">
          <div>
            <h2 className="text-xl font-bold text-white">Deck Builder</h2>
            <p className="text-sm text-slate-400">Build and manage agent decks for your projects</p>
          </div>
          <div className="flex items-center gap-2">
            {currentDeck && (
              <>
                <button
                  onClick={() => setDeckShared(currentDeck.id, !currentDeck.isShared)}
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
                  onClick={() => duplicateDeck(currentDeck.id)}
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

        <div className="flex-1 flex overflow-hidden">
          <DeckLibraryPanel
            decks={decks}
            currentDeck={currentDeck}
            onSelectDeck={selectDeck}
            onCreateDeck={() => setShowCreateModal(true)}
          />

          <DeckContentsPanel
            deck={currentDeck}
            deckAgents={deckAgents}
            totalAgents={totalAgents}
            onUpdateDeck={(updates) => currentDeck && updateDeck(currentDeck.id, updates)}
            onSetCount={handleSetCount}
            onRemoveAgent={handleRemoveAgent}
          />

          <AgentLibraryPanel
            agentSource={agentSource}
            customAgentCount={customAgentLibrary.agents.length}
            searchQuery={searchQuery}
            filteredAgents={filteredAgents}
            currentDeck={currentDeck}
            onSetAgentSource={setAgentSource}
            onSetSearchQuery={setSearchQuery}
            onAddAgent={handleAddAgent}
            onSetCount={handleSetCount}
          />
        </div>

        {/* Footer Actions */}
        {currentDeck && (
          <div className="px-6 py-3 border-t border-slate-700 flex items-center justify-between bg-slate-800/50">
            <div className="flex items-center gap-2">
              <button
                onClick={() => {
                  if (onDeckLoad) {
                    onDeckLoad(currentDeck.id);
                  }
                  if (onClose) {
                    onClose();
                  }
                }}
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
              onClick={() => {
                if (confirm(`Delete deck "${currentDeck.name}"?`)) {
                  deleteDeck(currentDeck.id);
                }
              }}
              className="px-3 py-2 text-red-400 hover:bg-red-500/20 rounded flex items-center gap-2"
            >
              <HiTrash className="w-4 h-4" />
              Delete
            </button>
          </div>
        )}
      </div>

      {/* Create Deck Modal */}
      {showCreateModal && (
        <CreateDeckModal
          deckName={newDeckName}
          deckDescription={newDeckDescription}
          deckTags={newDeckTags}
          onSetDeckName={setNewDeckName}
          onSetDeckDescription={setNewDeckDescription}
          onSetDeckTags={setNewDeckTags}
          onCreateDeck={handleCreateDeck}
          onClose={() => {
            setShowCreateModal(false);
            setNewDeckName('');
            setNewDeckDescription('');
          }}
        />
      )}
    </div>
  );
}
