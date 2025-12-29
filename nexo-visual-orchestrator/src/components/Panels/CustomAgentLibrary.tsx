// src/components/Panels/CustomAgentLibrary.tsx

import { useState, useMemo } from 'react';
import { useCustomAgentStore } from '../../stores/customAgentStore';
import { AGENT_REGISTRY } from '../../utils/agentRegistry';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import type { AgentType } from '../../types/agents';
import * as HiIcons from 'react-icons/hi';
import { 
  HiPlus, HiStar, HiTrash, HiDuplicate, HiShare, HiPencil,
  HiX, HiClock, HiCollection
} from 'react-icons/hi';

interface CustomAgentLibraryProps {
  onCollapse?: () => void;
  onSelectAgent?: (agentId: string) => void;
}

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
          displayedAgents.map((agent) => {
            const baseAgent = AGENT_REGISTRY[agent.baseAgentType];
            const template = ROLE_TEMPLATES[agent.baseAgentType];
            const IconComponent = (HiIcons as any)[template?.icon] || HiIcons.HiCube;
            const isFavorite = library.favorites.has(agent.id);
            
            return (
              <div
                key={agent.id}
                className="p-3 bg-slate-800 rounded border border-slate-700 hover:border-slate-600 transition-colors cursor-pointer group"
                onClick={() => onSelectAgent?.(agent.id)}
              >
                <div className="flex items-start gap-2 mb-2">
                  <div className="p-1.5 rounded bg-slate-700 flex-shrink-0">
                    <IconComponent className="w-4 h-4 text-slate-300" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between mb-1">
                      <h3 className="font-semibold text-sm text-white truncate">{agent.name}</h3>
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          toggleFavorite(agent.id);
                        }}
                        className={`p-1 rounded transition-colors ${
                          isFavorite
                            ? 'text-yellow-400 hover:text-yellow-300'
                            : 'text-slate-500 hover:text-yellow-400'
                        }`}
                        title={isFavorite ? 'Remove from favorites' : 'Add to favorites'}
                      >
                        <HiStar className={`w-3 h-3 ${isFavorite ? 'fill-current' : ''}`} />
                      </button>
                    </div>
                    <p className="text-xs text-slate-400 mb-1 line-clamp-2">
                      {agent.description || baseAgent?.description || 'Custom agent'}
                    </p>
                    <div className="flex items-center gap-2 text-xs text-slate-500">
                      <span>Based on: {baseAgent?.label || agent.baseAgentType}</span>
                      {agent.isShared && (
                        <span className="text-green-400 flex items-center gap-1">
                          <HiShare className="w-3 h-3" />
                          Shared
                        </span>
                      )}
                    </div>
                  </div>
                </div>
                
                {/* Tags */}
                {agent.tags.length > 0 && (
                  <div className="flex flex-wrap gap-1 mb-2">
                    {agent.tags.slice(0, 3).map((tag) => (
                      <span
                        key={tag}
                        className="text-xs bg-slate-700 text-slate-300 px-1.5 py-0.5 rounded"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                )}
                
                {/* Actions */}
                <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      duplicateCustomAgent(agent.id);
                    }}
                    className="p-1 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
                    title="Duplicate"
                  >
                    <HiDuplicate className="w-3 h-3" />
                  </button>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setAgentShared(agent.id, !agent.isShared);
                    }}
                    className={`p-1 hover:bg-slate-700 rounded ${
                      agent.isShared
                        ? 'text-green-400 hover:text-green-300'
                        : 'text-slate-400 hover:text-white'
                    }`}
                    title={agent.isShared ? 'Make private' : 'Share across projects'}
                  >
                    <HiShare className="w-3 h-3" />
                  </button>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDelete(agent.id, agent.name);
                    }}
                    className="p-1 hover:bg-red-500/20 rounded text-slate-400 hover:text-red-400 ml-auto"
                    title="Delete"
                  >
                    <HiTrash className="w-3 h-3" />
                  </button>
                </div>
              </div>
            );
          })
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
          onSave={(agent) => {
            setShowCreateModal(false);
            // Agent is already created by the modal
          }}
        />
      )}
    </div>
  );
}

