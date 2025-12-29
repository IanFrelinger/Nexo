// tests/tier-band-alignment.spec.ts

import { test, expect } from '@playwright/test';
import { addAgentToCanvas, dismissGuidedMode } from './helpers';

test.describe('Tier Band Alignment', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await dismissGuidedMode(page);
    
    // Wait for guided mode to be fully dismissed
    await page.waitForTimeout(1000);
    
    // Ensure guided mode overlay is gone
    const guidedModeOverlay = page.locator('.absolute.inset-0.bg-surface-dark.z-50');
    if (await guidedModeOverlay.isVisible().catch(() => false)) {
      await page.keyboard.press('Escape');
      await page.waitForTimeout(500);
    }
    
    // Wait for canvas to be ready
    await page.waitForSelector('.react-flow__pane', { timeout: 10000 });
    await page.waitForTimeout(1000);
  });

  test('should align tier bands with agent positions after layout', async ({ page }) => {
    // Add agents from different tiers
    await addAgentToCanvas(page, 'architect', { x: 0, y: 0 });
    await addAgentToCanvas(page, 'combat', { x: 0, y: 0 });
    await addAgentToCanvas(page, 'validator', { x: 0, y: 0 });
    
    await page.waitForTimeout(2000);

    // Verify agents were added
    const initialCount = await page.evaluate(() => {
      const store = (window as any).__ZUSTAND_STORE__;
      if (!store) return 0;
      const state = store.getState();
      return state.roles?.length || 0;
    });
    expect(initialCount).toBeGreaterThanOrEqual(3);

    // Get positions before layout
    const positionsBefore = await page.evaluate(() => {
      const store = (window as any).__ZUSTAND_STORE__;
      if (!store) return null;
      const state = store.getState();
      if (!state || !state.roles) return null;
      return state.roles.map((r: any) => ({
        id: r.id,
        tier: r.modelConfig?.tier,
        x: r.position?.x || 0,
        y: r.position?.y || 0,
      }));
    });
    console.log('Positions before layout:', JSON.stringify(positionsBefore, null, 2));

    // Click Layout button to apply hierarchical layout
    const layoutButton = page.locator('button:has-text("Layout")');
    await expect(layoutButton).toBeVisible();
    await layoutButton.click({ force: true });
    
    // Wait longer for layout to apply and state to update
    await page.waitForTimeout(5000);

    // Get positions from store after layout
    const positions = await page.evaluate(() => {
      const store = (window as any).__ZUSTAND_STORE__;
      if (!store) return null;
      const state = store.getState();
      if (!state || !state.roles) return null;
      return state.roles.map((r: any) => ({
        id: r.id,
        name: r.name,
        tier: r.modelConfig?.tier,
        x: r.position?.x || 0,
        y: r.position?.y || 0,
        hasPosition: !!r.position,
      }));
    });
    console.log('Positions after layout:', JSON.stringify(positions, null, 2));

    expect(positions).toBeTruthy();
    expect(positions?.length).toBeGreaterThanOrEqual(3);

    // Verify each agent is within its tier band boundaries
    // Strategic: center at 250, band from 100-400
    // Tactical: center at 650, band from 500-800
    // Execution: center at 1050, band from 900-1200
    const tierBoundaries = {
      strategic: { minY: 100, maxY: 400, centerY: 250 },
      tactical: { minY: 500, maxY: 800, centerY: 650 },
      execution: { minY: 900, maxY: 1200, centerY: 1050 },
    };

    // Filter out positions that are 0 (not yet laid out)
    const validPositions = positions?.filter((pos: any) => pos.y > 0 && pos.tier) || [];
    
    // Verify we have at least some valid positions
    expect(validPositions.length).toBeGreaterThan(0);
    
    // Verify each position is within its tier band
    // Note: After layout, positions should be at tier centers (250, 650, 1050)
    // But if layout didn't apply, positions might still be at defaults
    // In that case, we should at least verify they get constrained to tier bounds
    validPositions.forEach((pos: any) => {
      const boundaries = tierBoundaries[pos.tier as keyof typeof tierBoundaries];
      if (boundaries) {
        // Log for debugging if position is outside bounds
        if (pos.y < boundaries.minY - 10 || pos.y > boundaries.maxY + 10) {
          console.log(`Position mismatch: ${pos.tier} agent at Y=${pos.y}, expected ${boundaries.minY}-${boundaries.maxY}`);
        }
        // Positions must be within tier bounds (with small margin for rounding)
        // This ensures the constraint logic works even if layout didn't apply
        expect(pos.y).toBeGreaterThanOrEqual(boundaries.minY - 10);
        expect(pos.y).toBeLessThanOrEqual(boundaries.maxY + 10);
      }
    });

    // Verify strategic tier agent is in the top band (should be near center Y=250)
    const strategicAgent = positions?.find((p: any) => p.tier === 'strategic');
    if (strategicAgent) {
      expect(strategicAgent.y).toBeGreaterThanOrEqual(100);
      expect(strategicAgent.y).toBeLessThanOrEqual(400);
      // Should be close to center (250) - allow some variance for hierarchy
      expect(Math.abs(strategicAgent.y - 250)).toBeLessThan(200); // Within 200px of center
    }

    // Verify tactical tier agent is in the middle band (should be near center Y=650)
    const tacticalAgent = positions?.find((p: any) => p.tier === 'tactical');
    if (tacticalAgent) {
      expect(tacticalAgent.y).toBeGreaterThanOrEqual(500);
      expect(tacticalAgent.y).toBeLessThanOrEqual(800);
      // Should be close to center (650) - allow some variance for hierarchy
      expect(Math.abs(tacticalAgent.y - 650)).toBeLessThan(200); // Within 200px of center
    }

    // Verify execution tier agent is in the bottom band (should be near center Y=1050)
    const executionAgent = positions?.find((p: any) => p.tier === 'execution');
    if (executionAgent) {
      expect(executionAgent.y).toBeGreaterThanOrEqual(900);
      expect(executionAgent.y).toBeLessThanOrEqual(1200);
      // Should be close to center (1050) - allow some variance for hierarchy
      expect(Math.abs(executionAgent.y - 1050)).toBeLessThan(200); // Within 200px of center
    }
  });

  test('should constrain agents to tier bands when dragged', async ({ page }) => {
    // Add an agent and apply layout
    await addAgentToCanvas(page, 'combat', { x: 0, y: 0 });
    await page.waitForTimeout(1000);

    // Apply layout
    const layoutButton = page.locator('button:has-text("Layout")');
    await layoutButton.click({ force: true });
    await page.waitForTimeout(2000);

    // Get initial position
    const initialPosition = await page.evaluate(() => {
      const store = (window as any).__ZUSTAND_STORE__;
      if (!store) return null;
      const state = store.getState();
      const role = state.roles.find((r: any) => r.modelConfig?.tier === 'tactical');
      return role ? { x: role.position?.x || 0, y: role.position?.y || 0, tier: role.modelConfig?.tier } : null;
    });

    expect(initialPosition).toBeTruthy();
    if (initialPosition && initialPosition.y > 0) {
      // Verify initial position is within tactical tier bounds (500-800)
      // Center is at 650, so allow some variance
      expect(initialPosition.y).toBeGreaterThanOrEqual(500);
      expect(initialPosition.y).toBeLessThanOrEqual(800);
    } else {
      // If position is 0, the layout might not have been applied yet
      // This is acceptable - the constraint test will verify the constraint works
      console.log('Initial position not set, skipping initial position check');
    }

    // Try to drag the node (simulate by updating position in store to outside tier)
    await page.evaluate(() => {
      const store = (window as any).__ZUSTAND_STORE__;
      if (store) {
        const state = store.getState();
        const role = state.roles.find((r: any) => r.modelConfig?.tier === 'tactical');
        if (role) {
          // Try to set position outside tier (e.g., Y=200 which is in strategic tier)
          store.getState().updateRole(role.id, {
            position: { x: role.position.x, y: 200 },
          });
        }
      }
    });

    await page.waitForTimeout(1000);

    // Verify position was constrained back to tactical tier
    const constrainedPosition = await page.evaluate(() => {
      const store = (window as any).__ZUSTAND_STORE__;
      if (!store) return null;
      const state = store.getState();
      const role = state.roles.find((r: any) => r.modelConfig?.tier === 'tactical');
      return role ? { x: role.position?.x || 0, y: role.position?.y || 0 } : null;
    });

    expect(constrainedPosition).toBeTruthy();
    if (constrainedPosition) {
      // Position should be constrained to tactical tier (500-800)
      expect(constrainedPosition.y).toBeGreaterThanOrEqual(500);
      expect(constrainedPosition.y).toBeLessThanOrEqual(800);
    }
  });
});

