import { useState } from 'react';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import * as HiIcons from 'react-icons/hi';
import { HiX } from 'react-icons/hi';
import type { IconType } from 'react-icons';

interface RoleCategory {
  id: string;
  label: string;
  icon: IconType;
  roleIds: string[];
}

const ROLE_CATEGORIES: RoleCategory[] = [
  {
    id: 'strategic',
    label: 'Strategic',
    icon: HiIcons.HiViewGrid,
    roleIds: ['architect'],
  },
  {
    id: 'tactical',
    label: 'Tactical',
    icon: HiIcons.HiCube,
    roleIds: ['combat', 'economy', 'ai-behavior', 'level-design', 'narrative'],
  },
  {
    id: 'execution',
    label: 'Execution',
    icon: HiIcons.HiCog,
    roleIds: ['image-gen', 'audio-gen', 'validator', 'build-agent', 'playtest', 'feedback'],
  },
];

interface AgentLibraryProps {
  onCollapse?: () => void;
}

export default function AgentLibrary({ onCollapse }: AgentLibraryProps) {
  const [search, setSearch] = useState('');
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(
    new Set(ROLE_CATEGORIES.map((c) => c.id))
  );

  const toggleCategory = (categoryId: string) => {
    setExpandedCategories((prev) => {
      const next = new Set(prev);
      if (next.has(categoryId)) {
        next.delete(categoryId);
      } else {
        next.add(categoryId);
      }
      return next;
    });
  };

  const onDragStart = (event: React.DragEvent, roleId: string) => {
    event.dataTransfer.setData('application/reactflow', roleId);
    event.dataTransfer.effectAllowed = 'move';
  };

  return (
    <div className="w-64 bg-surface border-r border-slate-700 flex flex-col h-full">
      {/* Header */}
      <div className="p-3 border-b border-slate-700 flex items-center justify-between">
        <h2 className="font-semibold text-sm">Role Library</h2>
        {onCollapse && (
          <button onClick={onCollapse} className="text-slate-400 hover:text-slate-200">
            <HiX className="w-4 h-4" />
          </button>
        )}
      </div>

      {/* Search */}
      <div className="p-2 border-b border-slate-700">
        <input
          type="text"
          placeholder="Search roles..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full px-3 py-1.5 bg-surface-dark border border-slate-600 rounded text-sm placeholder:text-slate-500 focus:outline-none focus:border-blue-500"
        />
      </div>

      {/* Role List */}
      <div className="flex-1 overflow-y-auto p-2 space-y-1">
        {ROLE_CATEGORIES.map((category) => {
          const roles = category.roleIds
            .map((id) => ROLE_TEMPLATES[id])
            .filter(Boolean)
            .filter(
              (role) =>
                !role ||
                search === '' ||
                role.name.toLowerCase().includes(search.toLowerCase()) ||
                role.description.toLowerCase().includes(search.toLowerCase())
            );

          if (roles.length === 0) return null;

          const isExpanded = expandedCategories.has(category.id);

          return (
            <div key={category.id}>
              <button
                onClick={() => toggleCategory(category.id)}
                className="w-full flex items-center gap-2 px-2 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded"
              >
                <span className="text-xs">{isExpanded ? '▼' : '▶'}</span>
                <category.icon className="w-4 h-4" />
                <span>{category.label}</span>
                <span className="ml-auto text-xs text-slate-500">{roles.length}</span>
              </button>

              {isExpanded && (
                <div className="ml-4 mt-1 space-y-1">
                  {roles.map((role) => {
                    if (!role) return null;
                    
                    // Dynamically get the icon component
                    const IconComponent = (HiIcons as any)[role.icon] || HiIcons.HiCube;
                    const colorMap: Record<string, string> = {
                      purple: '#A855F7',
                      red: '#EF4444',
                      yellow: '#EAB308',
                      cyan: '#06B6D4',
                      green: '#22C55E',
                      indigo: '#6366F1',
                      pink: '#EC4899',
                      orange: '#F97316',
                      slate: '#64748B',
                      teal: '#14B8A6',
                      amber: '#F59E0B',
                      emerald: '#10B981',
                    };

                    return (
                      <div
                        key={role.id}
                        draggable
                        onDragStart={(e) => onDragStart(e, role.id)}
                        className="flex items-center gap-2 px-2 py-1.5 bg-surface-dark border border-slate-700 rounded cursor-grab hover:border-slate-500 active:cursor-grabbing"
                        title={role.description}
                      >
                        <IconComponent className="w-4 h-4" style={{ color: colorMap[role.color] || '#64748B' }} />
                        <span className="text-sm">{role.name}</span>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Help text */}
      <div className="p-2 border-t border-slate-700 text-xs text-slate-500">
        Drag roles onto the canvas. Instances spawn automatically based on workload.
      </div>
    </div>
  );
}
