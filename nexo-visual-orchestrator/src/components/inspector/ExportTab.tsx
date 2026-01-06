import { useState } from 'react';
import { api, type ExportRequest } from '../../api/client';

interface Props {
  cluster: any;
}

export function ExportTab({ cluster }: Props) {
  const [mode, setMode] = useState<'deterministic' | 'generated' | 'runtime'>('deterministic');
  const [target, setTarget] = useState<'csharp' | 'python' | 'typescript'>('csharp');
  const [isExporting, setIsExporting] = useState(false);
  
  const hasAgenticBricks = cluster.bricks.some((b: any) => b.hasAgentic);
  
  const handleExport = async () => {
    setIsExporting(true);
    try {
      const request: ExportRequest = {
        workflow: {
          name: cluster.name,
          description: cluster.description,
          instances: [],
          connections: [],
        },
        mode: mode === 'deterministic' ? 'PureDeterministic' : 
              mode === 'generated' ? 'AIGeneratedThenDeterministic' : 
              'WithRuntimeAI',
        target: target === 'csharp' ? 'CSharp' : 
                target === 'python' ? 'Python' : 
                'TypeScript',
        includeFallbacks: true,
        outputFormat: 'Zip',
      };
      
      const blob = await api.downloadExport(request);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${cluster.name}-export.zip`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Export failed:', error);
    } finally {
      setIsExporting(false);
    }
  };
  
  return (
    <div className="export-tab space-y-4">
      <section>
        <h4 className="font-semibold mb-3">Export Mode</h4>
        <div className="export-modes space-y-2">
          <label className={`block p-3 border-2 rounded cursor-pointer ${
            mode === 'deterministic' ? 'border-blue-500 bg-slate-800' : 'border-slate-700 hover:border-slate-600'
          }`}>
            <input
              type="radio"
              name="exportMode"
              value="deterministic"
              checked={mode === 'deterministic'}
              onChange={() => setMode('deterministic')}
              className="hidden"
            />
            <div className="flex items-center gap-3">
              <span className="text-2xl">⚙️</span>
              <div>
                <div className="font-medium">Pure Deterministic</div>
                <div className="text-xs text-slate-400">No AI dependencies. Just clean code.</div>
              </div>
            </div>
          </label>
          
          {hasAgenticBricks && (
            <>
              <label className={`block p-3 border-2 rounded cursor-pointer ${
                mode === 'generated' ? 'border-blue-500 bg-slate-800' : 'border-slate-700 hover:border-slate-600'
              }`}>
                <input
                  type="radio"
                  name="exportMode"
                  value="generated"
                  checked={mode === 'generated'}
                  onChange={() => setMode('generated')}
                  className="hidden"
                />
                <div className="flex items-center gap-3">
                  <span className="text-2xl">🔄</span>
                  <div>
                    <div className="font-medium">Generate → Deterministic</div>
                    <div className="text-xs text-slate-400">AI creates content now. Export as pure code.</div>
                  </div>
                </div>
              </label>
              
              <label className={`block p-3 border-2 rounded cursor-pointer ${
                mode === 'runtime' ? 'border-blue-500 bg-slate-800' : 'border-slate-700 hover:border-slate-600'
              }`}>
                <input
                  type="radio"
                  name="exportMode"
                  value="runtime"
                  checked={mode === 'runtime'}
                  onChange={() => setMode('runtime')}
                  className="hidden"
                />
                <div className="flex items-center gap-3">
                  <span className="text-2xl">🤖</span>
                  <div>
                    <div className="font-medium">With Runtime AI</div>
                    <div className="text-xs text-slate-400">Include Nexo runtime for live AI features.</div>
                  </div>
                </div>
              </label>
            </>
          )}
        </div>
      </section>
      
      <section>
        <h4 className="font-semibold mb-2">Target Language</h4>
        <div className="target-selector flex gap-2">
          {(['csharp', 'python', 'typescript'] as const).map(t => (
            <button
              key={t}
              className={`px-4 py-2 rounded ${
                target === t ? 'bg-blue-600 text-white' : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
              }`}
              onClick={() => setTarget(t)}
            >
              {t === 'csharp' ? 'C#' : t.charAt(0).toUpperCase() + t.slice(1)}
            </button>
          ))}
        </div>
      </section>
      
      <section className="export-preview p-4 bg-slate-800 rounded">
        <h4 className="font-semibold mb-3">Export Summary</h4>
        <div className="summary-card space-y-2 text-sm">
          <div className="summary-item flex justify-between">
            <span className="text-slate-400">Mode</span>
            <span className="font-medium">{mode}</span>
          </div>
          <div className="summary-item flex justify-between">
            <span className="text-slate-400">Target</span>
            <span className="font-medium">{target}</span>
          </div>
          <div className="summary-item flex justify-between">
            <span className="text-slate-400">Bricks</span>
            <span className="font-medium">{cluster.bricks.length}</span>
          </div>
          <div className="summary-item flex justify-between">
            <span className="text-slate-400">Runtime Requirements</span>
            <span className="font-medium">
              {mode === 'runtime' ? 'Nexo.Runtime, Ollama (optional)' : 'None'}
            </span>
          </div>
        </div>
      </section>
      
      <div className="export-actions flex gap-2">
        <button
          className="export-button flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded font-medium disabled:opacity-50"
          onClick={handleExport}
          disabled={isExporting}
        >
          {isExporting ? 'Exporting...' : 'Export'}
        </button>
        <button
          className="export-button px-4 py-2 bg-slate-700 hover:bg-slate-600 rounded"
          onClick={handleExport}
          disabled={isExporting}
        >
          Download .zip
        </button>
      </div>
    </div>
  );
}

