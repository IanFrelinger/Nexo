interface Props {
  current: string;
  hasDeterministic: boolean;
  hasAgentic: boolean;
  onChange: (impl: 'deterministic' | 'agentic') => void;
}

export function ImplementationToggle({ current, hasDeterministic, hasAgentic, onChange }: Props) {
  return (
    <div className="implementation-toggle flex gap-2">
      {hasDeterministic && (
        <button
          className={`px-3 py-1 rounded text-sm ${
            current === 'deterministic'
              ? 'bg-blue-600 text-white'
              : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
          }`}
          onClick={() => onChange('deterministic')}
        >
          ⚙️ Deterministic
        </button>
      )}
      {hasAgentic && (
        <button
          className={`px-3 py-1 rounded text-sm ${
            current === 'agentic'
              ? 'bg-blue-600 text-white'
              : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
          }`}
          onClick={() => onChange('agentic')}
        >
          🤖 Agentic
        </button>
      )}
    </div>
  );
}

