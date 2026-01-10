import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';

interface ExecutionOverlayProps {
  status: 'waiting' | 'running' | 'completed' | 'failed';
  progress?: number;
  currentBrick?: string;
  implementation?: 'Deterministic' | 'Agentic';
  duration?: number;
}

export const ExecutionOverlay: React.FC<ExecutionOverlayProps> = ({
  status,
  progress = 0,
  currentBrick,
  implementation,
  duration,
}) => {
  return (
    <AnimatePresence>
      {status === 'running' && (
        <motion.div
          className="execution-overlay absolute inset-0 pointer-events-none"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
        >
          {/* Pulsing border */}
          <motion.div
            className="pulse-border absolute inset-0 rounded-lg"
            animate={{
              boxShadow: [
                '0 0 0 0 rgba(59, 130, 246, 0.7)',
                '0 0 0 10px rgba(59, 130, 246, 0)',
              ],
            }}
            transition={{ duration: 1, repeat: Infinity }}
          />
          
          {/* Progress bar */}
          <div className="progress-container absolute bottom-0 left-0 right-0 h-1 bg-slate-900 rounded-b-lg overflow-hidden">
            <motion.div
              className="progress-bar h-full bg-gradient-to-r from-blue-600 to-purple-600"
              initial={{ width: 0 }}
              animate={{ width: `${progress}%` }}
              transition={{ duration: 0.3 }}
            />
          </div>
          
          {/* Current brick indicator */}
          {currentBrick && (
            <motion.div
              className="current-brick-badge absolute -top-6 right-0 bg-blue-600 text-white px-2 py-1 rounded text-xs font-semibold flex items-center gap-1 shadow-lg"
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
            >
              <span className="brick-name">{currentBrick}</span>
              <span className="brick-impl">{implementation === 'Deterministic' ? '⚙️' : '🤖'}</span>
              {duration && <span className="brick-duration text-xs opacity-75">({duration}ms)</span>}
            </motion.div>
          )}
        </motion.div>
      )}
      
      {status === 'completed' && (
        <motion.div
          className="complete-indicator absolute -top-2 -right-2 w-6 h-6 bg-green-500 rounded-full flex items-center justify-center text-white font-bold shadow-lg z-10"
          initial={{ scale: 0 }}
          animate={{ scale: 1 }}
          transition={{ type: 'spring', stiffness: 500, damping: 30 }}
        >
          ✓
        </motion.div>
      )}
      
      {status === 'failed' && (
        <motion.div
          className="failed-indicator absolute -top-2 -right-2 w-6 h-6 bg-red-500 rounded-full flex items-center justify-center text-white font-bold shadow-lg z-10"
          initial={{ scale: 0 }}
          animate={{ scale: 1 }}
          transition={{ type: 'spring', stiffness: 500, damping: 30 }}
        >
          ✗
        </motion.div>
      )}
    </AnimatePresence>
  );
};
