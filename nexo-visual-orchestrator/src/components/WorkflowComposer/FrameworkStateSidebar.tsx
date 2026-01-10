import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Activity, Shield, Zap, Database, ChevronRight } from 'lucide-react';

interface CircuitBreaker {
  name: string;
  state: 'closed' | 'open' | 'half-open';
  failures: number;
  threshold: number;
}

interface RateLimit {
  name: string;
  current: number;
  max: number;
}

interface FrameworkMetrics {
  deterministicTime: number;
  agenticTime: number;
  deterministicCount: number;
  agenticCount: number;
  cacheHits: number;
  cacheMisses: number;
  estimatedSavings: number;
}

interface Provider {
  name: string;
  status: 'active' | 'standby' | 'unavailable';
  latency?: number;
  calls: number;
  cost: number;
}

interface FrameworkState {
  circuitBreakers: CircuitBreaker[];
  rateLimits: RateLimit[];
  metrics: FrameworkMetrics;
  providers: Provider[];
}

interface FrameworkStateSidebarProps {
  state: FrameworkState;
  isOpen: boolean;
  onToggle: () => void;
}

export const FrameworkStateSidebar: React.FC<FrameworkStateSidebarProps> = ({
  state,
  isOpen,
  onToggle,
}) => {
  const [activeTab, setActiveTab] = useState<'resilience' | 'metrics' | 'providers'>('resilience');

  return (
    <>
      {/* Toggle button */}
      <button
        className="sidebar-toggle fixed right-4 top-4 z-50 bg-slate-800 hover:bg-slate-700 text-white p-2 rounded-lg shadow-lg flex items-center gap-2 border border-slate-700"
        onClick={onToggle}
        title={isOpen ? 'Hide framework state' : 'Show framework state'}
      >
        <Activity size={20} />
        <ChevronRight
          size={16}
          className={`transition-transform ${isOpen ? 'rotate-180' : ''}`}
        />
      </button>

      <AnimatePresence>
        {isOpen && (
          <motion.div
            className="framework-sidebar fixed right-0 top-0 h-full w-[280px] bg-slate-900 border-l border-slate-700 z-40 flex flex-col shadow-2xl"
            initial={{ x: 280, opacity: 0 }}
            animate={{ x: 0, opacity: 1 }}
            exit={{ x: 280, opacity: 0 }}
            transition={{ duration: 0.2 }}
          >
            <div className="sidebar-header p-4 border-b border-slate-700 flex items-center justify-between">
              <h3 className="text-lg font-bold text-white">Framework State</h3>
              <span className="live-indicator text-xs text-green-400 flex items-center gap-1">
                <span className="w-2 h-2 bg-green-400 rounded-full animate-pulse" />
                LIVE
              </span>
            </div>

            <div className="sidebar-tabs flex border-b border-slate-700">
              <button
                className={`flex-1 px-3 py-2 text-sm font-medium transition-colors ${
                  activeTab === 'resilience'
                    ? 'bg-slate-800 text-blue-400 border-b-2 border-blue-400'
                    : 'text-slate-400 hover:text-white hover:bg-slate-800'
                }`}
                onClick={() => setActiveTab('resilience')}
              >
                <Shield size={14} className="inline mr-1" />
                Resilience
              </button>
              <button
                className={`flex-1 px-3 py-2 text-sm font-medium transition-colors ${
                  activeTab === 'metrics'
                    ? 'bg-slate-800 text-blue-400 border-b-2 border-blue-400'
                    : 'text-slate-400 hover:text-white hover:bg-slate-800'
                }`}
                onClick={() => setActiveTab('metrics')}
              >
                <Zap size={14} className="inline mr-1" />
                Metrics
              </button>
              <button
                className={`flex-1 px-3 py-2 text-sm font-medium transition-colors ${
                  activeTab === 'providers'
                    ? 'bg-slate-800 text-blue-400 border-b-2 border-blue-400'
                    : 'text-slate-400 hover:text-white hover:bg-slate-800'
                }`}
                onClick={() => setActiveTab('providers')}
              >
                <Database size={14} className="inline mr-1" />
                Providers
              </button>
            </div>

            <div className="sidebar-content flex-1 overflow-y-auto p-4">
              {activeTab === 'resilience' && (
                <ResiliencePanel
                  circuitBreakers={state.circuitBreakers}
                  rateLimits={state.rateLimits}
                />
              )}
              {activeTab === 'metrics' && (
                <MetricsPanel metrics={state.metrics} />
              )}
              {activeTab === 'providers' && (
                <ProvidersPanel providers={state.providers} />
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </>
  );
};

const ResiliencePanel: React.FC<{
  circuitBreakers: CircuitBreaker[];
  rateLimits: RateLimit[];
}> = ({ circuitBreakers, rateLimits }) => (
  <div className="panel-content space-y-6">
    <section>
      <h4 className="text-sm font-semibold text-slate-300 mb-3">Circuit Breakers</h4>
      <div className="space-y-3">
        {circuitBreakers.map((cb) => {
          const healthPercent = (1 - cb.failures / cb.threshold) * 100;
          const statusIcon = cb.state === 'closed' ? '✓' : cb.state === 'half-open' ? '⚠️' : '✗';
          const statusColor = cb.state === 'closed' ? 'text-green-400' : 
                             cb.state === 'half-open' ? 'text-yellow-400' : 'text-red-400';
          
          return (
            <div key={cb.name} className="circuit-breaker">
              <div className="flex items-center justify-between mb-1">
                <span className="cb-name text-xs text-slate-400">{cb.name}</span>
                <span className={`cb-status ${statusColor} text-xs font-semibold`}>
                  {statusIcon}
                </span>
              </div>
              <div className="cb-bar h-2 bg-slate-800 rounded-full overflow-hidden">
                <div
                  className={`cb-fill h-full transition-all ${
                    cb.state === 'closed' ? 'bg-green-500' :
                    cb.state === 'half-open' ? 'bg-yellow-500' : 'bg-red-500'
                  }`}
                  style={{ width: `${Math.max(0, healthPercent)}%` }}
                />
              </div>
            </div>
          );
        })}
      </div>
    </section>

    <section>
      <h4 className="text-sm font-semibold text-slate-300 mb-3">Rate Limits</h4>
      <div className="space-y-3">
        {rateLimits.map((rl) => {
          const usagePercent = (rl.current / rl.max) * 100;
          const isWarning = usagePercent > 80;
          
          return (
            <div key={rl.name} className="rate-limit">
              <div className="flex items-center justify-between mb-1">
                <span className="rl-name text-xs text-slate-400">{rl.name}</span>
                <span className={`rl-count text-xs font-semibold ${isWarning ? 'text-red-400' : 'text-slate-300'}`}>
                  {rl.current}/{rl.max}
                </span>
              </div>
              <div className="rl-bar h-2 bg-slate-800 rounded-full overflow-hidden">
                <div
                  className="rl-fill h-full transition-all"
                  style={{
                    width: `${usagePercent}%`,
                    backgroundColor: isWarning ? '#ef4444' : '#3b82f6',
                  }}
                />
              </div>
            </div>
          );
        })}
      </div>
    </section>
  </div>
);

const MetricsPanel: React.FC<{ metrics: FrameworkMetrics }> = ({ metrics }) => {
  const totalTime = metrics.deterministicTime + metrics.agenticTime;
  const totalBricks = metrics.deterministicCount + metrics.agenticCount;
  const detTimePct = totalTime > 0 ? (metrics.deterministicTime / totalTime) * 100 : 0;
  const detCountPct = totalBricks > 0 ? (metrics.deterministicCount / totalBricks) * 100 : 0;

  return (
    <div className="panel-content space-y-6">
      <section>
        <h4 className="text-sm font-semibold text-slate-300 mb-3">Implementation Distribution</h4>
        
        <div className="space-y-2">
          <div className="metric-row">
            <span className="metric-label text-xs text-slate-400">⚙️ Deterministic</span>
            <div className="metric-bar h-2 bg-slate-800 rounded-full overflow-hidden mt-1">
              <div className="metric-fill det bg-blue-600 h-full transition-all" style={{ width: `${detCountPct}%` }} />
            </div>
            <span className="metric-value text-xs text-slate-300 mt-1">{metrics.deterministicCount} bricks</span>
          </div>
          
          <div className="metric-row">
            <span className="metric-label text-xs text-slate-400">🤖 Agentic</span>
            <div className="metric-bar h-2 bg-slate-800 rounded-full overflow-hidden mt-1">
              <div className="metric-fill ai bg-purple-600 h-full transition-all" style={{ width: `${100 - detCountPct}%` }} />
            </div>
            <span className="metric-value text-xs text-slate-300 mt-1">{metrics.agenticCount} bricks</span>
          </div>
        </div>
      </section>

      <section>
        <h4 className="text-sm font-semibold text-slate-300 mb-3">Time Distribution</h4>
        
        <div className="space-y-2">
          <div className="metric-row">
            <span className="metric-label text-xs text-slate-400">⚙️</span>
            <div className="metric-bar h-2 bg-slate-800 rounded-full overflow-hidden mt-1">
              <div className="metric-fill det bg-blue-600 h-full transition-all" style={{ width: `${detTimePct}%` }} />
            </div>
            <span className="metric-value text-xs text-slate-300 mt-1">{metrics.deterministicTime}ms</span>
          </div>
          
          <div className="metric-row">
            <span className="metric-label text-xs text-slate-400">🤖</span>
            <div className="metric-bar h-2 bg-slate-800 rounded-full overflow-hidden mt-1">
              <div className="metric-fill ai bg-purple-600 h-full transition-all" style={{ width: `${100 - detTimePct}%` }} />
            </div>
            <span className="metric-value text-xs text-slate-300 mt-1">{(metrics.agenticTime / 1000).toFixed(1)}s</span>
          </div>
        </div>

        {detCountPct > 50 && detTimePct < 20 && (
          <div className="insight-box mt-3 p-2 bg-blue-900/30 border border-blue-700 rounded text-xs text-blue-300">
            <strong>💡 Key Insight:</strong> Deterministic handles {detCountPct.toFixed(0)}% 
            of work in {detTimePct.toFixed(0)}% of time
          </div>
        )}
      </section>

      <section>
        <h4 className="text-sm font-semibold text-slate-300 mb-3">Cache Performance</h4>
        <div className="cache-stats grid grid-cols-3 gap-2">
          <div className="cache-stat text-center p-2 bg-slate-800 rounded">
            <span className="stat-value block text-lg font-bold text-white">{metrics.cacheHits}</span>
            <span className="stat-label text-xs text-slate-400">Hits</span>
          </div>
          <div className="cache-stat text-center p-2 bg-slate-800 rounded">
            <span className="stat-value block text-lg font-bold text-white">{metrics.cacheMisses}</span>
            <span className="stat-label text-xs text-slate-400">Misses</span>
          </div>
          <div className="cache-stat highlight text-center p-2 bg-green-900/30 border border-green-700 rounded">
            <span className="stat-value block text-lg font-bold text-green-400">${metrics.estimatedSavings.toFixed(2)}</span>
            <span className="stat-label text-xs text-green-300">Saved</span>
          </div>
        </div>
      </section>
    </div>
  );
};

const ProvidersPanel: React.FC<{ providers: Provider[] }> = ({ providers }) => (
  <div className="panel-content">
    <section>
      <h4 className="text-sm font-semibold text-slate-300 mb-3">AI Providers</h4>
      <div className="providers-table space-y-2">
        {providers.map((p) => {
          const statusDot = p.status === 'active' ? 'bg-green-400' :
                          p.status === 'standby' ? 'bg-yellow-400' : 'bg-red-400';
          
          return (
            <div key={p.name} className="provider-row p-2 bg-slate-800 rounded flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className={`status-dot w-2 h-2 rounded-full ${statusDot}`} />
                <span className="text-xs text-white">{p.name}</span>
              </div>
              <div className="text-right">
                {p.latency && <span className="latency text-xs text-slate-400 mr-2">{p.latency}ms</span>}
                <span className="text-xs text-slate-300">{p.calls} calls</span>
                <span className="text-xs text-slate-400 ml-2">${p.cost.toFixed(2)}</span>
              </div>
            </div>
          );
        })}
      </div>
    </section>
  </div>
);
