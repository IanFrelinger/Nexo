import { Page } from 'playwright';
import { VisualEvaluator } from './VisualEvaluator';
import * as path from 'path';
import type { EvaluationResult } from './types';

export interface ExplorationStep {
  action: string;
  selector?: string;
  description: string;
  screenshot?: string;
  visualValidation?: string[];
}

export interface ExplorationResult {
  journey: string;
  steps: ExplorationStep[];
  visualIssues: Array<{
    step: string;
    issue: string;
    severity: 'low' | 'medium' | 'high';
    screenshot?: string;
  }>;
  score: number;
  duration: number;
}

/**
 * VirtualUserExplorer - Acts like a real user exploring the frontend
 * Takes screenshots at each step and uses LLM to validate visual appearance
 */
export class VirtualUserExplorer {
  private steps: ExplorationStep[] = [];
  private visualIssues: ExplorationResult['visualIssues'] = [];
  private screenshotCounter = 0;
  
  constructor(
    private page: Page,
    private visualEvaluator: VisualEvaluator,
    private screenshotDir: string
  ) {}
  
  /**
   * Explore the UI like a virtual user would
   * Takes screenshots and validates visual appearance at each step
   */
  async exploreUserJourney(journey: 'new-user' | 'power-user' | 'explorer'): Promise<ExplorationResult> {
    const startTime = Date.now();
    this.steps = [];
    this.visualIssues = [];
    this.screenshotCounter = 0;
    
    console.log(`\n🧑 Virtual User Journey: ${journey}`);
    console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    
    // Initial page load and visual validation
    await this.takeScreenshotAndValidate('initial-load', [
      'Page loads without errors',
      'Main UI elements are visible',
      'No broken images or layout issues',
      'Professional and polished appearance'
    ]);
    
    switch (journey) {
      case 'new-user':
        await this.exploreAsNewUser();
        break;
      case 'power-user':
        await this.exploreAsPowerUser();
        break;
      case 'explorer':
        await this.exploreRandomly();
        break;
    }
    
    const duration = Date.now() - startTime;
    const score = this.calculateScore();
    
    return {
      journey,
      steps: this.steps,
      visualIssues: this.visualIssues,
      score,
      duration,
    };
  }
  
  /**
   * New user journey - discovers features organically
   */
  private async exploreAsNewUser(): Promise<void> {
    console.log('\n📖 New User Journey:');
    
    // 1. Look for main entry points
    await this.exploreAndValidate('discover-main-features', [
      'Find and click main navigation buttons',
      'Look for "Workflow Composer" or similar',
      'Check for help or tutorial buttons'
    ], async () => {
      const buttons = await this.page.locator('button, a[role="button"]').all();
      if (buttons.length > 0) {
        const mainButton = buttons.find(async btn => {
          const text = await btn.textContent();
          return text?.toLowerCase().includes('workflow') || 
                 text?.toLowerCase().includes('composer') ||
                 text?.toLowerCase().includes('start');
        });
        if (mainButton) {
          await mainButton.click();
          await this.humanDelay(1000, 2000);
        }
      }
    });
    
    // 2. Explore the canvas area
    await this.exploreAndValidate('explore-canvas', [
      'Canvas area is visible and accessible',
      'Empty state is clear and inviting',
      'Toolbar or controls are visible'
    ], async () => {
      await this.page.waitForSelector('.react-flow, [class*="canvas"], [class*="workflow-canvas"]', { timeout: 5000 }).catch(() => {});
      await this.humanDelay(500, 1000);
    });
    
    // 3. Discover library panel
    await this.exploreAndValidate('discover-library', [
      'Library panel is discoverable',
      'Library items are visually clear',
      'Categories or tabs are organized'
    ], async () => {
      // Try to find and open library
      const libraryButton = this.page.locator('button:has-text("Library"), button:has-text("▶ Library"), [class*="library-toggle"]').first();
      const visible = await libraryButton.isVisible({ timeout: 3000 }).catch(() => false);
      if (visible) {
        await libraryButton.click();
        await this.humanDelay(1000, 2000);
      }
    });
    
    // 4. Try loading a sample
    await this.exploreAndValidate('try-samples', [
      'Sample workflows are accessible',
      'Sample selection UI is clear',
      'Loading a sample works smoothly'
    ], async () => {
      const samplesButton = this.page.locator('button:has-text("Samples"), button:has-text("📋"), [class*="samples"]').first();
      const visible = await samplesButton.isVisible({ timeout: 3000 }).catch(() => false);
      if (visible) {
        await samplesButton.click();
        await this.humanDelay(1000, 2000);
        
        // Try clicking first sample if available
        const firstSample = this.page.locator('.workflow-card, .sample-item, button.workflow-card').first();
        const sampleVisible = await firstSample.isVisible({ timeout: 2000 }).catch(() => false);
        if (sampleVisible) {
          await firstSample.click();
          await this.humanDelay(2000, 3000);
        }
      }
    });
    
    // 5. Explore nodes on canvas
    await this.exploreAndValidate('explore-nodes', [
      'Nodes are visually clear and distinct',
      'Node icons or labels are readable',
      'Connections between nodes are visible'
    ], async () => {
      const nodes = await this.page.locator('.react-flow__node').all();
      if (nodes.length > 0) {
        // Click on first node
        await nodes[0].click();
        await this.humanDelay(1000, 2000);
      }
    });
    
    // 6. Discover inspector panel
    await this.exploreAndValidate('discover-inspector', [
      'Inspector panel appears when node is selected',
      'Inspector shows relevant information',
      'Implementation toggle is discoverable'
    ], async () => {
      // Inspector should already be open from node click
      await this.humanDelay(500, 1000);
    });
  }
  
