// src/components/Toolbar/ViewControls.tsx

/**
 * ViewControls Component
 * 
 * Toolbar section providing view management operations. Currently provides
 * an auto-layout button that automatically arranges roles on the canvas
 * using a hierarchical tier-based layout algorithm.
 */

import { HiRefresh } from 'react-icons/hi';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { hierarchicalTierLayout } from '../../utils/layoutEngine';

/**
 * ViewControls - View management and layout controls
 * @returns JSX element
 */
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

