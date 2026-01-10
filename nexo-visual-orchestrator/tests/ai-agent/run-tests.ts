import { DemoTestingAgent } from './DemoTestingAgent';
import * as path from 'path';
import * as fs from 'fs';
import * as dotenv from 'dotenv';
import * as http from 'http';

// Load environment variables from .env.local or .env
const envLocalPath = path.join(process.cwd(), '.env.local');
const envPath = path.join(process.cwd(), '.env');

if (fs.existsSync(envLocalPath)) {
  dotenv.config({ path: envLocalPath });
} else if (fs.existsSync(envPath)) {
  dotenv.config({ path: envPath });
}

// Check if a server is running at the given URL
async function checkServerRunning(url: string): Promise<boolean> {
  return new Promise((resolve) => {
    const parsedUrl = new URL(url);
    const options = {
      hostname: parsedUrl.hostname,
      port: parsedUrl.port || (parsedUrl.protocol === 'https:' ? 443 : 80),
      path: '/',
      method: 'HEAD',
      timeout: 2000,
    };

    const req = http.request(options, (res) => {
      resolve(res.statusCode !== undefined && res.statusCode < 500);
    });

    req.on('error', () => resolve(false));
    req.on('timeout', () => {
      req.destroy();
      resolve(false);
    });

    req.end();
  });
}

async function main() {
  const args = process.argv.slice(2);
  
  // Default to dev server (Vite default port)
  const defaultUrl = 'http://localhost:5173';
  const config = {
    baseUrl: args.find(a => a.startsWith('--url='))?.split('=')[1] || defaultUrl,
    llmApiKey: process.env.ANTHROPIC_API_KEY || '',
    screenshotDir: args.find(a => a.startsWith('--output='))?.split('=')[1] || path.join(process.cwd(), 'test-results', 'demo-tests'),
    headless: args.includes('--headless'), // Default to headed mode for live GUI interaction
  };
  
  // Check if server is running
  console.log(`🔍 Checking if server is running at ${config.baseUrl}...`);
  const serverRunning = await checkServerRunning(config.baseUrl);
  
  if (!serverRunning) {
    console.error(`\n❌ Server is not running at ${config.baseUrl}`);
    console.error(`\nPlease start the dev server first:`);
    console.error(`  npm run dev`);
    console.error(`\nOr specify a different URL:`);
    console.error(`  npm run test:demo -- --url=http://localhost:3000`);
    process.exit(1);
  }
  
  console.log(`✅ Server is running at ${config.baseUrl}`);
  console.log(`🌐 Testing against LIVE GUI (not preview/test environment)\n`);
  
  // Create screenshot directory
  if (!fs.existsSync(config.screenshotDir)) {
    fs.mkdirSync(config.screenshotDir, { recursive: true });
  }
  
  if (!config.llmApiKey) {
    console.warn('⚠️  ANTHROPIC_API_KEY not set - visual evaluation will use fallback mode');
  }
  
  if (!config.headless) {
    console.log('👁️  Running in HEADED mode - you can watch the browser interact with your GUI\n');
  }
  
  const agent = new DemoTestingAgent(config);
  
  try {
    await agent.initialize();
    const report = await agent.runAllTests();
    
    console.log('\n' + '═'.repeat(60));
    console.log('📊 TEST SUMMARY');
    console.log('═'.repeat(60));
    console.log(`Total:  ${report.summary.total}`);
    console.log(`Passed: ${report.summary.passed} ✓`);
    console.log(`Failed: ${report.summary.failed} ✗`);
    console.log(`Score:  ${(report.summary.score * 100).toFixed(1)}%`);
    console.log(`Time:   ${(report.summary.duration / 1000).toFixed(1)}s`);
    console.log('═'.repeat(60));
    console.log(`\n📄 Report: ${path.join(config.screenshotDir, 'report.html')}`);
    
    process.exit(report.summary.failed > 0 ? 1 : 0);
    
  } catch (error: any) {
    console.error('❌ Test run failed:', error);
    process.exit(1);
  } finally {
    await agent.cleanup();
  }
}

main().catch(console.error);
