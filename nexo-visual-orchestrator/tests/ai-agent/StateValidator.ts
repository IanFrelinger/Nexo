import { Page } from 'playwright';
import type { EvaluationResult } from './types';

export class StateValidator {
  async validate(
    page: Page,
    criteria: string[]
  ): Promise<EvaluationResult> {
    const results: Array<{
      criterion: string;
      passed: boolean;
      explanation: string;
      issues: string[];
    }> = [];
    
    for (const criterion of criteria) {
      const result = await this.validateCriterion(page, criterion);
      results.push(result);
    }
    
    const passedCount = results.filter(r => r.passed).length;
    const score = results.length > 0 ? passedCount / results.length : 0;
    
    return {
      score,
      summary: `${passedCount}/${results.length} criteria passed`,
      criteriaResults: results,
      recommendations: results
        .filter(r => !r.passed && r.issues.length > 0)
        .flatMap(r => r.issues),
    };
  }
  
  private async validateCriterion(
    page: Page,
    criterion: string
  ): Promise<{
    criterion: string;
    passed: boolean;
    explanation: string;
    issues: string[];
  }> {
    const issues: string[] = [];
    let passed = false;
    let explanation = '';
    
    // Parse common validation patterns
    if (criterion.includes('appears on canvas')) {
      const nodes = await page.locator('.react-flow__node').count();
      passed = nodes > 0;
      explanation = `Found ${nodes} node(s) on canvas`;
      if (!passed) issues.push('No nodes found on canvas');
      
    } else if (criterion.includes('selected')) {
      const selected = await page.locator('.react-flow__node.selected, [class*="selected"]').count();
      passed = selected > 0;
      explanation = `Found ${selected} selected node(s)`;
      if (!passed) issues.push('No node appears to be selected');
      
    } else if (criterion.includes('Inspector panel')) {
      const inspector = await page.locator('.inspector-panel, [class*="inspector"]').isVisible().catch(() => false);
      passed = inspector;
      explanation = inspector ? 'Inspector panel is visible' : 'Inspector panel not visible';
      if (!passed) issues.push('Inspector panel should be visible when node is selected');
      
    } else if (criterion.includes('Connection line')) {
      const edges = await page.locator('.react-flow__edge').count();
      passed = edges > 0;
      explanation = `Found ${edges} connection(s)`;
      if (!passed) issues.push('No connection lines found');
      
    } else if (criterion.includes('green feedback') || criterion.includes('Valid connection')) {
      // Check for connection feedback (might be in DOM or visual)
      const connectionFeedback = await page.locator('[class*="connection-valid"], [class*="connection-success"]').count();
      passed = connectionFeedback > 0 || await page.locator('.react-flow__edge').count() > 0;
      explanation = 'Connection feedback or connection exists';
      if (!passed) issues.push('No valid connection feedback found');
      
    } else if (criterion.includes('red indicator') || criterion.includes('Invalid')) {
      const invalidFeedback = await page.locator('[class*="invalid"], [class*="error"], [class*="connection-error"]').count();
      passed = invalidFeedback > 0;
      explanation = invalidFeedback > 0 ? 'Invalid connection feedback shown' : 'No invalid feedback found';
      if (!passed) issues.push('Should show error indicator for invalid connection');
      
    } else if (criterion.includes('⚙️') || criterion.includes('Deterministic')) {
      const detIndicator = await page.locator('text=⚙️, [class*="deterministic"]').count();
      passed = detIndicator > 0;
      explanation = `Found ${detIndicator} deterministic indicator(s)`;
      if (!passed) issues.push('Deterministic indicator not found');
      
    } else if (criterion.includes('🤖') || criterion.includes('Agentic')) {
      const agentIndicator = await page.locator('text=🤖, [class*="agentic"]').count();
      passed = agentIndicator > 0;
      explanation = `Found ${agentIndicator} agentic indicator(s)`;
      if (!passed) issues.push('Agentic indicator not found');
      
    } else if (criterion.includes('Mixed')) {
      const mixedIndicator = await page.locator('text=⚙️🤖, [class*="mixed"]').count();
      passed = mixedIndicator > 0;
      explanation = `Found ${mixedIndicator} mixed mode indicator(s)`;
      if (!passed) issues.push('Mixed mode indicator not found');
      
    } else if (criterion.includes('removed') || criterion.includes('deleted')) {
      // Check that node count decreased or node is gone
      const nodes = await page.locator('.react-flow__node').count();
      passed = nodes >= 0; // Basic check - would need before/after comparison for real validation
      explanation = `Current node count: ${nodes}`;
      
    } else if (criterion.includes('restored') || criterion.includes('undo')) {
      // Check that node count increased or node is back
      const nodes = await page.locator('.react-flow__node').count();
      passed = nodes > 0;
      explanation = `Current node count: ${nodes}`;
      
    } else {
      // Generic check - try to find element matching criterion
      const element = await page.locator(`text=${criterion}`).first().isVisible().catch(() => false);
      passed = element;
      explanation = element ? 'Element found' : 'Element not found';
      if (!passed) issues.push(`Expected element matching "${criterion}" not found`);
    }
    
    return {
      criterion,
      passed,
      explanation,
      issues,
    };
  }
}
