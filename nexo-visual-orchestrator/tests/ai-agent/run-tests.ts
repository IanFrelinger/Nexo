import { DemoTestingAgent } from './DemoTestingAgent';
import * as path from 'path';
import * as fs from 'fs';
import * as dotenv from 'dotenv';

// Load environment variables from .env.local or .env
const envLocalPath = path.join(process.cwd(), '.env.local');
const envPath = path.join(process.cwd(), '.env');

if (fs.existsSync(envLocalPath)) {
  dotenv.config({ path: envLocalPath });
} else if (fs.existsSync(envPath)) {
  dotenv.config({ path: envPath });
}

async function main() {
  const args = process.argv.slice(2);
  
  const config = {
    baseUrl: args.find(a => a.startsWith('--url='))?.split('=')[1] || 'http://localhost:4173',
    llmApiKey: process.env.ANTHROPIC_API_KEY || '',
    screenshotDir: args.find(a => a.startsWith('--output='))?.split('=')[1] || path.join(process.cwd(), 'test-results', 'demo-tests'),
    headless: !args.includes('--headed'),
  };
  
  // Create screenshot directory
  if (!fs.existsSync(config.screenshotDir)) {
    fs.mkdirSync(config.screenshotDir, { recursive: true });
  }
  
  if (!config.llmApiKey) {
    console.warn('⚠️  ANTHROPIC_API_KEY not set - visual evaluation will use fallback mode');
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
