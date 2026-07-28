#!/usr/bin/env node
/**
 * Headed Chromium walkthrough: open Event Extract, pick a large mock file,
 * start an extract, and leave the window open so you can watch Live extraction.
 *
 *   node scripts/watch-live-extract.mjs
 *   EVTX_BASE_URL=http://127.0.0.1:18080 FILE=exercise_alpha.evtq node scripts/watch-live-extract.mjs
 */
import { chromium } from 'playwright';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, '..');
const base = (process.env.EVTX_BASE_URL || 'http://127.0.0.1:18080').replace(/\/$/, '');
const file = process.env.FILE || 'exercise_alpha.evtq';
const holdMs = Number(process.env.HOLD_MS || 45000);

async function main() {
  // Prefer Playwright from NuGet package (same as .NET tests).
  let executablePath;
  const pwRoot = spawnSync('bash', ['-lc',
    `find "$HOME/.nuget/packages/microsoft.playwright" -maxdepth 1 -mindepth 1 -type d 2>/dev/null | sort -V | tail -1`],
    { encoding: 'utf8' }).stdout.trim();
  if (pwRoot) {
    const arch = process.arch === 'arm64' ? 'linux-arm64' : 'linux-x64';
    const chrome = spawnSync('bash', ['-lc',
      `ls "$HOME/.cache/ms-playwright"/chromium-*/chrome-linux*/chrome 2>/dev/null | head -1`],
      { encoding: 'utf8' }).stdout.trim();
    if (chrome && existsSync(chrome)) executablePath = chrome;
  }

  const browser = await chromium.launch({
    headless: false,
    slowMo: 80,
    executablePath: executablePath || undefined,
    args: ['--no-sandbox', '--disable-dev-shm-usage'],
  });
  const page = await browser.newPage({ viewport: { width: 1360, height: 920 } });
  console.log(`Opening ${base}/ …`);
  await page.goto(base + '/');
  await page.waitForFunction(() => document.querySelector('#connTxt')?.textContent?.includes('connected'), null, { timeout: 30000 });
  await page.waitForSelector(`#files tr[data-name="${file}"]`, { timeout: 15000 });
  await page.click(`#files tr[data-name="${file}"]`);
  await page.waitForSelector('#scopeCard', { state: 'visible' });
  // Full scan so progress stays visible longer.
  await page.click('#strideSeg button[data-s="1"]');
  await page.fill('#maxev', '0');
  console.log(`Starting extract on ${file} — watch the Live extraction panel.`);
  await page.click('#run');
  await page.waitForSelector('#livePanel', { state: 'visible', timeout: 15000 });
  // Keep the window open for observation (or until the job finishes + a short beat).
  const deadline = Date.now() + holdMs;
  while (Date.now() < deadline) {
    const done = await page.evaluate(() =>
      document.querySelector('#livePillTxt')?.textContent?.includes('COMPLETE')
      || document.querySelector('#livePillTxt')?.textContent?.includes('FAILED'));
    if (done) {
      console.log('Job finished — holding window briefly…');
      await page.waitForTimeout(8000);
      break;
    }
    await page.waitForTimeout(500);
  }
  console.log('Closing browser.');
  await browser.close();
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