  /**
   * Power user journey - uses advanced features
   */
  private async exploreAsPowerUser(): Promise<void> {
    console.log('\n⚡ Power User Journey:');
    
    // 1. Load a complex workflow
    await this.exploreAndValidate('load-complex-workflow', [
      'Complex workflows load correctly',
      'All nodes render properly',
      'Connections are visible'
    ], async () => {
      await this.loadWorkflow('security-pipeline');
    });
    
    // 2. Explore implementation toggles
    await this.exploreAndValidate('explore-implementations', [
      'Implementation toggles are accessible',
      'Toggle states are visually clear',
      'Switching implementations works smoothly'
    ], async () => {
      const nodes = await this.page.locator('.react-flow__node').all();
      if (nodes.length > 0) {
        await nodes[0].click();
        await this.humanDelay(1000, 2000);
        
        // Try to find and use implementation toggle
        const toggle = this.page.locator('.implementation-toggle, [class*="implementation-toggle"], button:has-text("⚙️"), button:has-text("🤖")').first();
        const visible = await toggle.isVisible({ timeout: 3000 }).catch(() => false);
        if (visible) {
          await toggle.click();
          await this.humanDelay(1000, 2000);
        }
      }
    });
    
    // 3. Test execution features
    await this.exploreAndValidate('test-execution', [
      'Run button is accessible',
      'Execution UI is clear',
      'Framework state is accessible'
    ], async () => {
      const runButton = this.page.locator('button:has-text("Run"), button:has-text("▶")').first();
      const visible = await runButton.isVisible({ timeout: 3000 }).catch(() => false);
      if (visible) {
        // Just validate it exists, don't actually run
        await this.humanDelay(500, 1000);
      }
    });
    
    // 4. Explore framework state
    await this.exploreAndValidate('explore-framework-state', [
      'Framework state sidebar is accessible',
      'Metrics are displayed clearly',
      'State information is organized'
    ], async () => {
      const frameworkButton = this.page.locator('button[title*="framework"], button[title*="Framework"], [class*="framework-toggle"]').first();
      const visible = await frameworkButton.isVisible({ timeout: 3000 }).catch(() => false);
      if (visible) {
        await frameworkButton.click();
        await this.humanDelay(1000, 2000);
      }
    });
  }
  
  /**
   * Random exploration - clicks around like a curious user
   */
  private async exploreRandomly(): Promise<void> {
    console.log('\n🔍 Random Exploration:');
    
    const maxSteps = 8;
    const clickableElements = await this.page.locator('button, a, [role="button"], [onclick]').all();
    
    for (let i = 0; i < Math.min(maxSteps, clickableElements.length); i++) {
      const randomIndex = Math.floor(Math.random() * clickableElements.length);
      const element = clickableElements[randomIndex];
      
      try {
        const text = await element.textContent();
        const isVisible = await element.isVisible({ timeout: 1000 }).catch(() => false);
        
        if (isVisible && text && text.trim().length > 0) {
          await this.exploreAndValidate(`random-click-${i}`, [
            `Clicking "${text.trim()}" works correctly`,
            'UI responds appropriately',
            'No errors or broken states'
          ], async () => {
            await element.click();
            await this.humanDelay(1000, 2500);
          });
        }
      } catch (e) {
        // Skip if element is not interactable
      }
    }
  }
  
