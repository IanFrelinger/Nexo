// src/components/Panels/RelationshipProperties.tsx

import type { Relationship, RoleDefinition } from '../../types/workflow';

interface RelationshipPropertiesProps {
  relationship: Relationship;
  sourceRole?: RoleDefinition;
  targetRole?: RoleDefinition;
  onDelete: () => void;
}

export default function RelationshipProperties({
  relationship,
  sourceRole,
  targetRole,
  onDelete,
}: RelationshipPropertiesProps) {
  return (
    <>
      <div className="p-3 border-b border-slate-700">
        <h3 className="font-semibold text-sm text-white">Relationship</h3>
        <p className="text-xs text-slate-500 mt-1">
          {sourceRole?.name || relationship.sourceRoleId} --({relationship.type})--&gt; {targetRole?.name || relationship.targetRoleId}
        </p>
      </div>
      <div className="flex-1 overflow-y-auto p-3 space-y-4 custom-scrollbar">
        <div>
          <label className="block text-xs text-slate-400 mb-1">Type</label>
          <select
            value={relationship.type}
            onChange={() => {
              // Simplified: delete and re-add for type change
              onDelete();
            }}
            className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 text-slate-200"
          >
            <option value="delegates">Delegates</option>
            <option value="reports-to">Reports To</option>
            <option value="negotiates">Negotiates</option>
            <option value="observes">Observes</option>
          </select>
        </div>
        {relationship.type === 'negotiates' && relationship.metadata?.topics && (
          <div>
            <label className="block text-xs text-slate-400 mb-1">Topics (comma-separated)</label>
            <textarea
              value={relationship.metadata.topics.join(', ')}
              onChange={() => {
                // This would need a proper update method
              }}
              rows={2}
              className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 resize-none text-slate-200"
            />
          </div>
        )}
      </div>
      <div className="p-3 border-t border-slate-700">
        <button
          onClick={onDelete}
          className="w-full px-3 py-1.5 bg-red-500/20 text-red-400 rounded text-sm hover:bg-red-500/30 transition-colors"
        >
          Delete Relationship
        </button>
      </div>
    </>
  );
}

