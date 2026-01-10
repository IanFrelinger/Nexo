import { useState, useEffect } from 'react';
import CardCanvas from './components/Canvas/CardCanvas';
import AgentLibrary from './components/Panels/AgentLibrary';
import CustomAgentLibrary from './components/Panels/CustomAgentLibrary';
import PropertiesPanel from './components/Panels/PropertiesPanel';
import ExecutionConsole from './components/Panels/ExecutionConsole';
import ViewControls from './components/Panels/ViewControls';
import MainToolbar from './components/Toolbar/MainToolbar';
import GuidedMode from './components/GuidedMode/GuidedMode';
import DeckBuilder from './components/DeckBuilder/DeckBuilder';
import { WorkflowComposer } from './components/WorkflowComposer/WorkflowComposer';
import { HiBookOpen, HiTerminal, HiCog, HiViewGrid } from 'react-icons/hi';
import { useOrchestrationStore } from './stores/orchestrationStore';
import { useDeckStore } from './stores/deckStore';
import { deckToWorkflow } from './types/deck';
import { ROLE_TEMPLATES } from './data/roleTemplates';
import type { RoleDefinition, Relationship } from './types/workflow';

export default function App() {
  const [showLibrary, setShowLibrary] = useState(true);
  const [showProperties, setShowProperties] = useState(true);
  const [showConsole, setShowConsole] = useState(true);
  const [showViewControls, setShowViewControls] = useState(true);
  const [showGuidedMode, setShowGuidedMode] = useState(false);
  const [showDeckBuilder, setShowDeckBuilder] = useState(false);
  const [showCustomAgentLibrary, setShowCustomAgentLibrary] = useState(false);
  const [showWorkflowComposer, setShowWorkflowComposer] = useState(false);
  const { roles, loadWorkflow } = useOrchestrationStore();
  const { loadDeck } = useDeckStore();

  // Check if we should show guided mode on mount
  useEffect(() => {
    // Show guided mode if canvas is empty and user hasn't dismissed it
    const hasDismissedGuidedMode = localStorage.getItem('nexo-guided-mode-dismissed') === 'true';
    if (roles.length === 0 && !hasDismissedGuidedMode) {
      setShowGuidedMode(true);
    }
  }, [roles.length]);

  const handleGuidedModeComplete = (workflow: { roles: RoleDefinition[]; relationships: Relationship[] }) => {
    loadWorkflow(workflow.roles, workflow.relationships);
    setShowGuidedMode(false);
    localStorage.setItem('nexo-guided-mode-dismissed', 'true');
  };

  const handleGuidedModeSkip = () => {
    setShowGuidedMode(false);
    localStorage.setItem('nexo-guided-mode-dismissed', 'true');
  };

  const handleShowGuidedMode = () => {
    setShowGuidedMode(true);
  };

  const handleShowDeckBuilder = () => {
    setShowDeckBuilder(true);
  };

  const handleDeckLoad = (deckId: string) => {
    const deck = loadDeck(deckId);
    if (deck) {
      const { roles: deckRoles, relationships: deckRelationships } = deckToWorkflow(deck, ROLE_TEMPLATES);
      loadWorkflow(deckRoles, deckRelationships);
      setShowDeckBuilder(false);
    }
  };

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
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, []);

  // Show workflow composer if enabled
  if (showWorkflowComposer) {
    return (
      <div className="h-screen">
        <div className="absolute top-2 left-2 z-50">
          <button
            onClick={() => setShowWorkflowComposer(false)}
            className="px-4 py-2 bg-slate-700 hover:bg-slate-600 rounded text-white text-sm"
          >
            ← Back to Orchestrator
          </button>
        </div>
        <WorkflowComposer />
      </div>
    );
  }

  return (
    <div className="h-screen flex flex-col bg-surface-dark">
      {showGuidedMode && (
        <GuidedMode onComplete={handleGuidedModeComplete} onSkip={handleGuidedModeSkip} />
      )}
      {showDeckBuilder && (
        <DeckBuilder
          onClose={() => setShowDeckBuilder(false)}
          onDeckLoad={handleDeckLoad}
        />
      )}
      <MainToolbar
        onShowGuidedMode={roles.length === 0 ? handleShowGuidedMode : undefined}
        onShowDeckBuilder={handleShowDeckBuilder}
        onShowWorkflowComposer={() => setShowWorkflowComposer(true)}
      />

      <div className="flex-1 flex overflow-hidden">
        {/* Left Panel - Agent Library or Custom Agent Library */}
        {showCustomAgentLibrary ? (
          <CustomAgentLibrary 
            onCollapse={() => setShowCustomAgentLibrary(false)}
            onSelectAgent={(agentId) => {
              // Handle agent selection - could add to canvas or deck
              console.log('Selected custom agent:', agentId);
            }}
          />
        ) : (
          showLibrary && (
            <AgentLibrary 
              onCollapse={() => setShowLibrary(false)}
              onShowCustomLibrary={() => {
                setShowCustomAgentLibrary(true);
                setShowLibrary(false);
              }}
            />
          )
        )}

        {/* Center - Canvas */}
        <div className="flex-1 flex flex-col">
          <CardCanvas />
          {showConsole && <ExecutionConsole />}
        </div>

        {/* Right Panel - Properties */}
        {showProperties && <PropertiesPanel />}
      </div>

      {/* View Controls (top right, floating) */}
      {showViewControls && (
        <div className="absolute top-16 right-4 z-50">
          <ViewControls onCollapse={() => setShowViewControls(false)} />
        </div>
      )}

      {/* Panel toggle buttons (bottom left) */}
      <div className="absolute bottom-52 left-2 flex flex-col gap-1">
        {!showLibrary && (
          <button
            onClick={() => setShowLibrary(true)}
            className="p-2 bg-surface border border-slate-700 rounded text-xs hover:bg-surface-light flex items-center gap-1"
            title="Show Agent Library"
          >
            <HiBookOpen className="w-4 h-4" />
            <span>Library</span>
          </button>
        )}
        {!showConsole && (
          <button
            onClick={() => setShowConsole(true)}
            className="p-2 bg-surface border border-slate-700 rounded text-xs hover:bg-surface-light flex items-center gap-1"
            title="Show Console"
          >
            <HiTerminal className="w-4 h-4" />
            <span>Console</span>
          </button>
        )}
      </div>

      {/* Properties toggle (bottom right) */}
      {!showProperties && (
        <div className="absolute bottom-52 right-2">
          <button
            onClick={() => setShowProperties(true)}
            className="p-2 bg-surface border border-slate-700 rounded text-xs hover:bg-surface-light flex items-center gap-1"
            title="Show Properties"
          >
            <HiCog className="w-4 h-4" />
            <span>Props</span>
          </button>
        </div>
      )}
      {!showViewControls && (
        <div className="absolute top-16 right-2">
          <button
            onClick={() => setShowViewControls(true)}
            className="p-2 bg-surface border border-slate-700 rounded text-xs hover:bg-surface-light flex items-center gap-1"
            title="Show View Controls"
          >
            <HiViewGrid className="w-4 h-4" />
            <span>View</span>
          </button>
        </div>
      )}
    </div>
  );
}
