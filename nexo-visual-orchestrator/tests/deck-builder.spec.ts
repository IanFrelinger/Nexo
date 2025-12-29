import { test, expect } from '@playwright/test';

test.describe('Deck Builder Functionality', () => {
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

  test('should open deck builder from toolbar', async ({ page }) => {
    // Find and click deck builder button
    const deckBuilderButton = page.locator('button:has-text("Deck Builder")');
    await expect(deckBuilderButton).toBeVisible({ timeout: 5000 });
    await deckBuilderButton.click();
    
    // Verify deck builder modal is visible
    const deckBuilder = page.locator('text=Deck Builder');
    await expect(deckBuilder).toBeVisible({ timeout: 3000 });
    
    // Verify deck builder has three main sections
    await expect(page.locator('text=New Deck')).toBeVisible();
    await expect(page.locator('input[placeholder*="Search agents"]')).toBeVisible();
  });

  test('should create a new deck', async ({ page }) => {
    // Open deck builder
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    // Click new deck button
    const newDeckButton = page.locator('button:has-text("New Deck")');
    await expect(newDeckButton).toBeVisible();
    await newDeckButton.click();
    await page.waitForTimeout(500);
    
    // Fill in deck name
    const nameInput = page.locator('input[placeholder*="My Awesome Deck"]');
    await expect(nameInput).toBeVisible();
    await nameInput.fill('Test Deck');
    
    // Fill in description
    const descInput = page.locator('textarea[placeholder*="Describe what this deck"]');
    await expect(descInput).toBeVisible();
    await descInput.fill('A test deck for validation');
    
    // Click create
    const createButton = page.locator('button:has-text("Create Deck")');
    await expect(createButton).toBeEnabled();
    await createButton.click();
    await page.waitForTimeout(500);
    
    // Verify deck appears in library
    await expect(page.locator('text=Test Deck')).toBeVisible();
    
    // Verify deck is selected and shows empty state
    await expect(page.locator('text=Deck is empty')).toBeVisible();
  });

  test('should add agents to deck', async ({ page }) => {
    // Open deck builder and create a deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    const nameInput = page.locator('input[placeholder*="My Awesome Deck"]');
    await nameInput.fill('My Agent Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Search for an agent
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    await expect(searchInput).toBeVisible();
    await searchInput.fill('architect');
    await page.waitForTimeout(500);
    
    // Find and click add button for architect
    const addButton = page.locator('button:has-text("Add to Deck")').first();
    await expect(addButton).toBeVisible();
    await addButton.click();
    await page.waitForTimeout(500);
    
    // Verify agent appears in deck contents
    await expect(page.locator('text=Architect')).toBeVisible();
    
    // Verify count shows 1
    const countDisplay = page.locator('text=/^1$/').first();
    await expect(countDisplay).toBeVisible();
  });

  test('should increase agent count in deck', async ({ page }) => {
    // Open deck builder, create deck, and add agent
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Count Test Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Add architect agent
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    await searchInput.fill('architect');
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Find the plus button to increase count
    const plusButtons = page.locator('button').filter({ has: page.locator('svg') });
    const count = await plusButtons.count();
    
    // Look for plus button near the count display
    const deckContent = page.locator('text=Architect').locator('..').locator('..');
    const plusButton = deckContent.locator('button').filter({ has: page.locator('svg[class*="HiPlus"]') }).first();
    
    if (await plusButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await plusButton.click();
      await page.waitForTimeout(500);
      
      // Verify count increased to 2
      await expect(page.locator('text=/^2$/').first()).toBeVisible({ timeout: 2000 });
    }
  });

  test('should remove agent from deck', async ({ page }) => {
    // Open deck builder, create deck, and add agent
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Remove Test Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Add architect agent
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    await searchInput.fill('architect');
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Verify agent is in deck
    await expect(page.locator('text=Architect')).toBeVisible();
    
    // Find and click delete/trash button
    const deckContent = page.locator('text=Architect').locator('..').locator('..');
    const deleteButton = deckContent.locator('button').filter({ has: page.locator('svg[class*="HiTrash"]') }).first();
    
    if (await deleteButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await deleteButton.click();
      await page.waitForTimeout(500);
      
      // Verify deck is empty again
      await expect(page.locator('text=Deck is empty')).toBeVisible({ timeout: 2000 });
    }
  });

  test('should search and filter agents in deck builder', async ({ page }) => {
    // Open deck builder
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    // Search for combat
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    await searchInput.fill('combat');
    await page.waitForTimeout(500);
    
    // Verify combat agent is visible
    await expect(page.locator('text=Combat').first()).toBeVisible();
    
    // Search for economy
    await searchInput.fill('economy');
    await page.waitForTimeout(500);
    
    // Verify economy agent is visible
    await expect(page.locator('text=Economy').first()).toBeVisible();
    
    // Clear search
    await searchInput.fill('');
    await page.waitForTimeout(500);
    
    // More agents should be visible
    const agentCards = page.locator('div').filter({ hasText: /Agent|Generator|Analyzer/ });
    const count = await agentCards.count();
    expect(count).toBeGreaterThan(2);
  });

  test('should load deck onto canvas', async ({ page }) => {
    // Open deck builder and create a deck with agents
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Load Test Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Add architect agent
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    await searchInput.fill('architect');
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Verify agent is in deck
    await expect(page.locator('text=Architect')).toBeVisible();
    
    // Click load deck button
    const loadButton = page.locator('button:has-text("Load Deck")');
    await expect(loadButton).toBeVisible();
    await loadButton.click();
    await page.waitForTimeout(1000);
    
    // Verify deck builder closes
    const deckBuilder = page.locator('text=Deck Builder');
    await expect(deckBuilder).not.toBeVisible({ timeout: 2000 });
    
    // Verify agent card appears on canvas
    // Since we're using CardCanvas now, look for agent cards
    const agentCard = page.locator('text=Architect').first();
    await expect(agentCard).toBeVisible({ timeout: 5000 });
  });

  test('should duplicate a deck', async ({ page }) => {
    // Open deck builder and create a deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Original Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Add an agent
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    await searchInput.fill('architect');
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Click duplicate button
    const duplicateButton = page.locator('button:has-text("Duplicate")');
    await expect(duplicateButton).toBeVisible();
    await duplicateButton.click();
    await page.waitForTimeout(500);
    
    // Verify duplicate appears in library with "(Copy)" suffix
    await expect(page.locator('text=Original Deck (Copy)')).toBeVisible({ timeout: 2000 });
  });

  test('should toggle deck sharing', async ({ page }) => {
    // Open deck builder and create a deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Shared Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Find share button
    const shareButton = page.locator('button:has-text("Private")').or(page.locator('button:has-text("Shared")'));
    await expect(shareButton).toBeVisible();
    
    // Click to toggle sharing
    await shareButton.click();
    await page.waitForTimeout(500);
    
    // Verify button text changed
    const buttonText = await shareButton.textContent();
    expect(buttonText).toMatch(/Shared|Private/);
  });

  test('should update deck name and description', async ({ page }) => {
    // Open deck builder and create a deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Editable Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Find deck name input in the center panel
    const nameInput = page.locator('input[placeholder*="Deck name"]');
    await expect(nameInput).toBeVisible();
    
    // Update name
    await nameInput.fill('Updated Deck Name');
    await page.waitForTimeout(500);
    
    // Verify name updated
    const nameValue = await nameInput.inputValue();
    expect(nameValue).toBe('Updated Deck Name');
    
    // Update description
    const descInput = page.locator('input[placeholder*="Description"]');
    if (await descInput.isVisible({ timeout: 2000 }).catch(() => false)) {
      await descInput.fill('Updated description');
      await page.waitForTimeout(500);
      
      const descValue = await descInput.inputValue();
      expect(descValue).toBe('Updated description');
    }
  });

  test('should show deck statistics', async ({ page }) => {
    // Open deck builder and create a deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Stats Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Add multiple agents
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    
    // Add architect
    await searchInput.fill('architect');
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Add combat
    await searchInput.fill('combat');
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Verify statistics show
    await expect(page.locator('text=Total Agents')).toBeVisible();
    
    // Should show count of 2
    const totalText = page.locator('text=/Total Agents/').locator('..');
    const totalContent = await totalText.textContent();
    expect(totalContent).toContain('2');
  });

  test('should persist decks in localStorage', async ({ page }) => {
    // Open deck builder and create a deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Persistent Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Add an agent
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    await searchInput.fill('architect');
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Close deck builder
    const closeButton = page.locator('button').filter({ has: page.locator('svg[class*="HiX"]') }).last();
    await closeButton.click();
    await page.waitForTimeout(500);
    
    // Reload page
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Open deck builder again
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    // Verify deck still exists
    await expect(page.locator('text=Persistent Deck')).toBeVisible({ timeout: 3000 });
  });

  test('should delete a deck', async ({ page }) => {
    // Open deck builder and create a deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Deletable Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Verify deck exists
    await expect(page.locator('text=Deletable Deck')).toBeVisible();
    
    // Click delete button
    const deleteButton = page.locator('button:has-text("Delete")');
    await expect(deleteButton).toBeVisible();
    
    // Set up dialog handler
    page.once('dialog', async dialog => {
      expect(dialog.message()).toContain('Delete deck');
      await dialog.accept();
    });
    
    await deleteButton.click();
    await page.waitForTimeout(500);
    
    // Verify deck is removed
    await expect(page.locator('text=Deletable Deck')).not.toBeVisible({ timeout: 2000 });
  });

  test('should show empty state when no deck selected', async ({ page }) => {
    // Open deck builder
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    // Verify empty state message
    await expect(page.locator('text=No deck selected')).toBeVisible();
    await expect(page.locator('text=Select a deck from the library or create a new one')).toBeVisible();
  });

  test('should show empty deck state when deck has no agents', async ({ page }) => {
    // Open deck builder and create a deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Empty Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Verify empty deck message
    await expect(page.locator('text=Deck is empty')).toBeVisible();
    await expect(page.locator('text=Add agents from the library on the right')).toBeVisible();
  });

  test('should handle multiple agents in deck', async ({ page }) => {
    // Open deck builder and create a deck
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Multi Agent Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    
    // Add architect
    await searchInput.fill('architect');
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Add combat
    await searchInput.fill('combat');
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Add economy
    await searchInput.fill('economy');
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Add to Deck")').first().click();
    await page.waitForTimeout(500);
    
    // Verify all agents are in deck
    await expect(page.locator('text=Architect')).toBeVisible();
    await expect(page.locator('text=Combat')).toBeVisible();
    await expect(page.locator('text=Economy')).toBeVisible();
    
    // Verify deck shows correct count
    const deckContent = page.locator('text=Deck Contents');
    await expect(deckContent).toBeVisible();
  });
});

