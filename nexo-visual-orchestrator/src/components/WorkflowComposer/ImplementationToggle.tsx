import React from 'react';

interface ImplementationToggleProps {
  value: 'Deterministic' | 'Agentic' | 'Auto' | 'Mixed';
  onChange: (value: 'Deterministic' | 'Agentic' | 'Auto' | 'Mixed') => void;
  availableImplementations: {
    deterministic: boolean;
    agentic: boolean;
  };
  showPerBrickOverrides?: boolean;
  bricks?: BrickInfo[];
  brickOverrides?: Record<string, 'Deterministic' | 'Agentic'>;
  onBrickOverrideChange?: (brickId: string, impl: 'Deterministic' | 'Agentic') => void;
}

interface BrickInfo {
  id: string;
  name: string;
  hasDeterministic: boolean;
  hasAgentic: boolean;
  defaultImplementation: 'Deterministic' | 'Agentic';
}

export const ImplementationToggle: React.FC<ImplementationToggleProps> = ({
  value,
  onChange,
  availableImplementations,
  showPerBrickOverrides = false,
  bricks = [],
  brickOverrides = {},
  onBrickOverrideChange,
}) => {
  const modeToSliderValue = (mode: string): number => {
    switch (mode) {
      case 'Deterministic': return 0;
      case 'Auto': return 1;
      case 'Agentic': return 2;
      case 'Mixed': return 1;
      default: return 1;
    }
  };

  const sliderValueToMode = (val: number): 'Deterministic' | 'Agentic' | 'Auto' | 'Mixed' => {
    switch (val) {
      case 0: return 'Deterministic';
      case 2: return 'Agentic';
      default: return 'Auto';
    }
  };

  const calculateDeterministicTime = (bricks: BrickInfo[], overrides: Record<string, 'Deterministic' | 'Agentic'>): number => {
    return bricks
      .filter(b => (overrides[b.id] || b.defaultImplementation) === 'Deterministic')
      .reduce((sum, _) => sum + 50, 0);
  };

  const calculateAgenticTime = (bricks: BrickInfo[], overrides: Record<string, 'Deterministic' | 'Agentic'>): number => {
    return bricks
      .filter(b => (overrides[b.id] || b.defaultImplementation) === 'Agentic')
      .reduce((sum, _) => sum + 2000, 0) / 1000;
  };

  return (
    <div className="implementation-toggle p-4 bg-slate-800 rounded-lg">
      {/* Main slider */}
      <div className="toggle-slider flex items-center gap-3 mb-3">
        <button
          className={`toggle-option px-4 py-2 rounded text-sm font-medium transition-colors ${
            value === 'Deterministic'
              ? 'bg-blue-600 text-white'
              : availableImplementations.deterministic
              ? 'bg-slate-700 text-slate-300 hover:bg-slate-600'
              : 'bg-slate-800 text-slate-500 cursor-not-allowed opacity-50'
          }`}
          onClick={() => onChange('Deterministic')}
          disabled={!availableImplementations.deterministic}
          title="Deterministic: Fast, predictable, auditable"
        >
          ⚙️
        </button>
        
        <input
          type="range"
          min="0"
          max="2"
          value={modeToSliderValue(value)}
          onChange={(e) => onChange(sliderValueToMode(parseInt(e.target.value)))}
          className="slider flex-1 h-2 bg-gradient-to-r from-blue-600 via-slate-600 to-purple-600 rounded-lg appearance-none cursor-pointer"
          style={{
            background: 'linear-gradient(to right, #3b82f6 0%, #6b7280 50%, #8b5cf6 100%)',
          }}
        />
        
        <button
          className={`toggle-option px-4 py-2 rounded text-sm font-medium transition-colors ${
            value === 'Agentic'
              ? 'bg-purple-600 text-white'
              : availableImplementations.agentic
              ? 'bg-slate-700 text-slate-300 hover:bg-slate-600'
              : 'bg-slate-800 text-slate-500 cursor-not-allowed opacity-50'
          }`}
          onClick={() => onChange('Agentic')}
          disabled={!availableImplementations.agentic}
          title="Agentic: Intelligent, contextual, adaptive"
        >
          🤖
        </button>
      </div>
      
      <div className="toggle-label text-sm text-slate-400 mb-4 text-center">
        {value === 'Deterministic' && 'Deterministic (fast, auditable)'}
        {value === 'Agentic' && 'Agentic (intelligent, adaptive)'}
        {value === 'Auto' && 'Auto (system decides)'}
        {value === 'Mixed' && 'Mixed (per-brick settings)'}
      </div>
      
      {/* Per-brick overrides */}
      {showPerBrickOverrides && value === 'Mixed' && bricks.length > 0 && (
        <div className="brick-overrides mt-4 pt-4 border-t border-slate-700">
          <h4 className="text-sm font-semibold mb-3 text-slate-300">Per-Brick Settings</h4>
          <div className="space-y-2">
            {bricks.map(brick => {
              const currentImpl = brickOverrides[brick.id] || brick.defaultImplementation;
              return (
                <div key={brick.id} className="brick-override-row flex items-center gap-3 p-2 bg-slate-900 rounded">
                  <span className="brick-name flex-1 text-sm text-slate-300">{brick.name}</span>
                  
                  <div className="brick-toggle flex gap-1">
                    <button
                      className={`mini-toggle px-2 py-1 rounded text-xs transition-colors ${
                        currentImpl === 'Deterministic'
                          ? 'bg-blue-600 text-white'
                          : brick.hasDeterministic
                          ? 'bg-slate-700 text-slate-300 hover:bg-slate-600'
                          : 'bg-slate-800 text-slate-500 cursor-not-allowed opacity-50'
                      }`}
                      onClick={() => onBrickOverrideChange?.(brick.id, 'Deterministic')}
                      disabled={!brick.hasDeterministic}
                    >
                      ⚙️
                    </button>
                    <button
                      className={`mini-toggle px-2 py-1 rounded text-xs transition-colors ${
                        currentImpl === 'Agentic'
                          ? 'bg-purple-600 text-white'
                          : brick.hasAgentic
                          ? 'bg-slate-700 text-slate-300 hover:bg-slate-600'
                          : 'bg-slate-800 text-slate-500 cursor-not-allowed opacity-50'
                      }`}
                      onClick={() => onBrickOverrideChange?.(brick.id, 'Agentic')}
                      disabled={!brick.hasAgentic}
                    >
                      🤖
                    </button>
                  </div>
                  
                  {!brick.hasDeterministic && (
                    <span className="locked-indicator text-xs text-slate-500" title="Only agentic available">
                      🔒 🤖 only
                    </span>
                  )}
                  {!brick.hasAgentic && (
                    <span className="locked-indicator text-xs text-slate-500" title="Only deterministic available">
                      🔒 ⚙️ only
                    </span>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}
      
      {/* Estimated execution time */}
      {bricks.length > 0 && (
        <div className="execution-estimate mt-4 pt-4 border-t border-slate-700 text-xs text-slate-400">
          <span className="estimate-label font-semibold">Estimated:</span>
          <div className="mt-1">
            <span className="text-blue-400">
              ⚙️ ~{calculateDeterministicTime(bricks, brickOverrides)}ms
            </span>
            {' | '}
            <span className="text-purple-400">
              🤖 ~{calculateAgenticTime(bricks, brickOverrides)}s
            </span>
          </div>
        </div>
      )}
    </div>
  );
};
