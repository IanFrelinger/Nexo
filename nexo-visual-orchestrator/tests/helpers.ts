import { Page, expect } from '@playwright/test';

/**
 * Helper function to drag and drop an agent onto the React Flow canvas
 * Uses mouse events instead of dragTo to avoid React Flow pane interception
 */
export async function dragAgentToCanvas(
  page: Page,
  agentSelector: string,
  targetX?: number,
  targetY?: number
) {
  const agent = page.locator(agentSelector).first();
  await expect(agent).toBeVisible({ timeout: 5000 });
  
  // Try multiple canvas selectors
  let canvas = page.locator('.react-flow__viewport');
  let canvasBox = await canvas.boundingBox();
  
  // Fallback to main react-flow container if viewport doesn't work
  if (!canvasBox || isNaN(canvasBox.left) || isNaN(canvasBox.top)) {
    canvas = page.locator('.react-flow');
    await expect(canvas).toBeVisible({ timeout: 5000 });
    canvasBox = await canvas.boundingBox();
  }
  
  // If still no valid box, use viewport size
  if (!canvasBox || isNaN(canvasBox.left) || isNaN(canvasBox.top)) {
    const viewport = page.viewportSize();
    if (viewport) {
      canvasBox = {
        left: 0,
        top: 0,
        width: viewport.width,
        height: viewport.height,
        x: 0,
        y: 0,
      };
    } else {
      throw new Error('Could not determine canvas bounds');
    }
  }
  
  const agentBox = await agent.boundingBox();
  if (!agentBox || isNaN(agentBox.x) || isNaN(agentBox.y)) {
    // Scroll agent into view and retry
    await agent.scrollIntoViewIfNeeded();
    await page.waitForTimeout(500);
    const retryBox = await agent.boundingBox();
    if (!retryBox || isNaN(retryBox.x) || isNaN(retryBox.y)) {
      throw new Error('Could not get valid agent bounding box');
    }
    Object.assign(agentBox, retryBox);
  }
  
  // Use target coordinates or default to canvas center
  const finalX = targetX ?? (canvasBox.left || 0) + (canvasBox.width || 800) / 2;
  const finalY = targetY ?? (canvasBox.top || 0) + (canvasBox.height || 600) / 2;
  
  // Start drag from agent center
  const startX = (agentBox.x || 0) + (agentBox.width || 0) / 2;
  const startY = (agentBox.y || 0) + (agentBox.height || 0) / 2;
  
  // Validate final coordinates
  if (isNaN(finalX) || isNaN(finalY) || isNaN(startX) || isNaN(startY)) {
    throw new Error(`Invalid drag coordinates: start=(${startX}, ${startY}), end=(${finalX}, ${finalY})`);
  }
  
  // Ensure coordinates are within viewport
  const viewportSize = page.viewportSize();
  if (viewportSize) {
    if (startX < 0 || startX > viewportSize.width || startY < 0 || startY > viewportSize.height) {
      // Scroll agent into view if needed
      await agent.scrollIntoViewIfNeeded();
      await page.waitForTimeout(200);
      const newAgentBox = await agent.boundingBox();
      if (newAgentBox) {
        const newStartX = newAgentBox.x + newAgentBox.width / 2;
        const newStartY = newAgentBox.y + newAgentBox.height / 2;
        await page.mouse.move(newStartX, newStartY);
      } else {
        await page.mouse.move(startX, startY);
      }
    } else {
      await page.mouse.move(startX, startY);
    }
  } else {
    await page.mouse.move(startX, startY);
  }
  
  await page.mouse.down();
  await page.mouse.move(Math.round(finalX), Math.round(finalY), { steps: 10 });
  await page.mouse.up();
  
  // Wait for React Flow to process
  await page.waitForTimeout(1500);
}