// Create Custom Agent Modal Component
interface CreateCustomAgentModalProps {
  onClose: () => void;
  onSave: (agent: any) => void;
}

function CreateCustomAgentModal({ onClose, onSave }: CreateCustomAgentModalProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [baseAgentType, setBaseAgentType] = useState<AgentType | ''>('');
  const [tags, setTags] = useState<string[]>([]);
  const [tagInput, setTagInput] = useState('');
  
  const { createCustomAgent } = useCustomAgentStore();

  const availableBaseTypes = Object.keys(AGENT_REGISTRY) as AgentType[];
  
  // Get base agent for selected type
  const selectedBaseAgent = baseAgentType ? AGENT_REGISTRY[baseAgentType] : null;

  const handleAddTag = () => {
    if (tagInput.trim() && !tags.includes(tagInput.trim())) {
      setTags([...tags, tagInput.trim()]);
      setTagInput('');
    }
  };

  const handleRemoveTag = (tag: string) => {
    setTags(tags.filter((t) => t !== tag));
  };

  const handleSave = () => {
    if (!name.trim() || !baseAgentType) return;
    
    const baseAgent = AGENT_REGISTRY[baseAgentType];
    const agent = createCustomAgent(
      name.trim(),
      baseAgentType,
      baseAgent.defaultConfig || {},
      description.trim() || undefined,
      tags
    );
    
    onSave(agent);
  };

  return (
    <div className="fixed inset-0 z-60 bg-black/70 flex items-center justify-center">
      <div className="bg-surface border border-slate-700 rounded-lg shadow-xl w-full max-w-md p-6">
        <h3 className="text-lg font-bold text-white mb-4">Create Custom Agent</h3>
        
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1">Agent Name</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="My Custom Combat Agent"
              className="w-full px-3 py-2 bg-slate-800 border border-slate-600 rounded text-white"
              autoFocus
            />
          </div>
          
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1">Description (optional)</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe this custom agent..."
              rows={2}
              className="w-full px-3 py-2 bg-slate-800 border border-slate-600 rounded text-white"
            />
          </div>
          
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1">Base Agent Type</label>
            <select
              value={baseAgentType}
              onChange={(e) => setBaseAgentType(e.target.value as AgentType)}
              className="w-full px-3 py-2 bg-slate-800 border border-slate-600 rounded text-white"
            >
              <option value="">Select base agent...</option>
              {availableBaseTypes.map((type) => {
                const agent = AGENT_REGISTRY[type];
                return (
                  <option key={type} value={type}>
                    {agent.label} - {agent.description}
                  </option>
                );
              })}
            </select>
          </div>
          
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1">Tags</label>
            <div className="flex gap-2 mb-2">
              <input
                type="text"
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && handleAddTag()}
                placeholder="Add tag..."
                className="flex-1 px-3 py-2 bg-slate-800 border border-slate-600 rounded text-white"
              />
              <button
                onClick={handleAddTag}
                className="px-3 py-2 bg-slate-700 hover:bg-slate-600 text-slate-300 rounded"
              >
                Add
              </button>
            </div>
            {tags.length > 0 && (
              <div className="flex flex-wrap gap-1">
                {tags.map((tag) => (
                  <span
                    key={tag}
                    className="inline-flex items-center gap-1 px-2 py-1 bg-slate-700 text-slate-300 rounded text-xs"
                  >
                    {tag}
                    <button
                      onClick={() => handleRemoveTag(tag)}
                      className="hover:text-red-400"
                    >
                      <HiX className="w-3 h-3" />
                    </button>
                  </span>
                ))}
              </div>
            )}
          </div>
          
          <div className="flex items-center gap-2 pt-2">
            <button
              onClick={handleSave}
              disabled={!name.trim() || !baseAgentType}
              className="flex-1 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-slate-700 disabled:text-slate-500 text-white rounded font-medium"
            >
              Create Agent
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

