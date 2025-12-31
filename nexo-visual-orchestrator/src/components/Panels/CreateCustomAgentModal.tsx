// src/components/Panels/CreateCustomAgentModal.tsx

/**
 * CreateCustomAgentModal Component
 * 
 * Modal dialog for creating a new custom agent. Allows users to:
 * - Name the custom agent
 * - Provide an optional description
 * - Select a base agent type to inherit from
 * - Add tags for organization
 * 
 * The custom agent is created based on an existing agent type with
 * configurable overrides.
 */

import { useState } from 'react';
import { useCustomAgentStore } from '../../stores/customAgentStore';
import { AGENT_REGISTRY } from '../../utils/agentRegistry';
import type { AgentType } from '../../types/agents';
import { HiX } from 'react-icons/hi';

interface CreateCustomAgentModalProps {
  /** Callback invoked when the modal is closed */
  onClose: () => void;
  /** Callback invoked when the agent is successfully created */
  onSave: (agent: any) => void;
}

/**
 * CreateCustomAgentModal - Modal for creating custom agents
 * @param props - Component props
 * @returns JSX element
 */
export default function CreateCustomAgentModal({ onClose, onSave }: CreateCustomAgentModalProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [baseAgentType, setBaseAgentType] = useState<AgentType | ''>('');
  const [tags, setTags] = useState<string[]>([]);
  const [tagInput, setTagInput] = useState('');
  
  const { createCustomAgent } = useCustomAgentStore();
  const availableBaseTypes = Object.keys(AGENT_REGISTRY) as AgentType[];

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

