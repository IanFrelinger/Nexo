export interface TestScenario {
  name: string;
  description: string;
  steps: TestStep[];
  validation: ValidationConfig;
}

export interface TestStep {
  action: string;
  selector?: string;
  name?: string;
  prefix?: string;
  interval?: number;
  count?: number;
  workflow?: string;
  nodeType?: string;
  agentId?: string;
  position?: { x: number; y: number };
  from?: string;
  to?: string;
  mode?: 'deterministic' | 'agentic' | 'mixed';
  key?: string;
  state?: string;
  duration?: number;
  label?: string;
  steps?: TestStep[];
  timeout?: number;
  url?: string;
}

export interface ValidationConfig {
  type: 'visual' | 'visual-sequence' | 'state' | 'performance' | 'accessibility';
  criteria: string[];
  thresholds?: Record<string, number>;
}

export interface TestResult {
  scenario: string;
  passed: boolean;
  score: number;
  duration: number;
  screenshots: string[];
  evaluation: EvaluationResult | null;
  error: string | null;
}

export interface EvaluationResult {
  score: number;
  summary: string;
  criteriaResults: CriterionResult[];
  recommendations: string[];
}

export interface CriterionResult {
  criterion: string;
  passed: boolean;
  explanation: string;
  issues: string[];
}

export interface TestReport {
  timestamp: string;
  summary: {
    total: number;
    passed: number;
    failed: number;
    score: number;
    duration: number;
  };
  results: TestResult[];
  recommendations: string[];
}
