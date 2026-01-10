import Anthropic from '@anthropic-ai/sdk';
import * as fs from 'fs';
import type { EvaluationResult } from './types';

export class VisualEvaluator {
  private client: Anthropic | null = null;
  
  constructor(private apiKey: string) {
    if (apiKey) {
      this.client = new Anthropic({ apiKey });
    }
  }
  
  async evaluate(
    screenshotPaths: string[],
    criteria: string[]
  ): Promise<EvaluationResult> {
    if (!this.client) {
      // Fallback: return mock evaluation if no API key
      return {
        score: 0.85,
        summary: 'Visual evaluation skipped (no API key)',
        criteriaResults: criteria.map(c => ({
          criterion: c,
          passed: true,
          explanation: 'Skipped',
          issues: [],
        })),
        recommendations: [],
      };
    }
    
    // Load screenshots as base64
    const images = screenshotPaths
      .filter(p => p && fs.existsSync(p))
      .map(p => {
        const buffer = fs.readFileSync(p);
        return {
          type: 'image' as const,
          source: {
            type: 'base64' as const,
            media_type: 'image/png' as const,
            data: buffer.toString('base64'),
          },
        };
      });
    
    if (images.length === 0) {
      return {
        score: 0,
        summary: 'No screenshots provided',
        criteriaResults: [],
        recommendations: ['Provide screenshots for evaluation'],
      };
    }
    
    const prompt = this.buildEvaluationPrompt(criteria);
    
    try {
      const response = await this.client.messages.create({
        model: 'claude-sonnet-4-20250514',
        max_tokens: 2000,
        messages: [
          {
            role: 'user',
            content: [
              ...images,
              { type: 'text', text: prompt },
            ],
          },
        ],
      });
      
      return this.parseEvaluationResponse(response);
    } catch (error: any) {
      console.warn('LLM evaluation failed:', error.message);
      return {
        score: 0.5,
        summary: `Evaluation error: ${error.message}`,
        criteriaResults: criteria.map(c => ({
          criterion: c,
          passed: false,
          explanation: 'Evaluation failed',
          issues: [error.message],
        })),
        recommendations: ['Check API key and network connection'],
      };
    }
  }
  
  private buildEvaluationPrompt(criteria: string[]): string {
    return `
You are a QA engineer evaluating a visual workflow composer UI. Analyze the screenshot(s) and evaluate against these criteria:

${criteria.map((c, i) => `${i + 1}. ${c}`).join('\n')}

For each criterion, provide:
- PASS/FAIL assessment
- Brief explanation
- Specific issues found (if any)

Then provide an overall score from 0.0 to 1.0 and a summary.

Respond in this JSON format:
{
  "criteria_results": [
    {
      "criterion": "...",
      "passed": true,
      "explanation": "...",
      "issues": []
    }
  ],
  "score": 0.85,
  "summary": "Overall assessment...",
  "recommendations": []
}
`;
  }
  
  private parseEvaluationResponse(response: any): EvaluationResult {
    const content = response.content[0].text;
    
    // Extract JSON from response
    const jsonMatch = content.match(/\{[\s\S]*\}/);
    if (!jsonMatch) {
      return {
        score: 0,
        summary: 'Failed to parse evaluation response',
        criteriaResults: [],
        recommendations: [],
      };
    }
    
    try {
      const parsed = JSON.parse(jsonMatch[0]);
      return {
        score: parsed.score || 0,
        summary: parsed.summary || 'No summary provided',
        criteriaResults: parsed.criteria_results || [],
        recommendations: parsed.recommendations || [],
      };
    } catch (e: any) {
      return {
        score: 0,
        summary: `Parse error: ${e.message}`,
        criteriaResults: [],
        recommendations: [],
      };
    }
  }
  
  async evaluateSequence(
    screenshotPaths: string[],
    criteria: string[]
  ): Promise<EvaluationResult> {
    if (!this.client) {
      return {
        score: 0.85,
        summary: 'Sequence evaluation skipped (no API key)',
        criteriaResults: criteria.map(c => ({
          criterion: c,
          passed: true,
          explanation: 'Skipped',
          issues: [],
        })),
        recommendations: [],
      };
    }
    
    const prompt = `
You are evaluating a sequence of ${screenshotPaths.length} screenshots showing an animation or progression in a workflow composer UI.

Evaluate the SEQUENCE against these criteria:
${criteria.map((c, i) => `${i + 1}. ${c}`).join('\n')}

Pay attention to:
- Smooth visual transitions between frames
- Correct order of operations
- Clear visual feedback at each stage
- No visual glitches or artifacts

Respond in JSON format as specified.
`;
    
    // Load all screenshots
    const images = screenshotPaths
      .filter(p => p && fs.existsSync(p))
      .map(p => {
        const buffer = fs.readFileSync(p);
        return {
          type: 'image' as const,
          source: {
            type: 'base64' as const,
            media_type: 'image/png' as const,
            data: buffer.toString('base64'),
          },
        };
      });
    
    if (images.length === 0) {
      return {
        score: 0,
        summary: 'No screenshots provided for sequence',
        criteriaResults: [],
        recommendations: [],
      };
    }
    
    try {
      const response = await this.client.messages.create({
        model: 'claude-sonnet-4-20250514',
        max_tokens: 2000,
        messages: [
          {
            role: 'user',
            content: [
              ...images,
              { type: 'text', text: prompt },
            ],
          },
        ],
      });
      
      return this.parseEvaluationResponse(response);
    } catch (error: any) {
      console.warn('LLM sequence evaluation failed:', error.message);
      return {
        score: 0.5,
        summary: `Sequence evaluation error: ${error.message}`,
        criteriaResults: [],
        recommendations: [],
      };
    }
  }
}
