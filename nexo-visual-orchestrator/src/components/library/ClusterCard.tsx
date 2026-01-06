import type { Cluster } from '../../api/client';

interface Props {
  cluster: Cluster;
}

export function ClusterCard({ cluster }: Props) {
  return (
    <div className="cluster-card bg-slate-800 rounded-lg p-4 hover:bg-slate-700 cursor-pointer transition-colors">
      <div className="cluster-card-header flex items-center gap-2 mb-2">
        <span className="text-2xl">{cluster.icon}</span>
        <span className="font-semibold">{cluster.name}</span>
      </div>
      
      <p className="text-sm text-slate-400 mb-3">{cluster.description}</p>
      
      <div className="cluster-meta flex items-center justify-between text-xs text-slate-500 mb-2">
        <span className="brick-count">{cluster.brickCount} cards</span>
        <span className="author">by {cluster.author}</span>
      </div>
      
      {cluster.tags.length > 0 && (
        <div className="cluster-tags flex flex-wrap gap-1 mb-2">
          {cluster.tags.map(tag => (
            <span key={tag} className="tag px-2 py-1 bg-slate-700 rounded text-xs">
              {tag}
            </span>
          ))}
        </div>
      )}
      
      <div className="cluster-stats flex items-center gap-4 text-xs text-slate-500">
        <span>👁 {cluster.usageCount}</span>
        <span className="scope">{cluster.scope}</span>
      </div>
    </div>
  );
}

