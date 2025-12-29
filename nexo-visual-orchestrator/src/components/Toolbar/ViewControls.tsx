// src/components/Toolbar/ViewControls.tsx

import { HiRefresh } from 'react-icons/hi';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { hierarchicalTierLayout } from '../../utils/layoutEngine';

export default function ViewControls() {
  const { roles, relationships, loadWorkflow } = useOrchestrationStore();

  const handleAutoLayout = () => {
    if (roles.length === 0) return;
    
    const layoutedRoles = hierarchicalTierLayout(roles);
    console.log('Layouted roles positions:', layoutedRoles.map(r => ({
      id: r.id,
      tier: r.modelConfig.tier,
      position: r.position,
    })));
    
    loadWorkflow(layoutedRoles, relationships);
  };

  return (
    <div className="flex items-center gap-2">
      <button
        onClick={handleAutoLayout}
        className="px-3 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded transition-colors flex items-center gap-1.5"
        title="Auto-layout nodes"
      >
        <HiRefresh className="w-4 h-4" />
        <span>Layout</span>
      </button>
    </div>
  );
}

