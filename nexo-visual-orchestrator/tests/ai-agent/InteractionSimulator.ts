import { Page } from 'playwright';
import * as fs from 'fs';
import * as path from 'path';

export class InteractionSimulator {
  async loadWorkflow(page: Page, workflowId: string): Promise<void> {
    // Try clicking sample workflow button first
    const sampleButton = page.locator('button:has-text("Samples"), button:has-text("📋")').first();
    const sampleVisible = await sampleButton.isVisible({ timeout: 5000 }).catch(() => false);
    
    if (sampleVisible) {
      await sampleButton.click();
      await page.waitForTimeout(1000);
      
      // Wait for the sample panel to appear
      await page.waitForSelector('.sample-workflows', { timeout: 5000 }).catch(() => {});
      
      // Look for the workflow card by name (matching the sample workflow names)
      const workflowNameMap: Record<string, string> = {
        'security-pipeline': 'Security Scanning Pipeline',
        'simple-pipeline': 'Simple Pipeline',
        'ai-code-review': 'AI Code Review',
      };
      
      const workflowName = workflowNameMap[workflowId] || workflowId;
      
      // Try multiple selectors for the workflow card
      const selectors = [
        `.workflow-name:has-text("${workflowName}")`,
        `.workflow-card:has-text("${workflowName}")`,
        `button.workflow-card:has-text("${workflowName}")`,
        `text=${workflowName}`,
      ];
      
      let clicked = false;
      for (const selector of selectors) {
        try {
          const card = page.locator(selector).first();
          const visible = await card.isVisible({ timeout: 2000 });
          if (visible) {
            await card.click();
            clicked = true;
            break;
          }
        } catch (e) {
          // Try next selector
        }
      }
      
      if (clicked) {
        // Wait for panel to close and nodes to appear
        await page.waitForTimeout(1000);
        // Wait for nodes to appear on canvas
        await page.waitForSelector('.react-flow__node', { timeout: 5000 }).catch(() => {});
        await page.waitForTimeout(500);
        return;
      }
    }
    
    // Fallback: Load from fixtures via window injection
    const workflow = await this.getWorkflowFixture(workflowId);
    await page.evaluate((wf) => {
      // @ts-ignore - window.loadWorkflow might be defined by the app
      const win = window as any;
      if (win.loadWorkflow) {
        win.loadWorkflow(wf);
      } else {
        // Try to find and trigger the load function
        const event = new CustomEvent('loadWorkflow', { detail: wf });
        window.dispatchEvent(event);
      }
    }, workflow);
    
    await page.waitForTimeout(1000); // Allow render
    // Wait for nodes to appear
    await page.waitForSelector('.react-flow__node', { timeout: 5000 }).catch(() => {});
  }
  
  async dragFromLibrary(
    page: Page,
    nodeType: string,
    itemId: string
  ): Promise<void> {
    const libraryItem = page.locator(
      `[data-testid="library-item-${itemId}"], [class*="library-item"]:has-text("${itemId}")`
    );
    
    const itemVisible = await libraryItem.isVisible({ timeout: 2000 }).catch(() => false);
    
    if (!itemVisible) {
      // Try alternative selector
      const altItem = page.locator(`text=${itemId}`).first();
      await altItem.hover();
    } else {
      await libraryItem.hover();
    }
    
    await page.mouse.down();
  }
  
  async dropOnCanvas(
    page: Page,
    position: { x: number; y: number }
  ): Promise<void> {
    const canvas = page.locator('[data-testid="workflow-canvas"], .react-flow').first();
    const box = await canvas.boundingBox();
    
    if (!box) {
      // Fallback: use viewport center
      const viewport = page.viewportSize();
      await page.mouse.move(viewport!.width / 2 + position.x, viewport!.height / 2 + position.y);
    } else {
      await page.mouse.move(box.x + position.x, box.y + position.y);
    }
    
    await page.mouse.up();
    await page.waitForTimeout(300); // Allow node to render
  }
  
