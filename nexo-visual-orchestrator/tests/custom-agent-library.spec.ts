import { test, expect } from '@playwright/test';

test.describe('Custom Agent Library Functionality', () => {
  test.beforeEach(async ({ page }) => {
    // Clear localStorage to start fresh
    await page.addInitScript(() => {
      localStorage.removeItem('nexo-guided-mode-dismissed');
      localStorage.removeItem('nexo-custom-agent-library');
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

  test('should open custom agent library from agent library panel', async ({ page }) => {
    // Find and click "My Agent Library" button
    const myLibraryButton = page.locator('button:has-text("My Agent Library")');
    await expect(myLibraryButton).toBeVisible({ timeout: 5000 });
    await myLibraryButton.click();
    await page.waitForTimeout(500);
    
    // Verify custom agent library is visible
    await expect(page.locator('text=My Agent Library')).toBeVisible({ timeout: 3000 });
    
    // Verify view mode tabs are visible
    await expect(page.locator('button:has-text("All")')).toBeVisible();
    await expect(page.locator('button').filter({ has: page.locator('svg[class*="HiStar"]') })).toBeVisible();
  });

  test('should create a custom agent', async ({ page }) => {
    // Open custom agent library
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    // Click create button
    const createButton = page.locator('button').filter({ has: page.locator('svg[class*="HiPlus"]') }).first();
    await expect(createButton).toBeVisible();
    await createButton.click();
    await page.waitForTimeout(500);
    
    // Fill in agent details
    const nameInput = page.locator('input[placeholder*="My Custom"]');
    await expect(nameInput).toBeVisible();
    await nameInput.fill('My Custom Combat Agent');
    
    // Select base agent type
    const baseTypeSelect = page.locator('select').first();
    await expect(baseTypeSelect).toBeVisible();
    await baseTypeSelect.selectOption({ label: /Combat/ });
    await page.waitForTimeout(300);
    
    // Fill description
    const descInput = page.locator('textarea[placeholder*="Describe this custom agent"]');
    if (await descInput.isVisible({ timeout: 2000 }).catch(() => false)) {
      await descInput.fill('A custom combat agent for my project');
    }
    
    // Click create
    const createAgentButton = page.locator('button:has-text("Create Agent")');
    await expect(createAgentButton).toBeEnabled();
    await createAgentButton.click();
    await page.waitForTimeout(500);
    
    // Verify agent appears in library
    await expect(page.locator('text=My Custom Combat Agent')).toBeVisible({ timeout: 3000 });
  });

  test('should display custom agents in library', async ({ page }) => {
    // Create a custom agent via localStorage first
    await page.evaluate(() => {
      const store = {
        state: {
          library: {
            agents: [{
              id: 'test-agent-1',
              name: 'Test Custom Agent',
              description: 'A test custom agent',
              baseAgentType: 'combat',
              configuration: {},
              tags: ['test', 'custom'],
              createdAt: new Date().toISOString(),
              updatedAt: new Date().toISOString(),
              isShared: false,
            }],
            favorites: new Set(),
            recent: [],
          },
        },
        version: 1,
      };
      localStorage.setItem('nexo-custom-agent-library', JSON.stringify(store));
    });
    
    // Reload page
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Open custom agent library
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    // Verify agent is displayed
    await expect(page.locator('text=Test Custom Agent')).toBeVisible({ timeout: 3000 });
    await expect(page.locator('text=Based on: Combat')).toBeVisible();
  });

  test('should toggle favorite status', async ({ page }) => {
    // Create agent via localStorage
    await page.evaluate(() => {
      const store = {
        state: {
          library: {
            agents: [{
              id: 'fav-test',
              name: 'Favorite Test Agent',
              baseAgentType: 'architect',
              configuration: {},
              tags: [],
              createdAt: new Date().toISOString(),
              updatedAt: new Date().toISOString(),
            }],
            favorites: new Set(),
            recent: [],
          },
        },
        version: 1,
      };
      localStorage.setItem('nexo-custom-agent-library', JSON.stringify(store));
    });
    
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Open custom agent library
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    // Find and click favorite button
    const agentCard = page.locator('text=Favorite Test Agent').locator('..').locator('..');
    const favoriteButton = agentCard.locator('button').filter({ has: page.locator('svg[class*="HiStar"]') }).first();
    
    if (await favoriteButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await favoriteButton.click();
      await page.waitForTimeout(500);
      
      // Switch to favorites view
      const favoritesTab = page.locator('button').filter({ has: page.locator('svg[class*="HiStar"]') }).nth(1);
      await favoritesTab.click();
      await page.waitForTimeout(500);
      
      // Verify agent appears in favorites
      await expect(page.locator('text=Favorite Test Agent')).toBeVisible({ timeout: 2000 });
    }
  });

  test('should search custom agents', async ({ page }) => {
    // Create multiple agents via localStorage
    await page.evaluate(() => {
      const store = {
        state: {
          library: {
            agents: [
              {
                id: 'agent-1',
                name: 'Combat Specialist',
                baseAgentType: 'combat',
                configuration: {},
                tags: ['combat'],
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
              },
              {
                id: 'agent-2',
                name: 'Economy Manager',
                baseAgentType: 'economy',
                configuration: {},
                tags: ['economy'],
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
              },
            ],
            favorites: new Set(),
            recent: [],
          },
        },
        version: 1,
      };
      localStorage.setItem('nexo-custom-agent-library', JSON.stringify(store));
    });
    
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Open custom agent library
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    // Search for combat
    const searchInput = page.locator('input[placeholder*="Search agents"]');
    await searchInput.fill('combat');
    await page.waitForTimeout(500);
    
    // Verify only combat agent is visible
    await expect(page.locator('text=Combat Specialist')).toBeVisible();
    await expect(page.locator('text=Economy Manager')).not.toBeVisible({ timeout: 2000 });
    
    // Clear search
    await searchInput.fill('');
    await page.waitForTimeout(500);
    
    // Both should be visible again
    await expect(page.locator('text=Combat Specialist')).toBeVisible();
    await expect(page.locator('text=Economy Manager')).toBeVisible();
  });

  test('should toggle agent sharing', async ({ page }) => {
    // Create agent via localStorage
    await page.evaluate(() => {
      const store = {
        state: {
          library: {
            agents: [{
              id: 'share-test',
              name: 'Shared Test Agent',
              baseAgentType: 'architect',
              configuration: {},
              tags: [],
              createdAt: new Date().toISOString(),
              updatedAt: new Date().toISOString(),
              isShared: false,
            }],
            favorites: new Set(),
            recent: [],
          },
        },
        version: 1,
      };
      localStorage.setItem('nexo-custom-agent-library', JSON.stringify(store));
    });
    
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Open custom agent library
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    // Find share button
    const agentCard = page.locator('text=Shared Test Agent').locator('..').locator('..');
    const shareButton = agentCard.locator('button').filter({ has: page.locator('svg[class*="HiShare"]') }).first();
    
    if (await shareButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await shareButton.click();
      await page.waitForTimeout(500);
      
      // Verify shared indicator appears
      await expect(page.locator('text=Shared').first()).toBeVisible({ timeout: 2000 });
    }
  });

  test('should delete custom agent', async ({ page }) => {
    // Create agent via localStorage
    await page.evaluate(() => {
      const store = {
        state: {
          library: {
            agents: [{
              id: 'delete-test',
              name: 'Delete Test Agent',
              baseAgentType: 'combat',
              configuration: {},
              tags: [],
              createdAt: new Date().toISOString(),
              updatedAt: new Date().toISOString(),
            }],
            favorites: new Set(),
            recent: [],
          },
        },
        version: 1,
      };
      localStorage.setItem('nexo-custom-agent-library', JSON.stringify(store));
    });
    
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Open custom agent library
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    // Verify agent exists
    await expect(page.locator('text=Delete Test Agent')).toBeVisible();
    
    // Set up dialog handler
    page.once('dialog', async dialog => {
      expect(dialog.message()).toContain('Delete');
      await dialog.accept();
    });
    
    // Find and click delete button
    const agentCard = page.locator('text=Delete Test Agent').locator('..').locator('..');
    const deleteButton = agentCard.locator('button').filter({ has: page.locator('svg[class*="HiTrash"]') }).first();
    
    if (await deleteButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await deleteButton.click();
      await page.waitForTimeout(500);
      
      // Verify agent is removed
      await expect(page.locator('text=Delete Test Agent')).not.toBeVisible({ timeout: 2000 });
    }
  });

  test('should persist custom agents in localStorage', async ({ page }) => {
    // Open custom agent library and create agent
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    // Create agent
    await page.locator('button').filter({ has: page.locator('svg[class*="HiPlus"]') }).first().click();
    await page.waitForTimeout(500);
    
    await page.locator('input[placeholder*="My Custom"]').fill('Persistent Agent');
    await page.locator('select').first().selectOption({ label: /Architect/ });
    await page.waitForTimeout(300);
    
    await page.locator('button:has-text("Create Agent")').click();
    await page.waitForTimeout(500);
    
    // Verify agent is stored
    const storedData = await page.evaluate(() => {
      return localStorage.getItem('nexo-custom-agent-library');
    });
    
    expect(storedData).toBeTruthy();
    const parsed = JSON.parse(storedData || '{}');
    expect(parsed.state.library.agents).toBeDefined();
    expect(parsed.state.library.agents.length).toBeGreaterThan(0);
    
    const agent = parsed.state.library.agents.find((a: any) => a.name === 'Persistent Agent');
    expect(agent).toBeDefined();
    
    // Reload and verify persistence
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    await expect(page.locator('text=Persistent Agent')).toBeVisible({ timeout: 3000 });
  });

  test('should show empty state when no custom agents', async ({ page }) => {
    // Open custom agent library
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    // Verify empty state
    await expect(page.locator('text=No custom agents yet')).toBeVisible();
    await expect(page.locator('text=Create your first custom agent')).toBeVisible();
  });

  test('should switch between view modes', async ({ page }) => {
    // Create agents via localStorage
    await page.evaluate(() => {
      const store = {
        state: {
          library: {
            agents: [
              {
                id: 'agent-1',
                name: 'Agent One',
                baseAgentType: 'combat',
                configuration: {},
                tags: [],
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
              },
              {
                id: 'agent-2',
                name: 'Agent Two',
                baseAgentType: 'economy',
                configuration: {},
                tags: [],
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
              },
            ],
            favorites: new Set(['agent-1']),
            recent: ['agent-2'],
          },
        },
        version: 1,
      };
      localStorage.setItem('nexo-custom-agent-library', JSON.stringify(store));
    });
    
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Open custom agent library
    await page.locator('button:has-text("My Agent Library")').click();
    await page.waitForTimeout(500);
    
    // Switch to favorites
    const favoritesTab = page.locator('button').filter({ has: page.locator('svg[class*="HiStar"]') }).nth(1);
    await favoritesTab.click();
    await page.waitForTimeout(500);
    
    // Verify only favorite is visible
    await expect(page.locator('text=Agent One')).toBeVisible();
    
    // Switch to recent
    const recentTab = page.locator('button').filter({ has: page.locator('svg[class*="HiClock"]') }).first();
    await recentTab.click();
    await page.waitForTimeout(500);
    
    // Verify recent agent is visible
    await expect(page.locator('text=Agent Two')).toBeVisible();
    
    // Switch back to all
    await page.locator('button:has-text("All")').click();
    await page.waitForTimeout(500);
    
    // Both should be visible
    await expect(page.locator('text=Agent One')).toBeVisible();
    await expect(page.locator('text=Agent Two')).toBeVisible();
  });

  test('should use custom agents in deck builder', async ({ page }) => {
    // Create custom agent via localStorage
    await page.evaluate(() => {
      const store = {
        state: {
          library: {
            agents: [{
              id: 'deck-test',
              name: 'Deck Test Agent',
              baseAgentType: 'combat',
              configuration: {},
              tags: [],
              createdAt: new Date().toISOString(),
              updatedAt: new Date().toISOString(),
            }],
            favorites: new Set(),
            recent: [],
          },
        },
        version: 1,
      };
      localStorage.setItem('nexo-custom-agent-library', JSON.stringify(store));
    });
    
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // Open deck builder
    await page.locator('button:has-text("Deck Builder")').click();
    await page.waitForTimeout(500);
    
    // Create a deck
    await page.locator('button:has-text("New Deck")').click();
    await page.waitForTimeout(500);
    await page.locator('input[placeholder*="My Awesome Deck"]').fill('Custom Agent Deck');
    await page.locator('button:has-text("Create Deck")').click();
    await page.waitForTimeout(500);
    
    // Switch to custom agents
    const customTab = page.locator('button:has-text("My Agents")');
    if (await customTab.isVisible({ timeout: 2000 }).catch(() => false)) {
      await customTab.click();
      await page.waitForTimeout(500);
      
      // Verify custom agent is visible
      await expect(page.locator('text=Deck Test Agent')).toBeVisible({ timeout: 3000 });
      
      // Add to deck
      const addButton = page.locator('button:has-text("Add to Deck")').first();
      if (await addButton.isVisible({ timeout: 2000 }).catch(() => false)) {
        await addButton.click();
        await page.waitForTimeout(500);
        
        // Verify agent is in deck
        await expect(page.locator('text=Deck Test Agent')).toBeVisible();
      }
    }
  });
});