  /**
   * Take screenshot and validate visually using LLM
   */
  private async takeScreenshotAndValidate(
    name: string,
    criteria: string[]
  ): Promise<EvaluationResult> {
    const screenshotPath = path.join(
      this.screenshotDir,
      `virtual-user-${this.screenshotCounter++}-${name}.png`
    );
    
    await this.page.screenshot({ path: screenshotPath, fullPage: false });
    
    const step: ExplorationStep = {
      action: 'screenshot',
      description: `Captured screenshot: ${name}`,
      screenshot: screenshotPath,
      visualValidation: criteria,
    };
    
    this.steps.push(step);
    
    // Use LLM to validate visual appearance
    const evaluation = await this.visualEvaluator.evaluate([screenshotPath], criteria);
    
    // Record any issues found
    evaluation.criteriaResults.forEach(result => {
      if (!result.passed && result.issues.length > 0) {
        const severity = this.determineSeverity(result.issues);
        this.visualIssues.push({
          step: name,
          issue: result.explanation,
          severity,
          screenshot: screenshotPath,
        });
      }
    });
    
    console.log(`  📸 ${name}: ${evaluation.score.toFixed(2)} - ${evaluation.summary}`);
    
    return evaluation;
  }
  
  /**
   * Explore an area and validate visually
   */
  private async exploreAndValidate(
    name: string,
    criteria: string[],
    action: () => Promise<void>
  ): Promise<void> {
    try {
      await action();
      await this.takeScreenshotAndValidate(name, criteria);
    } catch (error: any) {
      console.log(`  ⚠️  ${name}: ${error.message}`);
      this.visualIssues.push({
        step: name,
        issue: `Action failed: ${error.message}`,
        severity: 'medium',
      });
    }
  }
  
  /**
   * Load a workflow sample
   */
  private async loadWorkflow(workflowId: string): Promise<void> {
    const samplesButton = this.page.locator('button:has-text("Samples"), button:has-text("📋")').first();
    const visible = await samplesButton.isVisible({ timeout: 3000 }).catch(() => false);
    
    if (visible) {
      await samplesButton.click();
      await this.humanDelay(1000, 2000);
      
      const workflowNameMap: Record<string, string> = {
        'security-pipeline': 'Security Scanning Pipeline',
        'simple-pipeline': 'Simple Pipeline',
      };
      
      const workflowName = workflowNameMap[workflowId] || workflowId;
      const workflowCard = this.page.locator(`text=${workflowName}`).first();
      const cardVisible = await workflowCard.isVisible({ timeout: 3000 }).catch(() => false);
      
      if (cardVisible) {
        await workflowCard.click();
        await this.humanDelay(2000, 3000);
        await this.page.waitForSelector('.react-flow__node', { timeout: 10000 }).catch(() => {});
      }
    }
  }
  
  /**
   * Human-like delay (randomized)
   */
  private async humanDelay(min: number, max: number): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    await this.page.waitForTimeout(delay);
  }
  
  /**
   * Determine severity of visual issues
   */
  private determineSeverity(issues: string[]): 'low' | 'medium' | 'high' {
    const issueText = issues.join(' ').toLowerCase();
    
    if (issueText.includes('error') || issueText.includes('broken') || issueText.includes('crash')) {
      return 'high';
    }
    if (issueText.includes('layout') || issueText.includes('overlap') || issueText.includes('missing')) {
      return 'medium';
    }
    return 'low';
  }
  
  /**
   * Calculate overall exploration score
   */
  private calculateScore(): number {
    if (this.visualIssues.length === 0) return 1.0;
    
    const highIssues = this.visualIssues.filter(i => i.severity === 'high').length;
    const mediumIssues = this.visualIssues.filter(i => i.severity === 'medium').length;
    const lowIssues = this.visualIssues.filter(i => i.severity === 'low').length;
    
    // Weighted penalty
    const penalty = (highIssues * 0.3) + (mediumIssues * 0.15) + (lowIssues * 0.05);
    return Math.max(0, 1.0 - penalty);
  }
}
