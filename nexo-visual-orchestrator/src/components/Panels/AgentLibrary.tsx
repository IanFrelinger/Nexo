import { useState } from 'react';
import { AGENT_CATEGORIES, getAgentsByCategory } from '../../utils/agentRegistry';
import type { AgentType } from '../../types/agents';
import {
  HiOutlineCube,
  HiOutlinePlay,
  HiOutlineChartBar,
  HiOutlineCurrencyDollar,
  HiOutlineCpuChip,
  HiOutlineMap,
  HiOutlineBookOpen,
  HiOutlinePhoto,
  HiOutlineMusicalNote,
  HiOutlineCubeTransparent,
  HiOutlineArrowPath,
  HiOutlineBolt,
  HiOutlineFire,
  HiOutlineWrenchScrewdriver,
} from 'react-icons/hi2';
import { HiX } from 'react-icons/hi';
import type { IconType } from 'react-icons';

const agentIconMap: Record<string, IconType> = {
  architect: HiOutlineBolt,
  combat: HiOutlineFire,
  economy: HiOutlineCurrencyDollar,
  'ai-behavior': HiOutlineCpuChip,
  'level-design': HiOutlineMap,
  narrative: HiOutlineBookOpen,
  'image-gen': HiOutlinePhoto,
  'audio-gen': HiOutlineMusicalNote,
  'model-3d-gen': HiOutlineCubeTransparent,
  'unity-build': HiOutlineWrenchScrewdriver,
  'ai-player': HiOutlinePlay,
  'balance-analyzer': HiOutlineChartBar,
  'feedback-synthesizer': HiOutlineArrowPath,
};

const categoryIconMap: Record<string, IconType> = {
  architect: HiOutlineBolt,
  domain: HiOutlineCube,
  asset: HiOutlinePhoto,
  build: HiOutlineWrenchScrewdriver,
  playtest: HiOutlinePlay,
  analysis: HiOutlineChartBar,
};

interface AgentLibraryProps {
  onCollapse?: () => void;
}

export default function AgentLibrary({ onCollapse }: AgentLibraryProps) {
  const [search, setSearch] = useState('');
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(
    new Set(AGENT_CATEGORIES.map((c) => c.id))
  );

  const toggleCategory = (categoryId: string) => {
    setExpandedCategories((prev) => {
      const next = new Set(prev);
      if (next.has(categoryId)) {
        next.delete(categoryId);
      } else {
        next.add(categoryId);
      }
      return next;
    });
  };

  const onDragStart = (event: React.DragEvent, agentType: AgentType) => {
    event.dataTransfer.setData('application/reactflow', agentType);
    event.dataTransfer.effectAllowed = 'move';
  };

  return (
    <div className="w-64 bg-surface border-r border-slate-700 flex flex-col h-full">
      {/* Header */}
      <div className="p-3 border-b border-slate-700 flex items-center justify-between">
        <h2 className="font-semibold text-sm">Agent Library</h2>
        {onCollapse && (
          <button onClick={onCollapse} className="text-slate-400 hover:text-slate-200">
            <HiX className="w-4 h-4" />
          </button>
        )}
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
      <div className="flex-1 overflow-y-auto p-2 space-y-1">
        {AGENT_CATEGORIES.map((category) => {
          const agents = getAgentsByCategory(category.id).filter(
            (agent) =>
              search === '' ||
              agent.label.toLowerCase().includes(search.toLowerCase()) ||
              agent.description.toLowerCase().includes(search.toLowerCase())
          );

          if (agents.length === 0) return null;

          const isExpanded = expandedCategories.has(category.id);

          return (
            <div key={category.id}>
              <button
                onClick={() => toggleCategory(category.id)}
                className="w-full flex items-center gap-2 px-2 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded"
              >
                <span className="text-xs">{isExpanded ? '▼' : '▶'}</span>
                {(() => {
                  const CategoryIcon = categoryIconMap[category.id];
                  return CategoryIcon ? (
                    <CategoryIcon className="w-4 h-4" />
                  ) : (
                    <span>{category.icon}</span>
                  );
                })()}
                <span>{category.label}</span>
                <span className="ml-auto text-xs text-slate-500">{agents.length}</span>
              </button>

              {isExpanded && (
                <div className="ml-4 mt-1 space-y-1">
                  {agents.map((agent) => (
                    <div
                      key={agent.type}
                      draggable
                      onDragStart={(e) => onDragStart(e, agent.type)}
                      className="flex items-center gap-2 px-2 py-1.5 bg-surface-dark border border-slate-700 rounded cursor-grab hover:border-slate-500 active:cursor-grabbing"
                      title={agent.description}
                    >
                      {(() => {
                        const AgentIcon = agentIconMap[agent.type];
                        return AgentIcon ? (
                          <AgentIcon className="w-4 h-4" style={{ color: agent.color }} />
                        ) : (
                          <span>{agent.icon}</span>
                        );
                      })()}
                      <span className="text-sm">{agent.label}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Help text */}
      <div className="p-2 border-t border-slate-700 text-xs text-slate-500">
        Drag agents onto the canvas to add them to your workflow
      </div>
    </div>
  );
}

