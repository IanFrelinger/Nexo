import { useEffect } from 'react';

interface KeyboardShortcutsOptions {
  onRun?: () => void;
  onToggleMode?: () => void;
  onUndo?: () => void;
  onRedo?: () => void;
  onDelete?: () => void;
  onSelectAll?: () => void;
  enabled?: boolean;
}

export const useKeyboardShortcuts = ({
  onRun,
  onToggleMode,
  onUndo,
  onRedo,
  onDelete,
  onSelectAll,
  enabled = true,
}: KeyboardShortcutsOptions) => {
  useEffect(() => {
    if (!enabled) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      // Cmd/Ctrl + Enter = Run
      if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') {
        e.preventDefault();
        onRun?.();
        return;
      }
      
      // Cmd/Ctrl + M = Toggle Mode
      if ((e.metaKey || e.ctrlKey) && e.key === 'm') {
        e.preventDefault();
        onToggleMode?.();
        return;
      }
      
      // Cmd/Ctrl + Z = Undo
      if ((e.metaKey || e.ctrlKey) && e.key === 'z' && !e.shiftKey) {
        e.preventDefault();
        onUndo?.();
        return;
      }
      
      // Cmd/Ctrl + Shift + Z = Redo
      if ((e.metaKey || e.ctrlKey) && e.key === 'z' && e.shiftKey) {
        e.preventDefault();
        onRedo?.();
        return;
      }
      
      // Backspace/Delete = Delete selected (only if not in input)
      if ((e.key === 'Backspace' || e.key === 'Delete') && 
          !(e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement)) {
        onDelete?.();
        return;
      }
      
      // Cmd/Ctrl + A = Select all (only if not in input)
      if ((e.metaKey || e.ctrlKey) && e.key === 'a' &&
          !(e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement)) {
        e.preventDefault();
        onSelectAll?.();
        return;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onRun, onToggleMode, onUndo, onRedo, onDelete, onSelectAll, enabled]);
};
