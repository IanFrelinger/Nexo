import type { Brick } from '../../api/client';

interface Props {
  brick: Brick;
}

export function BrickCard({ brick }: Props) {
  return (
    <div className="brick-card bg-slate-800 rounded-lg p-4 hover:bg-slate-700 cursor-pointer transition-colors">
      <div className="brick-card-header flex items-center gap-2 mb-2">
        <span className="text-2xl">{brick.icon}</span>
        <span className="font-semibold">{brick.name}</span>
      </div>
      
      <p className="text-sm text-slate-400 mb-3">{brick.description}</p>
      
      <div className="brick-meta flex items-center gap-4 text-xs text-slate-500">
        <span className="category">{brick.category}</span>
        <div className="implementations flex gap-1">
          {brick.implementations.deterministic && (
            <span title="Deterministic">⚙️</span>
          )}
          {brick.implementations.agentic && (
            <span title="Agentic">🤖</span>
          )}
        </div>
      </div>
    </div>
  );
}

