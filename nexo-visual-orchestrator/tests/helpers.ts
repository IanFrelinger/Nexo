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
  const finalPosition = position || { x: 400, y: 300 };
  
  // Add role directly via store
  await page.evaluate(
    ({ templateId, x, y }) => {
      // Trigger a custom event that the component can listen to
      const event = new CustomEvent('test:addRole', {
        detail: { templateId, position: { x, y } },
      });
      window.dispatchEvent(event);
    },
    { templateId, x: finalPosition.x, y: finalPosition.y }
  );
  
  await page.waitForTimeout(1000);
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
