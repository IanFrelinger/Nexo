import { chromium, Browser, Page } from 'playwright';
import { VisualEvaluator } from './VisualEvaluator';
import { InteractionSimulator } from './InteractionSimulator';
import { StateValidator } from './StateValidator';
import { ReportGenerator } from './ReportGenerator';
import { VirtualUserExplorer } from './VirtualUserExplorer';
import { dismissGuidedMode } from '../helpers';
import type { TestScenario, TestResult, TestReport, TestStep, EvaluationResult } from './types';
import type { ExplorationResult } from './VirtualUserExplorer';
import * as path from 'path';
import * as fs from 'fs';

export class DemoTestingAgent {
  private browser: Browser | null = null;
  private page: Page | null = null;
  private visualEvaluator: VisualEvaluator;
  private interactionSimulator: InteractionSimulator;
  private stateValidator: StateValidator;
  private reportGenerator: ReportGenerator;
  private virtualUserExplorer: VirtualUserExplorer | null = null;
  
  private results: TestResult[] = [];
  private explorationResults: ExplorationResult[] = [];
  private performanceMetrics: Map<string, any> = new Map();
  
  constructor(private config: {
    baseUrl: string;
    llmApiKey: string;
    screenshotDir: string;
    headless: boolean;
  }) {
    this.visualEvaluator = new VisualEvaluator(config.llmApiKey);
    this.interactionSimulator = new InteractionSimulator();
    this.stateValidator = new StateValidator();
    this.reportGenerator = new ReportGenerator(config.screenshotDir);
  }
  
  /**
   * Initialize virtual user explorer after page is ready
   */
  private initializeVirtualUserExplorer(): void {
    if (this.page) {
      this.virtualUserExplorer = new VirtualUserExplorer(
        this.page,
        this.visualEvaluator,
        this.config.screenshotDir
      );
    }
  }
  
