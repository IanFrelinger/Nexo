/**
 * MainToolbar Component
 * 
 * Top-level toolbar component that orchestrates three main sections:
 * - FileOperations: Open, save, clear, guided setup, deck builder
 * - ExecutionControls: Run, pause, stop workflow execution
 * - ViewControls: Auto-layout and view management
 */

import FileOperations from './FileOperations';
import ExecutionControls from './ExecutionControls';
import ViewControls from './ViewControls';

interface MainToolbarProps {
  /** Callback to open the guided setup mode */
  onShowGuidedMode?: () => void;
  /** Callback to open the deck builder */
  onShowDeckBuilder?: () => void;
  /** Callback to open the workflow composer */
  onShowWorkflowComposer?: () => void;
}

/**
 * MainToolbar - Main application toolbar
 * @param props - Component props
 * @returns JSX element
 */
export default function MainToolbar({ onShowGuidedMode, onShowDeckBuilder, onShowWorkflowComposer }: MainToolbarProps) {
  return (
    <div className="h-12 bg-surface border-b border-slate-700 flex items-center justify-between px-4">
      <FileOperations 
        onShowGuidedMode={onShowGuidedMode}
        onShowDeckBuilder={onShowDeckBuilder}
        onShowWorkflowComposer={onShowWorkflowComposer}
      />
      <ExecutionControls />
      <ViewControls />
    </div>
  );
}

