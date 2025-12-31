// src/components/DeckBuilder/DeckBuilder.tsx

/**
 * DeckBuilder Component
 * 
 * Main orchestrator component for the deck building interface. Provides a three-panel
 * layout for creating and managing agent decks:
 * - Left: Deck library (list of all decks)
 * - Center: Current deck contents and editing
 * - Right: Agent library (available agents to add)
 * 
 * Features:
 * - Create, edit, and delete decks
 * - Add/remove agents from decks
 * - Adjust agent counts within decks
 * - Toggle between built-in and custom agents
 * - Search and filter agents
 * - Load decks onto the main canvas
 */

import { useState } from 'react';
import { useDeckStore } from '../../stores/deckStore';
import { useCustomAgentStore } from '../../stores/customAgentStore';
import type { AgentType } from '../../types/agents';
import DeckLibraryPanel from './DeckLibraryPanel';
import DeckContentsPanel from './DeckContentsPanel';
import AgentLibraryPanel from './AgentLibraryPanel';
import CreateDeckModal from './CreateDeckModal';
import DeckBuilderHeader from './DeckBuilderHeader';
import DeckBuilderFooter from './DeckBuilderFooter';
import { useAvailableAgents, useFilteredAgents, useDeckAgents } from './hooks';

interface DeckBuilderProps {
  /** Callback invoked when the deck builder is closed */
  onClose?: () => void;
  /** Callback invoked when a deck is loaded onto the canvas */
  onDeckLoad?: (deckId: string) => void;
}

/**
 * DeckBuilder component - Main deck building interface
 * @param props - Component props
 * @returns JSX element
 */
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
        <DeckBuilderHeader
          currentDeck={currentDeck}
          onSetDeckShared={setDeckShared}
          onDuplicateDeck={duplicateDeck}
          onClose={onClose}
        />

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

        <DeckBuilderFooter
          currentDeck={currentDeck}
          onLoadDeck={() => {
            if (currentDeck) {
              handleLoadDeck(currentDeck.id);
            }
          }}
          onDeleteDeck={() => {
            if (currentDeck && confirm(`Delete deck "${currentDeck.name}"?`)) {
              deleteDeck(currentDeck.id);
            }
          }}
          onClose={onClose}
        />
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
