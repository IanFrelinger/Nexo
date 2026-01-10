import React, { useState } from 'react';
import { useAgents, useBricks } from '../../hooks/useCatalog';
import { useClusters } from '../../hooks/useClusters';

interface LibraryPanelProps {
  onDragStart: (type: string, itemId: string) => void;
}

export const LibraryPanel: React.FC<LibraryPanelProps> = ({ onDragStart }) => {
  const [activeTab, setActiveTab] = useState<'agents' | 'behaviors' | 'bricks' | 'clusters'>('agents');
  const [searchQuery, setSearchQuery] = useState('');
  
  const { data: agents } = useAgents();
  const { data: bricks } = useBricks();
  const { data: clusters } = useClusters();
  
  const filteredItems = filterItems(activeTab, searchQuery, { agents, bricks, clusters });
  
  return (
    <div className="library-panel w-80 bg-slate-900 border-r border-slate-700 flex flex-col h-full">
      <div className="library-header p-4 border-b border-slate-700">
        <h3 className="text-lg font-bold mb-2">Library</h3>
        <input
          type="search"
          placeholder="Search..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="library-search w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded text-white text-sm"
        />
      </div>
      
      <div className="library-tabs flex border-b border-slate-700">
        <button
          className={`flex-1 px-3 py-2 text-sm font-medium transition-colors ${
            activeTab === 'agents'
              ? 'bg-slate-800 border-b-2 border-blue-500 text-white'
              : 'text-slate-400 hover:text-white hover:bg-slate-800'
          }`}
          onClick={() => setActiveTab('agents')}
        >
          📁 Agents
        </button>
        <button
          className={`flex-1 px-3 py-2 text-sm font-medium transition-colors ${
            activeTab === 'bricks'
              ? 'bg-slate-800 border-b-2 border-blue-500 text-white'
              : 'text-slate-400 hover:text-white hover:bg-slate-800'
          }`}
          onClick={() => setActiveTab('bricks')}
        >
          🧱 Bricks
        </button>
        <button
          className={`flex-1 px-3 py-2 text-sm font-medium transition-colors ${
            activeTab === 'clusters'
              ? 'bg-slate-800 border-b-2 border-blue-500 text-white'
              : 'text-slate-400 hover:text-white hover:bg-slate-800'
          }`}
          onClick={() => setActiveTab('clusters')}
        >
          📦 Clusters
        </button>
      </div>
      
      <div className="library-items flex-1 overflow-y-auto p-4 space-y-2">
        {filteredItems.map(item => (
          <LibraryItem
            key={item.id}
            item={item}
            type={activeTab}
            onDragStart={() => onDragStart(activeTab, item.id)}
          />
        ))}
      </div>
      
      <div className="library-footer p-4 border-t border-slate-700 space-y-2">
        <button className="create-button w-full px-3 py-2 bg-blue-600 hover:bg-blue-700 rounded text-sm font-medium">
          + New Brick
        </button>
        <button className="create-button w-full px-3 py-2 bg-blue-600 hover:bg-blue-700 rounded text-sm font-medium">
          + New Cluster
        </button>
      </div>
    </div>
  );
};

interface LibraryItemProps {
  item: any;
  type: string;
  onDragStart: () => void;
}

const LibraryItem: React.FC<LibraryItemProps> = ({ item, type, onDragStart }) => {
  const handleDragStart = (e: React.DragEvent) => {
    e.dataTransfer.setData('application/nexo-node-type', type === 'agents' ? 'agent' : type.slice(0, -1));
    e.dataTransfer.setData('application/nexo-item-id', item.id);
    e.dataTransfer.effectAllowed = 'move';
    onDragStart();
  };
  
  const hasDeterministic = item.hasDeterministic ?? item.implementations?.deterministic ?? true;
  const hasAgentic = item.hasAgentic ?? item.implementations?.agentic ?? true;
  
  return (
    <div
      className="library-item p-3 bg-slate-800 rounded border border-slate-700 cursor-grab active:cursor-grabbing hover:bg-slate-750 hover:border-slate-600 transition-colors"
      draggable
      onDragStart={handleDragStart}
    >
      <div className="flex items-start gap-3">
        <span className="item-icon text-2xl">{item.icon || '📦'}</span>
        <div className="item-info flex-1 min-w-0">
          <div className="item-name font-medium text-sm text-white truncate">{item.name}</div>
          <div className="item-description text-xs text-slate-400 line-clamp-2 mt-1">
            {item.description || 'No description'}
          </div>
        </div>
        <div className="item-implementations flex gap-1">
          {hasDeterministic && (
            <span className="impl-indicator text-xs" title="Has deterministic implementation">
              ⚙️
            </span>
          )}
          {hasAgentic && (
            <span className="impl-indicator text-xs" title="Has agentic implementation">
              🤖
            </span>
          )}
        </div>
      </div>
    </div>
  );
};

function filterItems(
  tab: string,
  query: string,
  data: { agents?: any[]; bricks?: any[]; clusters?: any[] }
): any[] {
  const items = tab === 'agents' ? data.agents || []
    : tab === 'bricks' ? data.bricks || []
    : tab === 'clusters' ? data.clusters || []
    : [];
  
  if (!query) return items;
  
  const lowerQuery = query.toLowerCase();
  return items.filter(item =>
    item.name?.toLowerCase().includes(lowerQuery) ||
    item.description?.toLowerCase().includes(lowerQuery) ||
    item.id?.toLowerCase().includes(lowerQuery)
  );
}
