import * as fs from 'fs';
import * as path from 'path';
import type { TestReport, TestResult } from './types';

export class ReportGenerator {
  constructor(private screenshotDir: string) {
    // Ensure directory exists
    if (!fs.existsSync(screenshotDir)) {
      fs.mkdirSync(screenshotDir, { recursive: true });
    }
  }
  
  async generate(results: TestResult[]): Promise<TestReport> {
    const passed = results.filter(r => r.passed).length;
    const failed = results.filter(r => !r.passed).length;
    const totalScore = results.length > 0 
      ? results.reduce((sum, r) => sum + r.score, 0) / results.length 
      : 0;
    
    const report: TestReport = {
      timestamp: new Date().toISOString(),
      summary: {
        total: results.length,
        passed,
        failed,
        score: totalScore,
        duration: results.reduce((sum, r) => sum + r.duration, 0),
      },
      results,
      recommendations: this.aggregateRecommendations(results),
    };
    
    // Generate HTML report
    await this.generateHtmlReport(report);
    
    // Generate JSON report
    const jsonPath = path.join(this.screenshotDir, 'report.json');
    fs.writeFileSync(jsonPath, JSON.stringify(report, null, 2));
    
    return report;
  }
  
  private aggregateRecommendations(results: TestResult[]): string[] {
    const recommendations = new Set<string>();
    
    for (const result of results) {
      if (result.evaluation?.recommendations) {
        result.evaluation.recommendations.forEach(r => recommendations.add(r));
      }
    }
    
    return Array.from(recommendations);
  }
  
  private async generateHtmlReport(report: TestReport): Promise<void> {
    const html = `
<!DOCTYPE html>
<html>
<head>
  <title>Nexo Demo Test Report</title>
  <style>
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
      background: #0d1117;
      color: #c9d1d9;
    }
    h1 { color: #58a6ff; }
    .summary {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 20px;
      margin: 20px 0;
    }
    .summary-card {
      background: #161b22;
      padding: 20px;
      border-radius: 8px;
      text-align: center;
    }
    .summary-value {
      font-size: 2em;
      font-weight: bold;
    }
    .passed { color: #3fb950; }
    .failed { color: #f85149; }
    .score { color: #58a6ff; }
    .result {
      background: #161b22;
      margin: 10px 0;
      padding: 15px;
      border-radius: 8px;
      border-left: 4px solid;
    }
    .result.pass { border-color: #3fb950; }
    .result.fail { border-color: #f85149; }
    .result-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .screenshots {
      display: flex;
      gap: 10px;
      margin-top: 10px;
      overflow-x: auto;
    }
    .screenshots img {
      max-height: 200px;
      border-radius: 4px;
      border: 1px solid #30363d;
    }
    .criteria-list {
      margin-top: 10px;
    }
    .criterion {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 5px 0;
    }
    .criterion.pass::before { content: '✓'; color: #3fb950; font-weight: bold; }
    .criterion.fail::before { content: '✗'; color: #f85149; font-weight: bold; }
    .recommendations {
      background: #1f2937;
      padding: 15px;
      border-radius: 8px;
      margin-top: 20px;
    }
    .recommendations h3 { color: #fbbf24; }
    .recommendations li { margin: 10px 0; }
    .error {
      color: #f85149;
      background: #3d1f1f;
      padding: 10px;
      border-radius: 4px;
      margin-top: 10px;
    }
  </style>
</head>
<body>
  <h1>🧪 Nexo Demo Test Report</h1>
  <p>Generated: ${new Date(report.timestamp).toLocaleString()}</p>
  
  <div class="summary">
    <div class="summary-card">
      <div class="summary-value">${report.summary.total}</div>
      <div>Total Tests</div>
    </div>
    <div class="summary-card">
      <div class="summary-value passed">${report.summary.passed}</div>
      <div>Passed</div>
    </div>
    <div class="summary-card">
      <div class="summary-value failed">${report.summary.failed}</div>
      <div>Failed</div>
    </div>
    <div class="summary-card">
      <div class="summary-value score">${(report.summary.score * 100).toFixed(1)}%</div>
      <div>Score</div>
    </div>
  </div>
  
  <h2>Test Results</h2>
  ${report.results.map(r => `
    <div class="result ${r.passed ? 'pass' : 'fail'}">
      <div class="result-header">
        <h3>${r.scenario}</h3>
        <span>${r.passed ? '✓ PASS' : '✗ FAIL'} (${(r.score * 100).toFixed(0)}%)</span>
      </div>
      <p>${r.evaluation?.summary || r.error || 'No details'}</p>
      
      ${r.error ? `
        <div class="error">
          <strong>Error:</strong> ${r.error}
        </div>
      ` : ''}
      
      ${r.evaluation?.criteriaResults && r.evaluation.criteriaResults.length > 0 ? `
        <div class="criteria-list">
          ${r.evaluation.criteriaResults.map(c => `
            <div class="criterion ${c.passed ? 'pass' : 'fail'}">
              ${c.criterion}: ${c.explanation}
              ${c.issues.length > 0 ? `<br><small style="color: #f85149;">Issues: ${c.issues.join(', ')}</small>` : ''}
            </div>
          `).join('')}
        </div>
      ` : ''}
      
      ${r.screenshots && r.screenshots.length > 0 ? `
        <div class="screenshots">
          ${r.screenshots.filter(s => s && !s.includes(',')).map(s => {
            const relativePath = path.relative(this.screenshotDir, s);
            return `<img src="${relativePath}" alt="Screenshot">`;
          }).join('')}
        </div>
      ` : ''}
    </div>
  `).join('')}
  
  ${report.recommendations.length > 0 ? `
    <div class="recommendations">
      <h3>💡 Recommendations</h3>
      <ul>
        ${report.recommendations.map(r => `<li>${r}</li>`).join('')}
      </ul>
    </div>
  ` : ''}
</body>
</html>
`;
    
    const htmlPath = path.join(this.screenshotDir, 'report.html');
    fs.writeFileSync(htmlPath, html);
  }
}
