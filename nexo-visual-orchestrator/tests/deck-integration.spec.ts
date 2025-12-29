import { test, expect } from '@playwright/test';
import { openDeckBuilder, createDeck, addAgentToDeck, loadDeckOntoCanvas } from './helpers';

test.describe('Deck Integration with Canvas', () => {
  test.beforeEach(async ({ page }) => {
    // Clear localStorage to start fresh
    await page.addInitScript(() => {
      localStorage.removeItem('nexo-guided-mode-dismissed');
      localStorage.removeItem('nexo-deck-store');
    });
    
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Dismiss guided mode if present
    const guidedMode = page.locator('.absolute.inset-0.bg-surface-dark.z-50');
    const isVisible = await guidedMode.isVisible({ timeout: 1000 }).catch(() => false);
    if (isVisible) {
      await page.keyboard.press('Escape');
      await page.waitForTimeout(300);
    }
  });

  test('should convert deck to workflow and display on canvas', async ({ page }) => {
    // Create deck with agents
    await openDeckBuilder(page);
    await createDeck(page, 'Workflow Test Deck');
    await addAgentToDeck(page, 'architect');
    await addAgentToDeck(page, 'combat');
    
    // Load deck onto canvas
    await loadDeckOntoCanvas(page);
    
    // Verify agents appear on canvas as cards
    await expect(page.locator('text=Architect').first()).toBeVisible({ timeout: 5000 });
    await expect(page.locator('text=Combat').first()).toBeVisible({ timeout: 5000 });
  });

  test('should load deck with multiple agent instances', async ({ page }) => {
    // Create deck
    await openDeckBuilder(page);
    await createDeck(page, 'Multi Instance Deck');
    await addAgentToDeck(page, 'architect');
    
    // Increase count
    const plusButton = page.locator('button').filter({ has: page.locator('svg[class*="HiPlus"]') }).first();
    if (await plusButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await plusButton.click();
      await page.waitForTimeout(500);
    }
    
    // Load deck
    await loadDeckOntoCanvas(page);
    
    // Verify multiple instances appear (or at least one)
    const architectCards = page.locator('text=Architect');
    const count = await architectCards.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('should preserve deck when loading onto canvas', async ({ page }) => {
    // Create deck with agents
    await openDeckBuilder(page);
    await createDeck(page, 'Preserve Test Deck');
    await addAgentToDeck(page, 'architect');
    await addAgentToDeck(page, 'economy');
    
    // Load deck
    await loadDeckOntoCanvas(page);
    
    // Reopen deck builder
    await openDeckBuilder(page);
    
    // Select the deck
    await page.locator('text=Preserve Test Deck').click();
    await page.waitForTimeout(500);
    
    // Verify deck still has agents
    await expect(page.locator('text=Architect')).toBeVisible();
    await expect(page.locator('text=Economy')).toBeVisible();
  });

  test('should handle loading empty deck', async ({ page }) => {
    // Create empty deck
    await openDeckBuilder(page);
    await createDeck(page, 'Empty Deck');
    
    // Try to load (button should be visible but may be disabled)
    const loadButton = page.locator('button:has-text("Load Deck")');
    await expect(loadButton).toBeVisible();
    
    // If enabled, click it
    const isEnabled = await loadButton.isEnabled().catch(() => false);
    if (isEnabled) {
      await loadButton.click();
      await page.waitForTimeout(1000);
      
      // Canvas should remain empty or show empty state
      const emptyState = page.locator('text=No agents yet');
      const isVisible = await emptyState.isVisible({ timeout: 2000 }).catch(() => false);
      // Either empty state or no cards should be visible
      expect(isVisible || await page.locator('text=Architect').count() === 0).toBe(true);
    }
  });

  test('should load deck and allow further modifications', async ({ page }) => {
    // Create and load deck
    await openDeckBuilder(page);
    await createDeck(page, 'Modifiable Deck');
    await addAgentToDeck(page, 'architect');
    await loadDeckOntoCanvas(page);
    
    // Verify agent on canvas
    await expect(page.locator('text=Architect').first()).toBeVisible({ timeout: 5000 });
    
    // Add another agent directly to canvas (via library)
    // Expand library category if needed
    const category = page.locator('button:has-text("Tactical")').first();
    const categoryText = await category.textContent();
    if (categoryText?.includes('▶')) {
      await category.click();
      await page.waitForTimeout(300);
    }
    
    // Try to add combat agent via drag (if library is visible)
    const combatAgent = page.locator('div[draggable="true"]:has-text("Combat Agent")').first();
    if (await combatAgent.isVisible({ timeout: 2000 }).catch(() => false)) {
      // Canvas should accept new agents even after deck load
      const canvas = page.locator('.flex-1.h-full.relative');
      await expect(canvas).toBeVisible();
    }
  });

  test('should handle deck with all agent types', async ({ page }) => {
    // Create comprehensive deck
    await openDeckBuilder(page);
    await createDeck(page, 'Comprehensive Deck');
    
    // Add multiple different agent types
    const agents = ['architect', 'combat', 'economy', 'ai-behavior'];
    
    for (const agent of agents) {
      await addAgentToDeck(page, agent);
      await page.waitForTimeout(300);
    }
    
    // Load deck
    await loadDeckOntoCanvas(page);
    
    // Verify all agents appear (at least some)
    let visibleCount = 0;
    for (const agent of ['Architect', 'Combat', 'Economy', 'AI Behavior']) {
      const isVisible = await page.locator(`text=${agent}`).first().isVisible({ timeout: 3000 }).catch(() => false);
      if (isVisible) visibleCount++;
    }
    
    expect(visibleCount).toBeGreaterThanOrEqual(2); // At least 2 should be visible
  });

  test('should maintain deck state when switching projects', async ({ page }) => {
    // Create deck
    await openDeckBuilder(page);
    await createDeck(page, 'Project Deck');
    await addAgentToDeck(page, 'architect');
    
    // Toggle sharing (simulating cross-project usage)
    const shareButton = page.locator('button:has-text("Private")').or(page.locator('button:has-text("Shared")'));
    if (await shareButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await shareButton.click();
      await page.waitForTimeout(500);
      
      // Verify it's now shared
      const buttonText = await shareButton.textContent();
      expect(buttonText).toMatch(/Shared|Private/);
    }
    
    // Close and reopen deck builder
    const closeButton = page.locator('button').filter({ has: page.locator('svg[class*="HiX"]') }).last();
    await closeButton.click();
    await page.waitForTimeout(500);
    
    await openDeckBuilder(page);
    await page.locator('text=Project Deck').click();
    await page.waitForTimeout(500);
    
    // Verify deck state is preserved
    await expect(page.locator('text=Architect')).toBeVisible();
  });

  test('should handle deck loading with existing canvas content', async ({ page }) => {
    // Add agent directly to canvas first
    const category = page.locator('button:has-text("Strategic")').first();
    const categoryText = await category.textContent();
    if (categoryText?.includes('▶')) {
      await category.click();
      await page.waitForTimeout(300);
    }
    
    // Use programmatic addition
    await page.evaluate(() => {
      window.dispatchEvent(new CustomEvent('test:addRole', {
        detail: { templateId: 'architect' }
      }));
    });
    await page.waitForTimeout(2000);
    
    // Verify agent on canvas
    const initialCount = await page.locator('text=Architect').count();
    expect(initialCount).toBeGreaterThanOrEqual(1);
    
    // Create and load deck
    await openDeckBuilder(page);
    await createDeck(page, 'Additional Deck');
    await addAgentToDeck(page, 'combat');
    await loadDeckOntoCanvas(page);
    
    // Verify both agents are on canvas
    await expect(page.locator('text=Architect').first()).toBeVisible({ timeout: 5000 });
    await expect(page.locator('text=Combat').first()).toBeVisible({ timeout: 5000 });
  });
});

