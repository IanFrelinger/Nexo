import { useEffect, useRef, useState } from 'react';
import { useExecutionStore } from '../../stores/executionStore';
import { useOrchestrationStore } from '../../stores/orchestrationStore';

export default function ExecutionConsole() {
  const { logs, status, clearLogs } = useExecutionStore();
  const setSelectedRole = useOrchestrationStore((s) => s.setSelectedRole);
  const [filter, setFilter] = useState<'all' | 'info' | 'warn' | 'error'>('all');
  const consoleRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (consoleRef.current) {
      consoleRef.current.scrollTop = consoleRef.current.scrollHeight;
    }
  }, [logs]);

  const filteredLogs = logs.filter((log) => filter === 'all' || log.level === filter);

  const levelColors = {
    debug: 'text-slate-500',
    info: 'text-blue-400',
    warn: 'text-yellow-400',
    error: 'text-red-400',
  };

  const formatTime = (timestamp: number) => {
    return new Date(timestamp).toLocaleTimeString('en-US', {
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  };

  return (
    <div className="h-48 bg-surface border-t border-slate-700 flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between px-3 py-2 border-b border-slate-700">
        <div className="flex items-center gap-3">
          <h3 className="font-semibold text-sm">Console</h3>
          <select
            value={filter}
            onChange={(e) => setFilter(e.target.value as typeof filter)}
            className="text-xs bg-surface-dark border border-slate-600 rounded px-2 py-0.5"
          >
            <option value="all">All</option>
            <option value="info">Info</option>
            <option value="warn">Warnings</option>
            <option value="error">Errors</option>
          </select>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={clearLogs}
            className="text-xs text-slate-400 hover:text-slate-200 px-2 py-0.5"
          >
            Clear
          </button>
          <div className="flex items-center gap-1.5">
            <div
              className={`w-2 h-2 rounded-full ${
                status === 'running'
                  ? 'bg-green-500 animate-pulse'
                  : status === 'paused'
                  ? 'bg-yellow-500'
                  : 'bg-slate-500'
              }`}
            />
            <span className="text-xs text-slate-400 capitalize">{status}</span>
          </div>
        </div>
      </div>

      {/* Logs */}
      <div ref={consoleRef} className="flex-1 overflow-y-auto p-2 font-mono text-xs space-y-0.5">
        {filteredLogs.length === 0 ? (
          <p className="text-slate-500">No logs yet. Run a workflow to see output.</p>
        ) : (
          filteredLogs.map((log) => (
            <div
              key={log.id}
              className={`flex gap-2 hover:bg-surface-light rounded px-1 ${
                log.agentId ? 'cursor-pointer' : ''
              }`}
              onClick={() => log.agentId && setSelectedRole(log.agentId)}
            >
              <span className="text-slate-500 shrink-0">{formatTime(log.timestamp)}</span>
              <span className={`shrink-0 uppercase ${levelColors[log.level]}`}>
                [{log.level}]
              </span>
              {log.agentId && (
                <span className="text-purple-400 shrink-0">[{log.agentId}]</span>
              )}
              <span className="text-slate-300">{log.message}</span>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

