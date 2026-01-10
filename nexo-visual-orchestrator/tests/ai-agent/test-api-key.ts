import Anthropic from '@anthropic-ai/sdk';
import * as dotenv from 'dotenv';
import * as path from 'path';
import * as fs from 'fs';

// Load environment variables
const envLocalPath = path.join(process.cwd(), '.env.local');
if (fs.existsSync(envLocalPath)) {
  dotenv.config({ path: envLocalPath });
}

const apiKey = process.env.ANTHROPIC_API_KEY;

if (!apiKey) {
  console.error('❌ ANTHROPIC_API_KEY not found in .env.local');
  process.exit(1);
}

console.log('🔑 API Key found:', apiKey.substring(0, 20) + '...');
console.log('📡 Testing API connection...\n');

const client = new Anthropic({ apiKey });

async function testAPI() {
  try {
    // Test 1: List available models
    console.log('1️⃣  Testing models list endpoint...');
    const response = await fetch('https://api.anthropic.com/v1/models', {
      headers: {
        'x-api-key': apiKey,
        'anthropic-version': '2023-06-01',
      },
    });
    
    if (!response.ok) {
      const error = await response.text();
      console.error('❌ Error:', response.status, error);
      return;
    }
    
    const modelsData = await response.json();
    console.log('✅ Available models:');
    modelsData.data?.forEach((model: any) => {
      console.log(`   - ${model.id}`);
    });
    
    // Test 2: Try a simple message with different model names
    console.log('\n2️⃣  Testing message creation with different models...');
    
    const modelsToTest = [
      'claude-3-5-sonnet-20241022',
      'claude-3-5-sonnet',
      'claude-sonnet-4-20250514',
      'claude-3-opus-20240229',
      'claude-3-5-haiku-20241022',
    ];
    
    for (const model of modelsToTest) {
      try {
        console.log(`   Testing ${model}...`);
        const message = await client.messages.create({
          model: model as any,
          max_tokens: 10,
          messages: [
            {
              role: 'user',
              content: 'Say "test"',
            },
          ],
        });
        
        console.log(`   ✅ ${model} works! Response: ${message.content[0].type}`);
        break; // Use the first working model
      } catch (error: any) {
        console.log(`   ❌ ${model} failed: ${error.message}`);
      }
    }
    
  } catch (error: any) {
    console.error('❌ API test failed:', error.message);
    if (error.status) {
      console.error('   Status:', error.status);
    }
    if (error.error) {
      console.error('   Error details:', JSON.stringify(error.error, null, 2));
    }
  }
}

testAPI().then(() => {
  console.log('\n✅ API test complete');
  process.exit(0);
}).catch((error) => {
  console.error('\n❌ Test failed:', error);
  process.exit(1);
});
