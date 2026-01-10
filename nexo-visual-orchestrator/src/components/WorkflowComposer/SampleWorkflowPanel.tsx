import React from 'react';
import { sampleWorkflows } from '../../data/sampleWorkflows';
import type { WorkflowDefinition } from '../../types/workflowComposer';

interface SampleWorkflowPanelProps {
  onLoadWorkflow: (workflow: WorkflowDefinition) => void;
  onClose: () => void;
}

export const SampleWorkflowPanel: React.FC<SampleWorkflowPanelProps> = ({
  onLoadWorkflow,
  onClose,
}) => {
  return (
    <div className="sample-workflows fixed inset-0 z-50 bg-black/70 flex items-center justify-center">
      <div className="bg-slate-900 border border-slate-700 rounded-lg shadow-2xl w-full max-w-4xl max-h-[80vh] flex flex-col">
        <div className="p-6 border-b border-slate-700 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-bold text-white mb-2">Quick Start</h2>
            <p className="text-slate-400">Load a sample workflow to explore Nexo's capabilities</p>
          </div>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-white transition-colors"
          >
            ✕
          </button>
        </div>
        
        <div className="flex-1 overflow-y-auto p-6">
          <div className="workflow-list grid grid-cols-1 md:grid-cols-2 gap-4">
            {sampleWorkflows.map((sample) => (
              <button
                key={sample.id}
                className="workflow-card p-4 bg-slate-800 hover:bg-slate-700 rounded-lg border border-slate-700 hover:border-blue-500 transition-all text-left group"
                onClick={() => {
                  onLoadWorkflow(sample.workflow);
                  onClose();
                }}
              >
                <div className="flex items-start gap-3 mb-3">
                  <span className="workflow-icon text-3xl">{sample.icon}</span>
                  <div className="workflow-info flex-1">
                    <h3 className="workflow-name text-lg font-semibold text-white mb-1 group-hover:text-blue-400 transition-colors">
                      {sample.name}
                    </h3>
                    <p className="workflow-desc text-sm text-slate-400">{sample.description}</p>
                  </div>
                </div>
                <div className="demo-points space-y-1">
                  {sample.demoPoints.map((point, i) => (
                    <div key={i} className="demo-point text-xs text-slate-500 flex items-center gap-2">
                      <span>💡</span>
                      <span>{point}</span>
                    </div>
                  ))}
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
