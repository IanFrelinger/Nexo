// src/components/Panels/CustomAgentLibrary.tsx

/**
 * CustomAgentLibrary Component
 * 
 * Sidebar panel displaying the user's custom agent library. Provides functionality
 * to view, search, filter (all/favorites/recent), and manage custom agents. Supports
 * creating new custom agents, toggling favorites, sharing, duplicating, and deleting.
 */

import { useState, useMemo } from 'react';
import { useCustomAgentStore } from '../../stores/customAgentStore';
import { HiPlus, HiX, HiClock, HiCollection, HiStar } from 'react-icons/hi';
import CreateCustomAgentModal from './CreateCustomAgentModal';
import CustomAgentListItem from './CustomAgentListItem';

interface CustomAgentLibraryProps {
  /** Callback invoked when the panel should be collapsed */
  onCollapse?: () => void;
  /** Callback invoked when an agent is selected */
  onSelectAgent?: (agentId: string) => void;
}

/**
 * CustomAgentLibrary - Panel for managing custom user-created agents
 * @param props - Component props
 * @returns JSX element
 */
export default function CustomAgentLibrary({ onCollapse, onSelectAgent }: CustomAgentLibraryProps) {
  const [search, setSearch] = useState('');
  const [viewMode, setViewMode] = useState<'all' | 'favorites' | 'recent'>('all');
  const [showCreateModal, setShowCreateModal] = useState(false);
  
  const {
    library,
    getFavorites,
    getRecent,
    searchAgents,
    toggleFavorite,
    deleteCustomAgent,
    duplicateCustomAgent,
    setAgentShared,
  } = useCustomAgentStore();

  // Get agents based on view mode
  const displayedAgents = useMemo(() => {
    if (search) {
      return searchAgents(search);
    }
    
    switch (viewMode) {
      case 'favorites':
        return getFavorites();
      case 'recent':
        return getRecent(20);
      default:
        return library.agents;
    }
  }, [search, viewMode, library.agents, getFavorites, getRecent, searchAgents]);

  const handleDelete = (agentId: string, agentName: string) => {
    if (confirm(`Delete custom agent "${agentName}"?`)) {
      deleteCustomAgent(agentId);
    }
  };

  return (
    <div className="w-80 bg-surface border-r border-slate-700 flex flex-col h-full">
      {/* Header */}
      <div className="p-3 border-b border-slate-700 flex items-center justify-between">
        <h2 className="font-semibold text-sm">My Agent Library</h2>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setShowCreateModal(true)}
            className="p-1.5 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
            title="Create custom agent"
          >
            <HiPlus className="w-4 h-4" />
          </button>
          {onCollapse && (
            <button onClick={onCollapse} className="text-slate-400 hover:text-slate-200">
              <HiX className="w-4 h-4" />
            </button>
          )}
        </div>
      </div>

      {/* View Mode Tabs */}
      <div className="flex border-b border-slate-700">
        <button
          onClick={() => setViewMode('all')}
          className={`flex-1 px-3 py-2 text-xs font-medium transition-colors ${
            viewMode === 'all'
              ? 'bg-slate-700 text-white border-b-2 border-indigo-500'
              : 'text-slate-400 hover:text-slate-200'
          }`}
        >
          All ({library.agents.length})
        </button>
        <button
          onClick={() => setViewMode('favorites')}
          className={`flex-1 px-3 py-2 text-xs font-medium transition-colors ${
            viewMode === 'favorites'
              ? 'bg-slate-700 text-white border-b-2 border-indigo-500'
              : 'text-slate-400 hover:text-slate-200'
          }`}
        >
          <HiStar className="w-3 h-3 inline mr-1" />
          ({library.favorites.size})
        </button>
        <button
          onClick={() => setViewMode('recent')}
          className={`flex-1 px-3 py-2 text-xs font-medium transition-colors ${
            viewMode === 'recent'
              ? 'bg-slate-700 text-white border-b-2 border-indigo-500'
              : 'text-slate-400 hover:text-slate-200'
          }`}
        >
          <HiClock className="w-3 h-3 inline mr-1" />
          Recent
        </button>
      </div>

      {/* Search */}
      <div className="p-2 border-b border-slate-700">
        <input
          type="text"
          placeholder="Search agents..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full px-3 py-1.5 bg-surface-dark border border-slate-600 rounded text-sm placeholder:text-slate-500 focus:outline-none focus:border-blue-500"
        />
      </div>

      {/* Agent List */}
      <div className="flex-1 overflow-y-auto p-2 space-y-2">
        {displayedAgents.length > 0 ? (
          displayedAgents.map((agent) => (
            <CustomAgentListItem
              key={agent.id}
              agent={agent}
              isFavorite={library.favorites.has(agent.id)}
              onToggleFavorite={() => toggleFavorite(agent.id)}
              onDelete={() => handleDelete(agent.id, agent.name)}
              onDuplicate={() => duplicateCustomAgent(agent.id)}
              onToggleShare={() => setAgentShared(agent.id, !agent.isShared)}
              onSelect={() => onSelectAgent?.(agent.id)}
            />
          ))
        ) : (
          <div className="text-center text-slate-500 text-sm py-8">
            <HiCollection className="w-8 h-8 mx-auto mb-2 opacity-50" />
            <p>
              {search
                ? 'No agents found'
                : viewMode === 'favorites'
                ? 'No favorites yet'
                : viewMode === 'recent'
                ? 'No recent agents'
                : 'No custom agents yet'}
            </p>
            {!search && viewMode === 'all' && (
              <p className="text-xs mt-1">Create your first custom agent</p>
            )}
          </div>
        )}
      </div>

      {/* Create Agent Modal */}
      {showCreateModal && (
        <CreateCustomAgentModal
          onClose={() => setShowCreateModal(false)}
          onSave={() => {
            setShowCreateModal(false);
          }}
        />
      )}
    </div>
  );
}

