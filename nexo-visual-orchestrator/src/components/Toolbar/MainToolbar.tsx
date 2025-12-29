import FileOperations from './FileOperations';
import ExecutionControls from './ExecutionControls';
import ViewControls from './ViewControls';

interface MainToolbarProps {
  onShowGuidedMode?: () => void;
  onShowDeckBuilder?: () => void;
}

export default function MainToolbar({ onShowGuidedMode, onShowDeckBuilder }: MainToolbarProps) {
  return (
    <div className="h-12 bg-surface border-b border-slate-700 flex items-center justify-between px-4">
      <FileOperations 
        onShowGuidedMode={onShowGuidedMode}
        onShowDeckBuilder={onShowDeckBuilder}
      />
      <ExecutionControls />
      <ViewControls />
    </div>
  );
}

