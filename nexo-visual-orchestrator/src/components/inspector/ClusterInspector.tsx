import { useState } from 'react';
import { useCluster } from '../../hooks/useClusters';
import { ImplementationToggle } from './ImplementationToggle';
import { ScalingConfig } from './ScalingConfig';
import { ExportTab as ExportTabComponent } from './ExportTab';

interface Props {
  clusterId: string;
  instanceId?: string;
}

type Tab = 'overview' | 'bricks' | 'scaling' | 'export';

export function ClusterInspector({ clusterId, instanceId }: Props) {
  const { data: cluster } = useCluster(clusterId);
  const [activeTab, setActiveTab] = useState<Tab>('overview');
  
  if (!cluster) return <div className="p-4 text-slate-400">Loading...</div>;
  
  return (
    <div className="cluster-inspector flex flex-col h-full bg-slate-900 text-white">
      <div className="inspector-header p-4 border-b border-slate-700 flex items-center gap-3">
        <span className="text-3xl">{cluster.icon}</span>
        <div className="flex-1">
          <h3 className="font-bold text-lg">{cluster.name}</h3>
          <span className="text-sm text-slate-400">v{cluster.version}</span>
        </div>
      </div>
      
      <div className="inspector-tabs flex border-b border-slate-700">
        {(['overview', 'bricks', 'scaling', 'export'] as Tab[]).map(tab => (
          <button
            key={tab}
            className={`px-4 py-2 capitalize ${activeTab === tab ? 'bg-slate-800 border-b-2 border-blue-500' : 'hover:bg-slate-800'}`}
            onClick={() => setActiveTab(tab)}
          >
            {tab === 'bricks' ? `${tab} (${cluster.bricks.length})` : tab}
          </button>
        ))}
      </div>
      
      <div className="inspector-content flex-1 overflow-y-auto p-4">
        {activeTab === 'overview' && <OverviewTab cluster={cluster} instanceId={instanceId} />}
        {activeTab === 'bricks' && <BricksTab cluster={cluster} instanceId={instanceId} />}
        {activeTab === 'scaling' && <ScalingTab cluster={cluster} />}
        {activeTab === 'export' && <ExportTabComponent cluster={cluster} />}
      </div>
    </div>
  );
}

function OverviewTab({ cluster }: { cluster: any; instanceId?: string }) {
  return (
    <div className="overview-tab space-y-4">
      <section>
        <h4 className="font-semibold mb-2">Description</h4>
        <p className="text-slate-300">{cluster.description}</p>
      </section>
      
      <section>
        <h4 className="font-semibold mb-2">Interface</h4>
        <div className="interface-section grid grid-cols-2 gap-4">
          <div className="inputs">
            <h5 className="text-sm font-medium text-slate-400 mb-2">Inputs</h5>
            {cluster.interface.inputs.map((input: any) => (
              <div key={input.name} className="port mb-2 p-2 bg-slate-800 rounded">
                <span className="port-name font-medium">{input.name}</span>
                <span className="port-type text-xs text-slate-400 ml-2">{input.type}</span>
                {input.required && <span className="required text-red-400 ml-1">*</span>}
              </div>
            ))}
          </div>
          <div className="outputs">
            <h5 className="text-sm font-medium text-slate-400 mb-2">Outputs</h5>
            {cluster.interface.outputs.map((output: any) => (
              <div key={output.name} className="port mb-2 p-2 bg-slate-800 rounded">
                <span className="port-name font-medium">{output.name}</span>
                <span className="port-type text-xs text-slate-400 ml-2">{output.type}</span>
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}

function BricksTab({ cluster }: { cluster: any; instanceId?: string }) {
  return (
    <div className="bricks-tab">
      <p className="text-sm text-slate-400 mb-4">
        {cluster.bricks.length} bricks in this cluster
      </p>
      
      <div className="bricks-list space-y-2">
        {cluster.bricks.map((brick: any) => (
          <div key={brick.localId} className="brick-item p-3 bg-slate-800 rounded">
            <div className="brick-header flex items-center gap-2 mb-2">
              <span className="text-xl">{brick.icon || '📦'}</span>
              <span className="font-medium">{brick.localId}</span>
            </div>
            
            {brick.allowImplementationOverride ? (
              <ImplementationToggle
                current={brick.defaultImplementation}
                hasDeterministic={true}
                hasAgentic={true}
                onChange={(_impl) => {
                  // TODO: Update instance implementation
                }}
              />
            ) : (
              <div className="impl-locked flex items-center gap-2 text-sm text-slate-400">
                <span>{brick.defaultImplementation === 'deterministic' ? '⚙️' : '🤖'}</span>
                <span>Locked</span>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

function ScalingTab({ cluster }: { cluster: any }) {
  return <ScalingConfig cluster={cluster} />;
}

