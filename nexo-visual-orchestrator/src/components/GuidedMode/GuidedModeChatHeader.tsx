// src/components/GuidedMode/GuidedModeChatHeader.tsx

import { HiChat, HiX } from 'react-icons/hi';

interface GuidedModeChatHeaderProps {
  onSkip: () => void;
}

export default function GuidedModeChatHeader({ onSkip }: GuidedModeChatHeaderProps) {
  return (
    <div className="flex items-center justify-between px-4 py-3 border-b border-slate-700">
      <div className="flex items-center gap-2">
        <HiChat className="w-5 h-5 text-purple-400" />
        <h2 className="font-semibold text-white">Team Setup</h2>
      </div>
      <button
        onClick={onSkip}
        className="p-1 hover:bg-slate-700 rounded text-slate-400 hover:text-white"
      >
        <HiX className="w-5 h-5" />
      </button>
    </div>
  );
}

