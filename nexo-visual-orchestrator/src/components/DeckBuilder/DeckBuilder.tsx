// src/components/DeckBuilder/DeckBuilder.tsx

import { useState, useMemo } from 'react';
import { useDeckStore } from '../../stores/deckStore';
import { useCustomAgentStore } from '../../stores/customAgentStore';
import { AGENT_REGISTRY } from '../../utils/agentRegistry';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import type { AgentType } from '../../types/agents';
import type { DeckAgent } from '../../types/deck';
import * as HiIcons from 'react-icons/hi';
import { 
  HiPlus, HiMinus, HiX, HiSave, HiDuplicate, HiTrash,
  HiTag, HiShare, HiCollection, HiCheck
} from 'react-icons/hi';

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

  const [searchQuery, setSearchQuery] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newDeckName, setNewDeckName] = useState('');
  const [newDeckDescription, setNewDeckDescription] = useState('');
  const [newDeckTags, setNewDeckTags] = useState<string[]>([]);

  // Get available agents from registry or custom library
  const availableAgents = useMemo(() => {
    if (agentSource === 'custom') {
      return customAgentLibrary.agents.map((customAgent) => {
        const baseDef = AGENT_REGISTRY[customAgent.baseAgentType];
        const template = ROLE_TEMPLATES[customAgent.baseAgentType];
        return {
          type: customAgent.baseAgentType as AgentType,
          definition: baseDef,
          template,
          customAgent, // Include custom agent config
          isCustom: true,
          customId: customAgent.id,
        };
      }).filter(item => item.template);
    } else {
      return Object.entries(AGENT_REGISTRY).map(([type, def]) => ({
        type: type as AgentType,
        definition: def,
        template: ROLE_TEMPLATES[type],
        isCustom: false,
      })).filter(item => item.template);
    }
  }, [agentSource, customAgentLibrary.agents]);

  // Filter agents by search
  const filteredAgents = useMemo(() => {
    if (!searchQuery) return availableAgents;
    const query = searchQuery.toLowerCase();
    return availableAgents.filter((item) => {
      const name = (item as any).isCustom && (item as any).customAgent
        ? (item as any).customAgent.name
        : item.definition.label;
      const description = (item as any).isCustom && (item as any).customAgent?.description
        ? (item as any).customAgent.description
        : item.definition.description;
      
      return (
        name.toLowerCase().includes(query) ||
        description.toLowerCase().includes(query) ||
        item.type.toLowerCase().includes(query) ||
        ((item as any).customAgent?.tags || []).some((tag: string) => tag.toLowerCase().includes(query))
      );
    });
  }, [availableAgents, searchQuery]);

  // Get agents in current deck
  const deckAgents = useMemo(() => {
    if (!currentDeck) return [];
    return currentDeck.agents.map((deckAgent) => {
      const agentDef = AGENT_REGISTRY[deckAgent.agentType];
      const template = ROLE_TEMPLATES[deckAgent.roleId];
      return {
        ...deckAgent,
        definition: agentDef,
        template,
      };
    });
  }, [currentDeck]);

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
      // Create a new deck if none selected
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

  const totalAgents = deckAgents.reduce((sum, agent) => sum + agent.count, 0);

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
                  onClick={() => {
                    duplicateDeck(currentDeck.id);
                  }}
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
          {/* Left: Deck Library */}
          <div className="w-64 border-r border-slate-700 flex flex-col bg-surface-dark">
            <div className="p-3 border-b border-slate-700">
              <button
                onClick={() => setShowCreateModal(true)}
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
                  onClick={() => selectDeck(deck.id)}
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

          {/* Center: Agent Selection */}
          <div className="flex-1 flex flex-col overflow-hidden">
            {currentDeck ? (
              <>
                {/* Current Deck Info */}
                <div className="p-4 border-b border-slate-700 bg-slate-800/50">
                  <div className="flex items-center justify-between mb-2">
                    <div>
                      <h3 className="font-bold text-lg text-white">{currentDeck.name}</h3>
                      {currentDeck.description && (
                        <p className="text-sm text-slate-400 mt-1">{currentDeck.description}</p>
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
                      value={currentDeck.name}
                      onChange={(e) => updateDeck(currentDeck.id, { name: e.target.value })}
                      className="flex-1 px-3 py-1.5 bg-slate-700 border border-slate-600 rounded text-sm text-white"
                      placeholder="Deck name"
                    />
                    <input
                      type="text"
                      value={currentDeck.description || ''}
                      onChange={(e) => updateDeck(currentDeck.id, { description: e.target.value })}
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
                                onClick={() => handleSetCount(deckAgent.agentType, deckAgent.count - 1)}
                                className="p-1 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
                              >
                                <HiMinus className="w-4 h-4" />
                              </button>
                              <span className="w-12 text-center font-semibold text-white">{deckAgent.count}</span>
                              <button
                                onClick={() => handleSetCount(deckAgent.agentType, deckAgent.count + 1)}
                                className="p-1 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
                              >
                                <HiPlus className="w-4 h-4" />
                              </button>
                              <button
                                onClick={() => handleRemoveAgent(deckAgent.agentType)}
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
              </>
            ) : (
              <div className="flex-1 flex items-center justify-center text-center text-slate-500">
                <div>
                  <HiCollection className="w-16 h-16 mx-auto mb-4 opacity-50" />
                  <p className="text-lg mb-2">No deck selected</p>
                  <p className="text-sm">Select a deck from the library or create a new one</p>
                </div>
              </div>
            )}
          </div>

          {/* Right: Agent Library */}
          <div className="w-80 border-l border-slate-700 flex flex-col bg-surface-dark">
            <div className="p-3 border-b border-slate-700 space-y-2">
              {/* Agent Source Toggle */}
              <div className="flex gap-1 bg-slate-800 rounded p-1">
                <button
                  onClick={() => setAgentSource('built-in')}
                  className={`flex-1 px-2 py-1 rounded text-xs font-medium transition-colors ${
                    agentSource === 'built-in'
                      ? 'bg-indigo-600 text-white'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  Built-in
                </button>
                <button
                  onClick={() => setAgentSource('custom')}
                  className={`flex-1 px-2 py-1 rounded text-xs font-medium transition-colors ${
                    agentSource === 'custom'
                      ? 'bg-indigo-600 text-white'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  My Agents ({customAgentLibrary.agents.length})
                </button>
              </div>
              
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search agents..."
                className="w-full px-3 py-2 bg-slate-800 border border-slate-600 rounded text-sm text-white placeholder:text-slate-500 focus:outline-none focus:border-indigo-500"
              />
            </div>
            <div className="flex-1 overflow-y-auto p-3 space-y-2">
              {filteredAgents.map((item) => {
                const IconComponent = (HiIcons as any)[item.template?.icon] || HiIcons.HiCube;
                // For custom agents, check by customId if available, otherwise by type
                const isInDeck = currentDeck?.agents.some((a) => 
                  (item as any).isCustom && (item as any).customId
                    ? a.agentType === item.type // Match by base type for now
                    : a.agentType === item.type
                );
                const deckAgent = currentDeck?.agents.find((a) => a.agentType === item.type);
                const displayName = (item as any).isCustom && (item as any).customAgent
                  ? (item as any).customAgent.name
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
                            {(item as any).isCustom && (
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
                          {(item as any).isCustom && (item as any).customAgent?.description
                            ? (item as any).customAgent.description
                            : item.definition.description}
                        </p>
                        <button
                          onClick={() => {
                            if (isInDeck) {
                              handleSetCount(item.type, (deckAgent?.count || 1) + 1);
                            } else {
                              handleAddAgent(item.type, item.type);
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
                onClick={() => {
                  if (onClose) {
                    onClose();
                  }
                }}
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
        <div className="fixed inset-0 z-60 bg-black/70 flex items-center justify-center">
          <div className="bg-surface border border-slate-700 rounded-lg shadow-xl w-full max-w-md p-6">
            <h3 className="text-lg font-bold text-white mb-4">Create New Deck</h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Deck Name</label>
                <input
                  type="text"
                  value={newDeckName}
                  onChange={(e) => setNewDeckName(e.target.value)}
                  placeholder="My Awesome Deck"
                  className="w-full px-3 py-2 bg-slate-800 border border-slate-600 rounded text-white"
                  autoFocus
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Description (optional)</label>
                <textarea
                  value={newDeckDescription}
                  onChange={(e) => setNewDeckDescription(e.target.value)}
                  placeholder="Describe what this deck is for..."
                  rows={3}
                  className="w-full px-3 py-2 bg-slate-800 border border-slate-600 rounded text-white"
                />
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={handleCreateDeck}
                  disabled={!newDeckName.trim()}
                  className="flex-1 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-slate-700 disabled:text-slate-500 text-white rounded font-medium"
                >
                  Create Deck
                </button>
                <button
                  onClick={() => {
                    setShowCreateModal(false);
                    setNewDeckName('');
                    setNewDeckDescription('');
                  }}
                  className="px-4 py-2 bg-slate-700 hover:bg-slate-600 text-slate-300 rounded"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// Helper to get color classes
function getColorClasses(color: string) {
  const colorMap: Record<string, { bg: string; text: string; border: string; accent: string }> = {
    purple: { bg: 'bg-purple-500/10', text: 'text-purple-400', border: 'border-purple-500/30', accent: 'bg-purple-500/20' },
    red: { bg: 'bg-red-500/10', text: 'text-red-400', border: 'border-red-500/30', accent: 'bg-red-500/20' },
    yellow: { bg: 'bg-yellow-500/10', text: 'text-yellow-400', border: 'border-yellow-500/30', accent: 'bg-yellow-500/20' },
    cyan: { bg: 'bg-cyan-500/10', text: 'text-cyan-400', border: 'border-cyan-500/30', accent: 'bg-cyan-500/20' },
    green: { bg: 'bg-green-500/10', text: 'text-green-400', border: 'border-green-500/30', accent: 'bg-green-500/20' },
    indigo: { bg: 'bg-indigo-500/10', text: 'text-indigo-400', border: 'border-indigo-500/30', accent: 'bg-indigo-500/20' },
    pink: { bg: 'bg-pink-500/10', text: 'text-pink-400', border: 'border-pink-500/30', accent: 'bg-pink-500/20' },
    orange: { bg: 'bg-orange-500/10', text: 'text-orange-400', border: 'border-orange-500/30', accent: 'bg-orange-500/20' },
    slate: { bg: 'bg-slate-500/10', text: 'text-slate-400', border: 'border-slate-500/30', accent: 'bg-slate-500/20' },
    teal: { bg: 'bg-teal-500/10', text: 'text-teal-400', border: 'border-teal-500/30', accent: 'bg-teal-500/20' },
    amber: { bg: 'bg-amber-500/10', text: 'text-amber-400', border: 'border-amber-500/30', accent: 'bg-amber-500/20' },
    emerald: { bg: 'bg-emerald-500/10', text: 'text-emerald-400', border: 'border-emerald-500/30', accent: 'bg-emerald-500/20' },
  };
  return colorMap[color] || colorMap.slate;
}

