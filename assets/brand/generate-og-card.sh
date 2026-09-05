#!/bin/bash
# Generate Ashlar flat OG card from HTML template
# This script creates a 1200x630 PNG screenshot for social sharing

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HTML_FILE="$SCRIPT_DIR/og-card-generator.html"
OUTPUT_FILE="$SCRIPT_DIR/ashlar-og-flat-1200x630.png"

echo "Generating flat OG card..."

# Method 1: Try playwright
if command -v npx &> /dev/null; then
    echo "Using Playwright..."
    npx -y playwright screenshot \
        "file://$HTML_FILE" \
        "$OUTPUT_FILE" \
        --viewport-size=1200,630
    echo "✓ Generated: $OUTPUT_FILE"
    exit 0
fi

# Method 2: Try puppeteer
if command -v node &> /dev/null && [ -f "$(npm root -g)/puppeteer" ]; then
    echo "Using Puppeteer..."
    node -e "
    const puppeteer = require('puppeteer');
    (async () => {
      const browser = await puppeteer.launch();
      const page = await browser.newPage();
      await page.setViewport({ width: 1200, height: 630 });
      await page.goto('file://$HTML_FILE');
      await page.screenshot({ path: '$OUTPUT_FILE' });
      await browser.close();
      console.log('✓ Generated: $OUTPUT_FILE');
    })();
    "
    exit 0
fi

# Fallback: Instructions
echo ""
echo "Could not auto-generate. Manual export required:"
echo ""
echo "1. Open in browser: file://$HTML_FILE"
echo "2. Set viewport to 1200x630 (DevTools → Device Mode)"
echo "3. Screenshot (Cmd/Ctrl+Shift+P → Capture screenshot)"
echo "4. Save as: $OUTPUT_FILE"
echo ""
echo "Or install playwright: npm install -g playwright"
echo "Then run: bash $0"

exit 1