  async dragConnection(
    page: Page,
    fromHandle: string,
    toHandle: string
  ): Promise<void> {
    const [fromNode, fromPort] = fromHandle.split(':');
    const [toNode, toPort] = toHandle.split(':');
    
    const sourceHandle = page.locator(
      `[data-testid="node-${fromNode}"] [data-handleid="${fromPort}"], 
       [data-testid="node-${fromNode}"] .react-flow__handle[data-handleid="${fromPort}"]`
    ).first();
    
    const targetHandle = page.locator(
      `[data-testid="node-${toNode}"] [data-handleid="${toPort}"],
       [data-testid="node-${toNode}"] .react-flow__handle[data-handleid="${toPort}"]`
    ).first();
    
    const sourceBox = await sourceHandle.boundingBox();
    const targetBox = await targetHandle.boundingBox();
    
    if (!sourceBox || !targetBox) {
      throw new Error(`Handles not found: ${fromHandle} -> ${toHandle}`);
    }
    
    await page.mouse.move(
      sourceBox.x + sourceBox.width / 2,
      sourceBox.y + sourceBox.height / 2
    );
    await page.mouse.down();
    await page.mouse.move(
      targetBox.x + targetBox.width / 2,
      targetBox.y + targetBox.height / 2,
      { steps: 10 }
    );
    await page.mouse.up();
    await page.waitForTimeout(200);
  }
  
  async setImplementation(
    page: Page,
    mode: 'deterministic' | 'agentic' | 'mixed'
  ): Promise<void> {
    const toggle = page.locator('[data-testid="implementation-toggle"], [class*="implementation-toggle"]').first();
    const slider = toggle.locator('input[type="range"]');
    
    const sliderExists = await slider.isVisible({ timeout: 1000 }).catch(() => false);
    
    if (sliderExists) {
      const value = mode === 'deterministic' ? 0 : mode === 'agentic' ? 2 : 1;
      await slider.fill(String(value));
    } else {
      // Try clicking buttons
      const detButton = page.locator('button:has-text("⚙️"), button:has-text("Deterministic")').first();
      const agentButton = page.locator('button:has-text("🤖"), button:has-text("Agentic")').first();
      
      if (mode === 'deterministic') {
        await detButton.click();
      } else if (mode === 'agentic') {
        await agentButton.click();
      }
    }
    
    await page.waitForTimeout(300);
  }
  
  private async getWorkflowFixture(workflowId: string): Promise<any> {
    const fixturesPath = path.join(process.cwd(), 'tests/e2e/fixtures/sampleWorkflows.json');
    
    // Try to load from file first
    if (fs.existsSync(fixturesPath)) {
      const fixtures = JSON.parse(fs.readFileSync(fixturesPath, 'utf-8'));
      if (fixtures[workflowId]) {
        return fixtures[workflowId];
      }
    }
    
    // Fallback to inline fixtures
    const fixtures: Record<string, any> = {
      'security-pipeline': {
        nodes: [
          { id: 'input-1', type: 'input', position: { x: 50, y: 200 } },
          { id: 'dep-scanner', type: 'agent', position: { x: 250, y: 100 } },
          { id: 'owasp-scanner', type: 'agent', position: { x: 250, y: 300 } },
          { id: 'report-gen', type: 'agent', position: { x: 500, y: 200 } },
          { id: 'output-1', type: 'output', position: { x: 700, y: 200 } },
        ],
        edges: [
          { source: 'input-1', target: 'dep-scanner' },
          { source: 'input-1', target: 'owasp-scanner' },
          { source: 'dep-scanner', target: 'report-gen' },
          { source: 'owasp-scanner', target: 'report-gen' },
          { source: 'report-gen', target: 'output-1' },
        ],
      },
      'simple-pipeline': {
        nodes: [
          { id: 'input-1', type: 'input', position: { x: 50, y: 150 } },
          { id: 'brick-1', type: 'brick', position: { x: 250, y: 150 } },
          { id: 'output-1', type: 'output', position: { x: 450, y: 150 } },
        ],
        edges: [
          { source: 'input-1', target: 'brick-1' },
          { source: 'brick-1', target: 'output-1' },
        ],
      },
      'two-unconnected-nodes': {
        nodes: [
          { id: 'input-1', type: 'input', position: { x: 50, y: 150 } },
          { id: 'scanner-1', type: 'agent', position: { x: 300, y: 150 } },
        ],
        edges: [],
      },
      'all-node-types': {
        nodes: [
          { id: 'input-1', type: 'input', position: { x: 50, y: 100 } },
          { id: 'agent-1', type: 'agent', position: { x: 200, y: 100 } },
          { id: 'brick-1', type: 'brick', position: { x: 350, y: 100 } },
          { id: 'cluster-1', type: 'cluster', position: { x: 200, y: 250 } },
          { id: 'transform-1', type: 'transform', position: { x: 350, y: 250 } },
          { id: 'condition-1', type: 'conditional', position: { x: 500, y: 175 } },
          { id: 'output-1', type: 'output', position: { x: 650, y: 175 } },
        ],
        edges: [],
      },
    };
    
    return fixtures[workflowId] || fixtures['simple-pipeline'];
  }
}
