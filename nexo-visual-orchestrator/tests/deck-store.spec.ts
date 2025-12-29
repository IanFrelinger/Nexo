import { test, expect } from '@playwright/test';

/**
 * Unit tests for deck store functionality
 * These tests validate the deck store operations via the UI
 */
test.describe('Deck Store Operations', () => {
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

  test('should create and store deck in localStorage', async ({ page }) => {
    // Create deck via UI
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Storage Test Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Verify deck is stored in localStorage
    const storedData = await page.evaluate(() => {
      return localStorage.getItem('nexo-deck-store');
    });
    
    expect(storedData).toBeTruthy();
    const parsed = JSON.parse(storedData || '{}');
    expect(parsed.state).toBeDefined();
    expect(parsed.state.decks).toBeDefined();
    expect(parsed.state.decks.length).toBeGreaterThan(0);
    
    const deck = parsed.state.decks.find((d: any) => d.name === 'Storage Test Deck');
    expect(deck).toBeDefined();
    expect(deck.id).toBeDefined();
    expect(deck.createdAt).toBeDefined();
  });

  test('should persist deck with agents across page reloads', async ({ page }) => {
    // Create deck and add agent
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Persistent Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Add agent
    await page.locator('input[placeholder*="Search agents"]').fill('architect');
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Reload page
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Open deck builder
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    // Select deck
    await page.locator('text=Persistent Deck').click();
    await page.waitForTimeout(500);
    
    // Verify agent is still in deck
    await expect(page.locator('text=Architect')).toBeVisible();
    
    // Verify in localStorage
    const storedData = await page.evaluate(() => {
      return localStorage.getItem('nexo-deck-store');
    });
    
    const parsed = JSON.parse(storedData || '{}');
    const deck = parsed.state.decks.find((d: any) => d.name === 'Persistent Deck');
    expect(deck).toBeDefined();
    expect(deck.agents.length).toBe(1);
    expect(deck.agents[0].agentType).toBe('architect');
  });

  test('should update deck metadata', async ({ page }) => {
    // Create deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Update Test Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Update name
    const nameInput = page.locator('input[placeholder*="Deck name"]');
    await nameInput.fill('Updated Name');
    await page.waitForTimeout(500);
    
    // Verify update in localStorage
    const storedData = await page.evaluate(() => {
      return localStorage.getItem('nexo-deck-store');
    });
    
    const parsed = JSON.parse(storedData || '{}');
    const deck = parsed.state.decks.find((d: any) => d.name === 'Updated Name');
    expect(deck).toBeDefined();
    expect(deck.updatedAt).toBeDefined();
  });

  test('should handle deck deletion from store', async ({ page }) => {
    // Create deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Delete Test Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Get deck ID from localStorage
    const deckId = await page.evaluate(() => {
      const stored = localStorage.getItem('nexo-deck-store');
      if (!stored) return null;
      const parsed = JSON.parse(stored);
      const deck = parsed.state.decks.find((d: any) => d.name === 'Delete Test Deck');
      return deck?.id || null;
    });
    
    expect(deckId).toBeTruthy();
    
    // Delete deck
    page.once('dialog', async dialog => {
      await dialog.accept();
    });
    
    await page.locator('button:has-text("Delete")').click();
    await page.waitForTimeout(500);
    
    // Verify deletion in localStorage
    const storedData = await page.evaluate(() => {
      return localStorage.getItem('nexo-deck-store');
    });
    
    const parsed = JSON.parse(storedData || '{}');
    const deck = parsed.state.decks.find((d: any) => d.id === deckId);
    expect(deck).toBeUndefined();
  });

  test('should handle multiple decks in store', async ({ page }) => {
    // Create first deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Deck 1');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Close modal and create second deck
    const closeButton = page.locator('button').filter({ has: page.locator('svg[class*="HiX"]') }).last();
    await closeButton.click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Deck 2');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Verify both decks in localStorage
    const storedData = await page.evaluate(() => {
      return localStorage.getItem('nexo-deck-store');
    });
    
    const parsed = JSON.parse(storedData || '{}');
    expect(parsed.state.decks.length).toBeGreaterThanOrEqual(2);
    
    const deck1 = parsed.state.decks.find((d: any) => d.name === 'Deck 1');
    const deck2 = parsed.state.decks.find((d: any) => d.name === 'Deck 2');
    
    expect(deck1).toBeDefined();
    expect(deck2).toBeDefined();
    expect(deck1.id).not.toBe(deck2.id);
  });

  test('should maintain deck state when switching between decks', async ({ page }) => {
    // Create two decks
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    // Create first deck
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Deck A');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Add agent to Deck A
    await page.locator('input[placeholder*="Search agents"]').fill('architect');
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Create second deck
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Deck B');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Add different agent to Deck B
    await page.locator('input[placeholder*="Search agents"]').fill('combat');
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Switch back to Deck A
    await page.locator('text=Deck A').click();
    await page.waitForTimeout(500);
    
    // Verify Deck A still has architect
    await expect(page.locator('text=Architect')).toBeVisible();
    await expect(page.locator('text=Combat')).not.toBeVisible();
    
    // Switch to Deck B
    await page.locator('text=Deck B').click();
    await page.waitForTimeout(500);
    
    // Verify Deck B has combat
    await expect(page.locator('text=Combat')).toBeVisible();
    await expect(page.locator('text=Architect')).not.toBeVisible();
  });
});