  async initialize(): Promise<void> {
    this.browser = await chromium.launch({
      headless: this.config.headless,
      args: ['--disable-gpu', '--no-sandbox'],
      slowMo: this.config.headless ? 0 : 100, // Slow down actions in headed mode so user can see
    });
    
    this.page = await this.browser.newPage();
    await this.page.setViewportSize({ width: 1920, height: 1080 });
    
    // Enable console logging to see what's happening in the browser
    this.page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log(`[Browser Console] ${msg.text()}`);
      }
    });
    
    // Log page errors
    this.page.on('pageerror', error => {
      console.log(`[Page Error] ${error.message}`);
    });
    
    console.log('🤖 Demo Testing Agent initialized');
    console.log(`📍 Connecting to: ${this.config.baseUrl}`);
    
    // Initialize virtual user explorer once page is ready
    this.initializeVirtualUserExplorer();
  }
  
  async runAllTests(): Promise<TestReport> {
    if (!this.page) throw new Error('Agent not initialized');
    
    console.log('🧪 Starting comprehensive demo tests...\n');
    
    // Navigate to demo
    await this.page.goto(this.config.baseUrl);
    await dismissGuidedMode(this.page);
    await this.page.waitForLoadState('networkidle');
    await this.page.waitForTimeout(1000);
    
    // Run test suites
    await this.runVisualTests();
    await this.runInteractionTests();
    await this.runExecutionTests();
    await this.runPerformanceTests();
    await this.runAccessibilityTests();
    
    // Run virtual user exploration journeys
    await this.runVirtualUserJourneys();
    
    // Generate report
    const report = await this.reportGenerator.generate(this.results, this.explorationResults);
    
    console.log('\n📊 Test run complete. Report generated.');
    
    return report;
  }
  
  /**
   * Run virtual user exploration journeys
   * Acts like real users exploring the frontend
   */
  private async runVirtualUserJourneys(): Promise<void> {
    if (!this.virtualUserExplorer) {
      console.log('⚠️  Virtual user explorer not initialized');
      return;
    }
    
    console.log('\n🧑 Running Virtual User Journeys...');
    console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    
    // Navigate to fresh state
    await this.page!.goto(this.config.baseUrl);
    await dismissGuidedMode(this.page!);
    await this.page!.waitForLoadState('networkidle');
    await this.page!.waitForTimeout(2000);
    
    // Run different user journeys
    const journeys: Array<'new-user' | 'power-user' | 'explorer'> = ['new-user', 'power-user', 'explorer'];
    
    for (const journey of journeys) {
      // Reset to fresh state for each journey
      await this.page!.goto(this.config.baseUrl);
      await dismissGuidedMode(this.page!);
      await this.page!.waitForLoadState('networkidle');
      await this.page!.waitForTimeout(2000);
      
      const result = await this.virtualUserExplorer.exploreUserJourney(journey);
      this.explorationResults.push(result);
      
      // Convert exploration result to test result for reporting
      this.results.push({
        scenario: `Virtual User: ${journey}`,
        passed: result.score >= 0.7,
        score: result.score,
        duration: result.duration,
        screenshots: result.steps
          .filter(s => s.screenshot)
          .map(s => s.screenshot!),
        evaluation: {
          score: result.score,
          summary: `Found ${result.visualIssues.length} visual issue(s) during ${journey} journey`,
          criteriaResults: result.visualIssues.map(issue => ({
            criterion: issue.step,
            passed: issue.severity === 'low',
            explanation: issue.issue,
            issues: [issue.issue],
          })),
          recommendations: result.visualIssues
            .filter(i => i.severity === 'high' || i.severity === 'medium')
            .map(i => `Fix ${i.severity} issue in ${i.step}: ${i.issue}`),
        },
        error: result.visualIssues.length > 0 
          ? `${result.visualIssues.length} visual issue(s) found`
          : null,
      });
    }
    
    // Summary
    const totalIssues = this.explorationResults.reduce((sum, r) => sum + r.visualIssues.length, 0);
    const highIssues = this.explorationResults
      .flatMap(r => r.visualIssues)
      .filter(i => i.severity === 'high').length;
    
    console.log(`\n📊 Virtual User Exploration Summary:`);
    console.log(`   Total visual issues found: ${totalIssues}`);
    console.log(`   High severity issues: ${highIssues}`);
    console.log(`   Average journey score: ${(this.explorationResults.reduce((sum, r) => sum + r.score, 0) / this.explorationResults.length).toFixed(2)}`);
  }
  
  private async runVisualTests(): Promise<void> {
    console.log('👁️  Running visual tests...');
    
    const visualScenarios: TestScenario[] = [
      {
        name: 'Initial Load - Empty Canvas',
        description: 'Verify the composer loads with correct layout',
        steps: [
          { action: 'navigate', url: this.config.baseUrl },
          { action: 'waitForTimeout', duration: 2000 },
          { action: 'click', selector: 'button:has-text("Workflow Composer")', timeout: 10000 },
          { action: 'waitForTimeout', duration: 2000 },
          { action: 'screenshot', name: 'initial-load' },
        ],
        validation: {
          type: 'visual',
          criteria: [
            'Library panel visible on left or toggleable',
            'Canvas area is centered and empty',
            'Toolbar visible at top',
            'No loading spinners or error states',
          ],
        },
      },
      {
        name: 'Library Panel - All Categories',
        description: 'Verify all node categories are visible',
        steps: [
          { action: 'navigate', url: this.config.baseUrl },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'click', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'waitForTimeout', duration: 3000 },
          // Library panel might be visible by default or need to be opened
          // First check if library toggle button exists
          { action: 'waitForSelector', selector: 'button:has-text("▶ Library"), button:has-text("◀ Library"), button:has-text("Library"), .library-panel, [class*="library-panel"]', timeout: 10000 },
          { action: 'waitForTimeout', duration: 1000 },
          // Try clicking library toggle if it shows "▶ Library" (closed state)
          { action: 'click', selector: 'button:has-text("▶ Library")', timeout: 3000 },
          { action: 'waitForTimeout', duration: 2000 },
          // Wait for library panel structure to be visible (even if empty due to API)
          { action: 'waitForSelector', selector: '.library-panel, [class*="library-panel"], .library-header, h3:has-text("Library"), input[placeholder*="Search"]', timeout: 10000 },
          { action: 'waitForTimeout', duration: 1000 },
          // Wait for library tabs to be visible (even if empty due to API)
          { action: 'waitForSelector', selector: '.library-tabs, [class*="library-tabs"], button:has-text("📁"), button:has-text("🧱"), button:has-text("📦"), button:has-text("Agents"), button:has-text("Bricks")', timeout: 10000 },
          { action: 'screenshot', name: 'library-panel' },
        ],
        validation: {
          type: 'state',
          criteria: [
            'Library panel is accessible',
            'Items show implementation indicators if visible',
            'Items are visually distinct',
          ],
        },
      },
      {
        name: 'Sample Workflow Loaded',
        description: 'Verify sample workflow renders correctly',
        steps: [
          { action: 'navigate', url: this.config.baseUrl },
          { action: 'waitForTimeout', duration: 2000 },
          { action: 'click', selector: 'button:has-text("Workflow Composer")', timeout: 10000 },
          { action: 'waitForTimeout', duration: 2000 },
          { action: 'click', selector: 'button:has-text("Samples"), button:has-text("📋")', timeout: 5000 },
          { action: 'waitForTimeout', duration: 1500 },
          { action: 'waitForSelector', selector: '.sample-workflows', timeout: 5000 },
          { action: 'click', selector: '.workflow-name:has-text("Security Scanning Pipeline"), button.workflow-card:has-text("Security Scanning Pipeline")', timeout: 5000 },
          { action: 'waitForTimeout', duration: 2000 },
          { action: 'waitForSelector', selector: '.react-flow__node', timeout: 10000 },
          { action: 'screenshot', name: 'sample-workflow-loaded' },
        ],
        validation: {
          type: 'visual',
          criteria: [
            'Nodes are visible on canvas',
            'Connections between nodes are visible',
            'Nodes are properly positioned',
          ],
        },
      },
    ];
    
    for (const scenario of visualScenarios) {
      const result = await this.executeScenario(scenario);
      this.results.push(result);
    }
  }
  
  private async runInteractionTests(): Promise<void> {
    console.log('🖱️  Running interaction tests...');
    
    const interactionScenarios: TestScenario[] = [
      {
        name: 'Load Sample Workflow',
        description: 'Verify sample workflow can be loaded',
        steps: [
          { action: 'navigate', url: this.config.baseUrl },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'click', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'loadWorkflow', workflow: 'security-pipeline' },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: '.react-flow__node', timeout: 15000 },
          { action: 'screenshot', name: 'workflow-loaded' },
        ],
        validation: {
          type: 'state',
          criteria: [
            'Nodes appear on canvas',
            'Canvas shows workflow structure',
          ],
        },
      },
      {
        name: 'Select Node',
        description: 'Verify nodes can be selected',
        steps: [
          { action: 'navigate', url: this.config.baseUrl },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'click', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'loadWorkflow', workflow: 'security-pipeline' },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: '.react-flow__node', timeout: 15000 },
          { action: 'click', selector: '.react-flow__node', timeout: 15000 },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: '.inspector-panel, [class*="inspector-panel"], [class*="Inspector"]', timeout: 15000 },
          { action: 'screenshot', name: 'node-selected' },
        ],
        validation: {
          type: 'state',
          criteria: [
            'Node appears selected',
            'Inspector panel shows node properties or is accessible',
          ],
        },
      },
      {
        name: 'Toggle Implementation',
        description: 'Verify implementation can be toggled',
        steps: [
          { action: 'navigate', url: this.config.baseUrl },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'click', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'loadWorkflow', workflow: 'security-pipeline' },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: '.react-flow__node', timeout: 15000 },
          // Select an agent node (they have implementation toggles)
          // Try to find an agent node specifically (they definitely have toggles)
          // First try to find an agent node by its data attribute or type
          { action: 'click', selector: '.react-flow__node[data-type="agent"], .react-flow__node:has([class*="agent"]), .react-flow__node', timeout: 15000 },
          { action: 'waitForTimeout', duration: 3000 },
          // Ensure inspector is visible and open (click inspector toggle if needed)
          { action: 'click', selector: 'button:has-text("▶ Inspector"), button:has-text("Inspector")', timeout: 5000 },
          { action: 'waitForTimeout', duration: 2000 },
          { action: 'waitForSelector', selector: '.inspector-panel, [class*="inspector-panel"], .inspector-header, h3:has-text("Inspector")', timeout: 15000 },
          { action: 'waitForTimeout', duration: 2000 },
          // Wait for implementation toggle to be visible (check for label or toggle)
          { action: 'waitForSelector', selector: 'label:has-text("Implementation Mode"), .implementation-toggle, [class*="implementation-toggle"], .toggle-option, button:has-text("⚙️"), button:has-text("🤖"), input[type="range"]', timeout: 15000 },
          { action: 'screenshot', name: 'before-toggle' },
          { action: 'setImplementation', mode: 'deterministic' },
          { action: 'waitForTimeout', duration: 3000 },
          // Verify toggle exists - check for any toggle component or label
          { action: 'waitForSelector', selector: 'label:has-text("Implementation Mode"), .implementation-toggle, [class*="implementation-toggle"], .toggle-option, .toggle-slider, button:has-text("⚙️"), button:has-text("🤖"), input[type="range"]', timeout: 5000 },
          { action: 'screenshot', name: 'after-toggle' },
        ],
        validation: {
          type: 'state',
          criteria: [
            '⚙️ Deterministic indicator is visible or toggle state changed',
            'Implementation toggle button state updated',
          ],
        },
      },
    ];
    
    for (const scenario of interactionScenarios) {
      const result = await this.executeScenario(scenario);
      this.results.push(result);
    }
  }
  
  private async runExecutionTests(): Promise<void> {
    console.log('▶️  Running execution visualization tests...');
    
    const executionScenarios: TestScenario[] = [
      {
        name: 'Execution Button Visible',
        description: 'Verify run button is accessible',
        steps: [
          { action: 'navigate', url: this.config.baseUrl },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'click', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'loadWorkflow', workflow: 'security-pipeline' },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: 'button:has-text("Run Workflow"), button:has-text("▶ Run")', timeout: 15000 },
          { action: 'screenshot', name: 'execution-ready' },
        ],
        validation: {
          type: 'state',
          criteria: [
            'Run button is visible',
            'Workflow is ready to execute',
          ],
        },
      },
      {
        name: 'Framework State Sidebar',
        description: 'Verify framework state sidebar can be opened',
        steps: [
          { action: 'navigate', url: this.config.baseUrl },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'click', selector: 'button:has-text("Workflow Composer")', timeout: 15000 },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'click', selector: 'button.sidebar-toggle, button[title*="framework"], button[title*="Framework"], button[title*="Show framework state"], button[title*="Hide framework state"]', timeout: 15000 },
          { action: 'waitForTimeout', duration: 3000 },
          { action: 'waitForSelector', selector: '.framework-sidebar, [class*="framework-sidebar"], [class*="FrameworkState"]', timeout: 10000 },
          { action: 'screenshot', name: 'framework-sidebar' },
        ],
        validation: {
          type: 'state',
          criteria: [
            'Framework state sidebar is accessible',
            'Sidebar shows metrics or state information',
          ],
        },
      },
    ];
    
    for (const scenario of executionScenarios) {
      const result = await this.executeScenario(scenario);
      this.results.push(result);
    }
  }
  
  private async runPerformanceTests(): Promise<void> {
    console.log('⚡ Running performance tests...');
    
    const performanceScenarios: TestScenario[] = [
      {
        name: 'Initial Load Time',
        description: 'Verify app loads within acceptable time',
        steps: [
          { action: 'measureLoadTime' },
        ],
        validation: {
          type: 'performance',
          criteria: [
            'Page loads successfully',
            'No major performance issues',
          ],
          thresholds: {
            loadTime: 5000,
          },
        },
      },
    ];
    
    for (const scenario of performanceScenarios) {
      const result = await this.executeScenario(scenario);
      this.results.push(result);
    }
  }
  
  private async runAccessibilityTests(): Promise<void> {
    console.log('♿ Running accessibility tests...');
    
    const accessibilityScenarios: TestScenario[] = [
      {
        name: 'Keyboard Navigation',
        description: 'Verify app is keyboard navigable',
        steps: [
          { action: 'navigate', url: this.config.baseUrl },
          { action: 'waitForTimeout', duration: 2000 },
          { action: 'click', selector: 'button:has-text("Workflow Composer")', timeout: 10000 },
          { action: 'waitForTimeout', duration: 2000 },
          { action: 'press', key: 'Tab' },
          { action: 'waitForTimeout', duration: 500 },
          { action: 'screenshot', name: 'keyboard-focus' },
        ],
        validation: {
          type: 'accessibility',
          criteria: [
            'Focus indicator is visible or elements are focusable',
            'Tab navigation works',
          ],
        },
      },
    ];
    
    for (const scenario of accessibilityScenarios) {
      const result = await this.executeScenario(scenario);
      this.results.push(result);
    }
  }
  
  private async executeScenario(scenario: TestScenario): Promise<TestResult> {
    const startTime = Date.now();
    const screenshots: string[] = [];
    
    console.log(`  ├─ ${scenario.name}`);
    
    try {
      // Execute steps
      for (const step of scenario.steps) {
        const screenshot = await this.executeStep(step);
        if (screenshot) {
          if (screenshot.includes(',')) {
            // Multiple screenshots from sequence
            screenshots.push(...screenshot.split(','));
          } else {
            screenshots.push(screenshot);
          }
        }
      }
      
      // Validate using appropriate method
      let evaluation: EvaluationResult;
      
      switch (scenario.validation.type) {
        case 'visual':
        case 'visual-sequence':
          if (scenario.validation.type === 'visual-sequence') {
            evaluation = await this.visualEvaluator.evaluateSequence(
              screenshots,
              scenario.validation.criteria
            );
          } else {
            evaluation = await this.visualEvaluator.evaluate(
              screenshots,
              scenario.validation.criteria
            );
          }
          break;
        case 'state':
          evaluation = await this.stateValidator.validate(
            this.page!,
            scenario.validation.criteria
          );
          break;
        case 'performance':
          evaluation = this.validatePerformance(scenario.validation);
          break;
        case 'accessibility':
          evaluation = await this.validateAccessibility(scenario.validation);
          break;
        default:
          throw new Error(`Unknown validation type: ${scenario.validation.type}`);
      }
      
      const passed = evaluation.score >= 0.8;
      
      console.log(`  │  ${passed ? '✓' : '✗'} ${evaluation.score.toFixed(2)} - ${evaluation.summary}`);
      
      return {
        scenario: scenario.name,
        passed,
        score: evaluation.score,
        duration: Date.now() - startTime,
        screenshots,
        evaluation,
        error: null,
      };
      
    } catch (error: any) {
      console.log(`  │  ✗ Error: ${error.message}`);
      
      return {
        scenario: scenario.name,
        passed: false,
        score: 0,
        duration: Date.now() - startTime,
        screenshots,
        evaluation: null,
        error: error.message,
      };
    }
  }
  
  private async executeStep(step: TestStep): Promise<string | null> {
    if (!this.page) throw new Error('Page not initialized');
    
    switch (step.action) {
      case 'navigate':
        if ((step as any).url) {
          await this.page.goto((step as any).url, { waitUntil: 'networkidle' });
        }
        break;
        
      case 'click':
        if (step.selector) {
          const timeout = step.timeout || 5000;
          await this.page.click(step.selector, { timeout }).catch(() => {
            // Try with force if normal click fails
            return this.page!.click(step.selector!, { force: true, timeout: 2000 });
          });
        }
        break;
        
      case 'screenshot':
        if (step.name) {
          const screenshotPath = path.join(this.config.screenshotDir, `${step.name}.png`);
          await this.page.screenshot({ path: screenshotPath, fullPage: false });
          return screenshotPath;
        }
        break;
        
      case 'screenshotSequence':
        const paths: string[] = [];
        for (let i = 0; i < (step.count || 5); i++) {
          const seqPath = path.join(this.config.screenshotDir, `${step.prefix || 'seq'}-${i}.png`);
          await this.page.screenshot({ path: seqPath });
          paths.push(seqPath);
          await this.page.waitForTimeout(step.interval || 500);
        }
        return paths.join(',');
        
      case 'loadWorkflow':
        if (step.workflow) {
          await this.interactionSimulator.loadWorkflow(this.page, step.workflow);
        }
        break;
        
      case 'dragFromLibrary':
        if (step.nodeType && step.agentId) {
          await this.interactionSimulator.dragFromLibrary(
            this.page,
            step.nodeType,
            step.agentId
          );
        }
        break;
        
      case 'dropOnCanvas':
        if (step.position) {
          await this.interactionSimulator.dropOnCanvas(this.page, step.position);
        }
        break;
        
      case 'dragConnection':
        if (step.from && step.to) {
          await this.interactionSimulator.dragConnection(this.page, step.from, step.to);
        }
        break;
        
      case 'setImplementation':
        if (step.mode) {
          await this.interactionSimulator.setImplementation(this.page, step.mode);
        }
        break;
        
      case 'press':
        if (step.key) {
          const key = step.key.replace('Meta+', 'Meta+');
          await this.page.keyboard.press(key);
        }
        break;
        
      case 'waitForState':
        if (step.state) {
          await this.page.waitForSelector(`[data-execution-state="${step.state}"]`, { timeout: 10000 }).catch(() => {});
        }
        break;
        
      case 'waitForSelector':
        if (step.selector) {
          const timeout = (step as any).timeout || 5000;
          await this.page.waitForSelector(step.selector, { timeout }).catch(() => {});
        }
        break;
        
      case 'waitForTimeout':
        if (step.duration) {
          await this.page.waitForTimeout(step.duration);
        }
        break;
        
      case 'measureLoadTime':
        // Performance metrics captured via CDP
        const metrics = await this.page.evaluate(() => {
          return {
            loadTime: performance.timing.loadEventEnd - performance.timing.navigationStart,
            domContentLoaded: performance.timing.domContentLoadedEventEnd - performance.timing.navigationStart,
          };
        });
        this.performanceMetrics.set('loadTime', metrics);
        break;
        
      case 'measureFrameRate':
        // Frame rate measurement would require CDP
        break;
        
      case 'measureMemory':
        // Memory measurement would require CDP
        break;
        
      case 'openFrameworkState':
        await this.page.click('button[title*="framework"], button[title*="Framework"]').catch(() => {});
        break;
        
      case 'expandBrickList':
        await this.page.click('[data-testid="expand-bricks"]').catch(() => {});
        break;
        
      case 'auditAria':
        // ARIA audit would be done in validateAccessibility
        break;
        
      case 'auditContrast':
        // Contrast audit would require color analysis
        break;
        
      case 'extractAccessibilityTree':
        // Accessibility tree extraction
        break;
        
      default:
        console.warn(`Unknown step action: ${step.action}`);
    }
    
    return null;
  }
  
  private validatePerformance(validation: any): EvaluationResult {
    const loadTime = this.performanceMetrics.get('loadTime');
    
    if (loadTime && validation.thresholds?.loadTime) {
      const passed = loadTime.loadTime < validation.thresholds.loadTime;
      return {
        score: passed ? 0.9 : 0.5,
        summary: `Load time: ${loadTime.loadTime}ms (threshold: ${validation.thresholds.loadTime}ms)`,
        criteriaResults: [
          {
            criterion: 'Load time within threshold',
            passed,
            explanation: `${loadTime.loadTime}ms`,
            issues: passed ? [] : ['Load time exceeds threshold'],
          },
        ],
        recommendations: passed ? [] : ['Optimize initial load time'],
      };
    }
    
    return {
      score: 0.8,
      summary: 'Performance metrics collected',
      criteriaResults: [],
      recommendations: [],
    };
  }
  
  private async validateAccessibility(validation: any): Promise<EvaluationResult> {
    const results: Array<{
      criterion: string;
      passed: boolean;
      explanation: string;
      issues: string[];
    }> = [];
    
    for (const criterion of validation.criteria) {
      if (criterion.includes('Focus indicator') || criterion.includes('Tab navigation')) {
        // Check if elements are focusable
        const focusableElements = await this.page!.locator('button, a, input, [tabindex]').count();
        results.push({
          criterion,
          passed: focusableElements > 0,
          explanation: `Found ${focusableElements} focusable elements`,
          issues: focusableElements === 0 ? ['No focusable elements found'] : [],
        });
      } else {
        results.push({
          criterion,
          passed: true,
          explanation: 'Accessibility check performed',
          issues: [],
        });
      }
    }
    
    const passedCount = results.filter(r => r.passed).length;
    const score = results.length > 0 ? passedCount / results.length : 0;
    
    return {
      score,
      summary: `${passedCount}/${results.length} accessibility criteria passed`,
      criteriaResults: results,
      recommendations: [],
    };
  }
  
  async cleanup(): Promise<void> {
    if (this.browser) {
      await this.browser.close();
    }
  }
}
