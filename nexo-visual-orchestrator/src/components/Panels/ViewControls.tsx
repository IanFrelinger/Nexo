// src/components/Panels/ViewControls.tsx

import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { TIER_CONFIGS } from '../../utils/layoutEngine';
import { HiChevronDown, HiChevronUp, HiEye, HiEyeOff } from 'react-icons/hi';

const RELATIONSHIP_TYPE_LABELS: Record<string, string> = {
  'delegates': 'Delegates',
  'reports-to': 'Reports To',
  'negotiates': 'Negotiates',
  'observes': 'Observes',
};

const RELATIONSHIP_TYPE_COLORS: Record<string, string> = {
  'delegates': '#64748b', // slate-500
  'reports-to': '#8b5cf6', // purple-500
  'negotiates': '#eab308', // yellow-500
  'observes': '#64748b', // slate-500
};

interface ViewControlsProps {
  onCollapse?: () => void;
}

export default function ViewControls({ onCollapse }: ViewControlsProps) {
  const {
    collapsedTiers,
    visibleRelationshipTypes,
    toggleTierCollapse,
    toggleRelationshipTypeVisibility,
  } = useOrchestrationStore();

  return (
    <div className="bg-surface border border-slate-700 rounded-lg p-4 space-y-4 min-w-[240px] shadow-lg">
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-semibold text-slate-200">View Controls</h3>
        {onCollapse && (
          <button
            onClick={onCollapse}
            className="text-slate-400 hover:text-slate-200 text-xs"
            title="Hide View Controls"
          >
            ×
          </button>
        )}
      </div>
      
      {/* Tier Collapse Controls */}
      <div>
        <h4 className="text-xs font-medium text-slate-400 mb-2 uppercase tracking-wide">Tiers</h4>
        <div className="space-y-1.5">
          {TIER_CONFIGS.map(tier => {
            const isCollapsed = collapsedTiers.has(tier.tier);
            return (
              <button
                key={tier.tier}
                onClick={() => toggleTierCollapse(tier.tier)}
                className="w-full flex items-center justify-between px-2 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded transition-colors"
              >
                <span className="flex items-center gap-2">
                  <div
                    className="w-3 h-3 rounded"
                    style={{ backgroundColor: tier.color.replace('0.1', '0.6') }}
                  />
                  <span>{tier.label}</span>
                </span>
                {isCollapsed ? (
                  <HiChevronDown className="w-4 h-4 text-slate-400" />
                ) : (
                  <HiChevronUp className="w-4 h-4 text-slate-400" />
                )}
              </button>
            );
          })}
        </div>
      </div>

      {/* Relationship Type Filters */}
      <div>
        <h4 className="text-xs font-medium text-slate-400 mb-2 uppercase tracking-wide">Relationships</h4>
        <div className="space-y-1.5">
          {Object.entries(RELATIONSHIP_TYPE_LABELS).map(([type, label]) => {
            const isVisible = visibleRelationshipTypes.has(type);
            return (
              <button
                key={type}
                onClick={() => toggleRelationshipTypeVisibility(type)}
                className="w-full flex items-center justify-between px-2 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded transition-colors"
              >
                <span className="flex items-center gap-2">
                  <div
                    className="w-3 h-3 rounded"
                    style={{ backgroundColor: RELATIONSHIP_TYPE_COLORS[type] }}
                  />
                  <span>{label}</span>
                </span>
                {isVisible ? (
                  <HiEye className="w-4 h-4 text-slate-400" />
                ) : (
                  <HiEyeOff className="w-4 h-4 text-slate-500" />
                )}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

