// src/components/Panels/CustomAgentListItem.tsx

import { HiStar, HiTrash, HiDuplicate, HiShare } from 'react-icons/hi';
import * as HiIcons from 'react-icons/hi';
import { AGENT_REGISTRY } from '../../utils/agentRegistry';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import type { CustomAgentConfig } from '../../types/customAgent';

interface CustomAgentListItemProps {
  agent: CustomAgentConfig;
  isFavorite: boolean;
  onToggleFavorite: () => void;
  onDelete: () => void;
  onDuplicate: () => void;
  onToggleShare: () => void;
  onSelect?: () => void;
}

export default function CustomAgentListItem({
  agent,
  isFavorite,
  onToggleFavorite,
  onDelete,
  onDuplicate,
  onToggleShare,
  onSelect,
}: CustomAgentListItemProps) {
  const baseAgent = AGENT_REGISTRY[agent.baseAgentType];
  const template = ROLE_TEMPLATES[agent.baseAgentType];
  const IconComponent = (HiIcons as any)[template?.icon] || HiIcons.HiCube;

  return (
    <div
      className="p-3 bg-slate-800 rounded border border-slate-700 hover:border-slate-600 transition-colors cursor-pointer group"
      onClick={onSelect}
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
                onToggleFavorite();
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
            onDuplicate();
          }}
          className="p-1 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
          title="Duplicate"
        >
          <HiDuplicate className="w-3 h-3" />
        </button>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onToggleShare();
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
            onDelete();
          }}
          className="p-1 hover:bg-red-500/20 rounded text-slate-400 hover:text-red-400 ml-auto"
          title="Delete"
        >
          <HiTrash className="w-3 h-3" />
        </button>
      </div>
    </div>
  );
}

