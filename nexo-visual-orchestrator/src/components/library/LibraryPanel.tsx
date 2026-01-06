import { useState } from 'react';
import { useBricks, useClusters } from '../../hooks/useCatalog';
import { useClusters as useClustersHook } from '../../hooks/useClusters';
import { BrickCard } from './BrickCard';
import { ClusterCard } from './ClusterCard';

type LibraryTab = 'cards' | 'combos' | 'decks';

export function LibraryPanel() {
  const [tab, setTab] = useState<LibraryTab>('cards');
  const [search, setSearch] = useState('');
  
  const { data: bricks } = useBricks();
  const { data: clusters } = useClustersHook();
  
  const filteredBricks = bricks?.filter(b => 
    b.name.toLowerCase().includes(search.toLowerCase()) ||
    b.description.toLowerCase().includes(search.toLowerCase())
  );
  
  const filteredClusters = clusters?.filter(c =>
    c.name.toLowerCase().includes(search.toLowerCase()) ||
    c.tags.some(t => t.toLowerCase().includes(search.toLowerCase()))
  );
  
  const combos = filteredClusters?.filter(c => c.brickCount < 10) ?? [];
  const decks = filteredClusters?.filter(c => c.brickCount >= 10) ?? [];
  
  return (
    <div className="library-panel flex flex-col h-full bg-slate-900 text-white">
      <div className="library-header p-4 border-b border-slate-700 flex justify-between items-center">
        <h2 className="text-xl font-bold">Library</h2>
        <button 
          className="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded"
          onClick={() => {/* TODO: Open create modal */}}
        >
          + Create
        </button>
      </div>
      
      <input
        type="search"
        placeholder="Search..."
        value={search}
        onChange={e => setSearch(e.target.value)}
        className="library-search m-4 px-3 py-2 bg-slate-800 border border-slate-700 rounded text-white"
      />
      
      <div className="library-tabs flex border-b border-slate-700">
        <button 
          className={`flex-1 px-4 py-2 ${tab === 'cards' ? 'bg-slate-800 border-b-2 border-blue-500' : 'hover:bg-slate-800'}`}
          onClick={() => setTab('cards')}
        >
          Cards ({bricks?.length ?? 0})
        </button>
        <button 
          className={`flex-1 px-4 py-2 ${tab === 'combos' ? 'bg-slate-800 border-b-2 border-blue-500' : 'hover:bg-slate-800'}`}
          onClick={() => setTab('combos')}
        >
          Combos ({combos.length})
        </button>
        <button 
          className={`flex-1 px-4 py-2 ${tab === 'decks' ? 'bg-slate-800 border-b-2 border-blue-500' : 'hover:bg-slate-800'}`}
          onClick={() => setTab('decks')}
        >
          Decks ({decks.length})
        </button>
      </div>
      
      <div className="library-content flex-1 overflow-y-auto p-4">
        {tab === 'cards' && (
          <div className="library-grid grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {filteredBricks?.map(brick => (
              <BrickCard key={brick.id} brick={brick} />
            ))}
          </div>
        )}
        
        {tab === 'combos' && (
          <div className="library-grid grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {combos.map(cluster => (
              <ClusterCard key={cluster.id} cluster={cluster} />
            ))}
          </div>
        )}
        
        {tab === 'decks' && (
          <div className="library-grid grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {decks.map(cluster => (
              <ClusterCard key={cluster.id} cluster={cluster} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

