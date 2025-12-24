import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { AGENT_REGISTRY } from '../../utils/agentRegistry';
import type { ConfigProperty } from '../../types/agents';

export default function PropertiesPanel() {
  const { nodes, selectedNodeId, updateNodeConfig, updateNodeLabel, removeNode } =
    useOrchestrationStore();

  const selectedNode = nodes.find((n) => n.id === selectedNodeId);

  if (!selectedNode) {
    return (
      <div className="w-80 bg-surface border-l border-slate-700 p-4">
        <p className="text-slate-500 text-sm">Select a node to view properties</p>
      </div>
    );
  }

  const agentDef = AGENT_REGISTRY[selectedNode.data.agentType];

  const handleConfigChange = (key: string, value: unknown) => {
    updateNodeConfig(selectedNode.id, { [key]: value });
  };

  const renderConfigField = (key: string, prop: ConfigProperty) => {
    const value = selectedNode.data.config[key] ?? prop.default;

    switch (prop.type) {
      case 'string':
        return (
          <input
            type="text"
            value={(value as string) || ''}
            onChange={(e) => handleConfigChange(key, e.target.value)}
            className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500"
          />
        );

      case 'textarea':
        return (
          <textarea
            value={(value as string) || ''}
            onChange={(e) => handleConfigChange(key, e.target.value)}
            rows={3}
            className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 resize-none"
          />
        );

      case 'number':
        return (
          <input
            type="number"
            value={(value as number) ?? 0}
            min={prop.min}
            max={prop.max}
            step={prop.step ?? 1}
            onChange={(e) => handleConfigChange(key, parseFloat(e.target.value))}
            className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500"
          />
        );

      case 'boolean':
        return (
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={Boolean(value)}
              onChange={(e) => handleConfigChange(key, e.target.checked)}
              className="w-4 h-4 rounded border-slate-600 bg-surface-dark text-blue-500 focus:ring-blue-500"
            />
            <span className="text-sm text-slate-300">Enabled</span>
          </label>
        );

      case 'select':
        return (
          <select
            value={(value as string) || ''}
            onChange={(e) => handleConfigChange(key, e.target.value)}
            className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500"
          >
            {prop.options?.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        );

      case 'multiselect':
        const selectedValues = (value as string[]) || [];
        return (
          <div className="space-y-1">
            {prop.options?.map((opt) => (
              <label key={opt.value} className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={selectedValues.includes(opt.value)}
                  onChange={(e) => {
                    const newValues = e.target.checked
                      ? [...selectedValues, opt.value]
                      : selectedValues.filter((v) => v !== opt.value);
                    handleConfigChange(key, newValues);
                  }}
                  className="w-4 h-4 rounded border-slate-600 bg-surface-dark text-blue-500"
                />
                <span className="text-sm text-slate-300">{opt.label}</span>
              </label>
            ))}
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className="w-80 bg-surface border-l border-slate-700 flex flex-col h-full">
      {/* Header */}
      <div className="p-3 border-b border-slate-700">
        <div className="flex items-center gap-2">
          <span className="text-lg">{agentDef.icon}</span>
          <input
            type="text"
            value={selectedNode.data.label}
            onChange={(e) => updateNodeLabel(selectedNode.id, e.target.value)}
            className="flex-1 bg-transparent font-semibold text-sm focus:outline-none border-b border-transparent focus:border-blue-500"
          />
        </div>
        <p className="text-xs text-slate-500 mt-1">{selectedNode.id}</p>
      </div>

      {/* Config Fields */}
      <div className="flex-1 overflow-y-auto p-3 space-y-4">
        <div>
          <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-2">
            Configuration
          </h3>
          <div className="space-y-3">
            {Object.entries(agentDef.configSchema.properties).map(([key, prop]) => (
              <div key={key}>
                <label className="block text-sm text-slate-300 mb-1">
                  {prop.label}
                  {agentDef.configSchema.required.includes(key) && (
                    <span className="text-red-400 ml-1">*</span>
                  )}
                </label>
                {prop.description && (
                  <p className="text-xs text-slate-500 mb-1">{prop.description}</p>
                )}
                {renderConfigField(key, prop)}
              </div>
            ))}
          </div>
        </div>

        {/* Output preview */}
        {selectedNode.data.output != null && (
          <div>
            <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-2">
              Output
            </h3>
            <pre className="text-xs bg-surface-dark p-2 rounded border border-slate-700 overflow-auto max-h-40">
              {String(JSON.stringify(selectedNode.data.output, null, 2) || '')}
            </pre>
          </div>
        )}
      </div>

      {/* Actions */}
      <div className="p-3 border-t border-slate-700">
        <button
          onClick={() => removeNode(selectedNode.id)}
          className="w-full px-3 py-1.5 bg-red-500/20 text-red-400 rounded text-sm hover:bg-red-500/30 transition-colors"
        >
          Delete Node
        </button>
      </div>
    </div>
  );
}

