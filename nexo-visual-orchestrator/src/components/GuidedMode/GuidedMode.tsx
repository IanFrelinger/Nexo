import GuidedModeChat from './GuidedModeChat';
import type { RoleDefinition, Relationship } from '../../types/workflow';

interface GuidedModeProps {
  onComplete: (workflow: { roles: RoleDefinition[]; relationships: Relationship[] }) => void;
  onSkip: () => void;
}

export default function GuidedMode({ onComplete, onSkip }: GuidedModeProps) {
  return (
    <div className="absolute inset-0 bg-surface-dark z-50 flex items-center justify-center">
      <div className="w-full max-w-2xl h-[80vh] bg-surface border border-slate-700 rounded-lg shadow-2xl overflow-hidden">
        <GuidedModeChat onComplete={onComplete} onSkip={onSkip} />
      </div>
    </div>
  );
}

