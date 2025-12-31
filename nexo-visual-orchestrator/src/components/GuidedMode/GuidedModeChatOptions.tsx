// src/components/GuidedMode/GuidedModeChatOptions.tsx

/**
 * GuidedModeChatOptions Component
 * 
 * Renders interactive option buttons for the guided mode chat. Displays
 * a list of selectable options with labels, descriptions, and optional icons.
 * Used for user input during the guided workflow setup process.
 */

import { HiChevronRight } from 'react-icons/hi';

interface ChatOption {
  id: string;
  label: string;
  description?: string;
  icon?: React.ReactNode;
}

interface GuidedModeChatOptionsProps {
  options: ChatOption[];
  onSelect: (optionId: string) => void;
}

export default function GuidedModeChatOptions({
  options,
  onSelect,
}: GuidedModeChatOptionsProps) {
  return (
    <div className="space-y-2 mt-2">
      {options.map(option => (
        <button
          key={option.id}
          onClick={() => onSelect(option.id)}
          className="w-full flex items-center gap-3 p-3 bg-slate-800 hover:bg-slate-700 border border-slate-700 hover:border-purple-500/50 rounded-lg transition-all text-left"
        >
          {option.icon && <div className="flex-shrink-0">{option.icon}</div>}
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-white">{option.label}</p>
            {option.description && (
              <p className="text-xs text-slate-400 mt-0.5">{option.description}</p>
            )}
          </div>
          <HiChevronRight className="w-5 h-5 text-slate-500 flex-shrink-0" />
        </button>
      ))}
    </div>
  );
}

