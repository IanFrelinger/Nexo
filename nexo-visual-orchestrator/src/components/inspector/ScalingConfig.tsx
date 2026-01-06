import { useState } from 'react';

interface Props {
  cluster: any;
}

export function ScalingConfig({ cluster }: Props) {
  const [mode, setMode] = useState(cluster.scaling.mode);
  const [count, setCount] = useState(cluster.scaling.fixedCount);
  
  return (
    <div className="scaling-tab space-y-4">
      <section>
        <h4 className="font-semibold mb-3">Scaling Mode</h4>
        <div className="scaling-modes grid grid-cols-3 gap-3">
          {[
            { value: 'single', icon: '1️⃣', name: 'Single', desc: 'One instance' },
            { value: 'fixed', icon: '📊', name: 'Fixed', desc: 'Set number' },
            { value: 'dynamic', icon: '📈', name: 'Dynamic', desc: 'Calculate at runtime' },
          ].map(m => (
            <label
              key={m.value}
              className={`p-3 border-2 rounded cursor-pointer ${
                mode === m.value ? 'border-blue-500 bg-slate-800' : 'border-slate-700 hover:border-slate-600'
              }`}
            >
              <input
                type="radio"
                name="scalingMode"
                value={m.value}
                checked={mode === m.value}
                onChange={() => setMode(m.value)}
                className="hidden"
              />
              <div className="text-center">
                <div className="text-2xl mb-1">{m.icon}</div>
                <div className="font-medium">{m.name}</div>
                <div className="text-xs text-slate-400">{m.desc}</div>
              </div>
            </label>
          ))}
        </div>
      </section>
      
      {mode === 'fixed' && (
        <section>
          <h4 className="font-semibold mb-2">Instance Count</h4>
          <input
            type="range"
            min={1}
            max={cluster.scaling.maxInstances}
            value={count}
            onChange={e => setCount(parseInt(e.target.value))}
            className="w-full"
          />
          <span className="text-sm text-slate-400">{count} instances</span>
        </section>
      )}
      
      {mode === 'dynamic' && (
        <section>
          <h4 className="font-semibold mb-2">Scaling Expression</h4>
          <input
            type="text"
            placeholder="levelSize / 100"
            defaultValue={cluster.scaling.dynamicExpression}
            className="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded text-white"
          />
          <p className="text-xs text-slate-400 mt-1">
            Available variables: levelSize, playerCount, config.*
          </p>
        </section>
      )}
    </div>
  );
}

