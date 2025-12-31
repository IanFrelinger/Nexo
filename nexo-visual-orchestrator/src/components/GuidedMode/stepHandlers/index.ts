// src/components/GuidedMode/stepHandlers/index.ts

/**
 * Step Handlers Index
 * 
 * Central export point for all guided mode step handlers. Each handler
 * processes user input for a specific step and transitions to the next step.
 */

export { handleWelcomeStep } from './welcomeStep';
export { handleTeamStructureStep } from './teamStructureStep';
export { handleAutonomyLevelStep } from './autonomyLevelStep';
export { handleScalingPreferenceStep } from './scalingPreferenceStep';
export { handleConflictResolutionStep } from './conflictResolutionStep';
export { handleReviewStep } from './reviewStep';

