import { useState, useEffect } from 'react';
import OrchestrationCanvas from './components/Canvas/OrchestrationCanvas';
import AgentLibrary from './components/Panels/AgentLibrary';
import PropertiesPanel from './components/Panels/PropertiesPanel';
import ExecutionConsole from './components/Panels/ExecutionConsole';
import MainToolbar from './components/Toolbar/MainToolbar';

export default function App() {
  const [showLibrary, setShowLibrary] = useState(true);
  const [showProperties, setShowProperties] = useState(true);
  const [showConsole, setShowConsole] = useState(true);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ctrl/Cmd + S = Save
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        // Save functionality is handled in MainToolbar
      }
      // Ctrl/Cmd + O = Open
      if ((e.ctrlKey || e.metaKey) && e.key === 'o') {
        e.preventDefault();
        // Open functionality is handled in MainToolbar
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  return (
    <div className="h-screen flex flex-col bg-surface-dark">
      <MainToolbar />

      <div className="flex-1 flex overflow-hidden">
        {/* Left Panel - Agent Library */}
        {showLibrary && <AgentLibrary onCollapse={() => setShowLibrary(false)} />}

        {/* Center - Canvas */}
        <div className="flex-1 flex flex-col">
          <OrchestrationCanvas />
          {showConsole && <ExecutionConsole />}
        </div>

        {/* Right Panel - Properties */}
        {showProperties && <PropertiesPanel />}
      </div>

      {/* Panel toggle buttons (bottom left) */}
      <div className="absolute bottom-52 left-2 flex flex-col gap-1">
        {!showLibrary && (
          <button
            onClick={() => setShowLibrary(true)}
            className="p-2 bg-surface border border-slate-700 rounded text-xs hover:bg-surface-light"
            title="Show Agent Library"
          >
            Library
          </button>
        )}
        {!showConsole && (
          <button
            onClick={() => setShowConsole(true)}
            className="p-2 bg-surface border border-slate-700 rounded text-xs hover:bg-surface-light"
            title="Show Console"
          >
            Console
          </button>
        )}
      </div>

      {/* Properties toggle (bottom right) */}
      {!showProperties && (
        <div className="absolute bottom-52 right-2">
          <button
            onClick={() => setShowProperties(true)}
            className="p-2 bg-surface border border-slate-700 rounded text-xs hover:bg-surface-light"
            title="Show Properties"
          >
            Props
          </button>
        </div>
      )}
    </div>
  );
}
