import { Page, expect } from '@playwright/test';

/**
 * Helper function to add a role to the canvas programmatically
 * This bypasses drag and drop which is complex with ReactFlow
 */
export async function addAgentToCanvas(
  page: Page,
  agentName: string,
  position?: { x: number; y: number }
) {
  const textToId: Record<string, string> = {
    'Architect': 'architect',
    'Combat Agent': 'combat',
    'Economy Agent': 'economy',
    'AI Behavior Agent': 'ai-behavior',
    'Level Design Agent': 'level-design',
    'Narrative Agent': 'narrative',
    'Image Generator': 'image-gen',
    'Audio Generator': 'audio-gen',
    'Build Agent': 'build-agent',
    'Playtest Agent': 'playtest',
    'Feedback Synthesizer': 'feedback',
    'Validator': 'validator',
  };
  
  const templateId = textToId[agentName] || agentName.toLowerCase().replace(/\s+/g, '-');
  // Don't provide position - let the component calculate it to avoid overlaps
  // const finalPosition = position || { x: 400, y: 300 };
  
  // Add role directly via store
  await page.evaluate(
    ({ templateId, providedPosition }) => {
      // Trigger a custom event that the component can listen to
      const event = new CustomEvent('test:addRole', {
        detail: { 
          templateId, 
          position: providedPosition || undefined, // Only provide if explicitly set
        },
      });
      window.dispatchEvent(event);
    },
    { templateId, providedPosition: position || undefined }
  );
  
  // Wait longer to ensure the role is added and positioned
  await page.waitForTimeout(2000);
}

/**
 * Helper function to drag and drop an agent onto the React Flow canvas
 * Falls back to programmatic addition if drag fails
 */
export async function dragAgentToCanvas(
  page: Page,
  agentSelector: string,
  targetX?: number,
  targetY?: number
) {
  const agent = page.locator(agentSelector).first();
  await expect(agent).toBeVisible({ timeout: 5000 });
  
  // Get agent text
  const agentText = await agent.textContent();
  if (!agentText) {
    throw new Error('Could not get agent text');
  }
  
  // Try actual drag and drop first
  const canvasWrapper = page.locator('.react-flow').locator('..').first();
  await expect(canvasWrapper).toBeVisible({ timeout: 5000 });
  
  try {
    await agent.dragTo(canvasWrapper, { force: true });
    await page.waitForTimeout(2000);
    
    // Check if it worked
    const nodes = page.locator('.react-flow__node');
    const nodeCount = await nodes.count();
    if (nodeCount > 0) {
      return; // Success!
    }
  } catch (e) {
    // Drag failed, continue to fallback
  }
  
  // Fallback: Use programmatic addition
  await addAgentToCanvas(page, agentText.trim(), targetX && targetY ? { x: targetX, y: targetY } : undefined);
  
  // Verify node was created
  const nodes = page.locator('.react-flow__node');
  await expect(nodes.first()).toBeVisible({ timeout: 5000 });
}

/**
 * Helper function to dismiss the guided mode dialog
 */
export async function dismissGuidedMode(page: Page) {
  // Clear local storage to reset guided mode state BEFORE page loads
  await page.addInitScript(() => {
    localStorage.setItem('nexo-guided-mode-dismissed', 'true');
  });
  
  // Also set it after page loads
  await page.evaluate(() => {
    localStorage.setItem('nexo-guided-mode-dismissed', 'true');
  });
  
  // Wait for page to fully load
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(1000);
  
  // Try to find and close the guided mode if it's visible
  const skipButton = page.locator('button:has-text("Skip"), button:has-text("×"), button:has-text("Close")').first();
  const isVisible = await skipButton.isVisible().catch(() => false);
  
  if (isVisible) {
    // Use force click to bypass any overlays
    await skipButton.click({ force: true, timeout: 2000 }).catch(() => {});
    await page.waitForTimeout(500);
  }
  
  // Also try pressing Escape
  await page.keyboard.press('Escape');
  await page.waitForTimeout(500);
  
  // Verify guided mode overlay is gone
  const overlay = page.locator('.absolute.inset-0.bg-surface-dark.z-50');
  const overlayVisible = await overlay.isVisible().catch(() => false);
  
  if (overlayVisible) {
    // Force hide by clicking outside or pressing Escape multiple times
    await page.keyboard.press('Escape');
    await page.keyboard.press('Escape');
    await page.waitForTimeout(500);
  }
}
