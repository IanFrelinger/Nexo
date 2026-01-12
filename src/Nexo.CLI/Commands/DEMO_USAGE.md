# Nexo CLI Demo Commands

CLI-based demos for Universal Testing Agent and Autonomous Development Agent.

## Universal Testing Agent

Test any application with AI-powered testing:

```bash
# Test a website
nexo demo test "https://example.com" "Test the checkout flow and verify payment works"

# Test with specific options
nexo demo test "https://example.com" "Test login functionality" \
  --type WebApp \
  --depth Thorough \
  --max-duration 15m \
  --output report.json

# Test an API
nexo demo test "api://https://api.example.com" "Verify all CRUD operations work correctly"

# Test a game (requires game adapter)
nexo demo test "C:\Games\MyGame\MyGame.exe" "Play through tutorial and find bugs"
```

### Options

- `--type` - Target type (WebApp, Game, Api, Cli, DesktopApp, Auto)
- `--depth` - Testing depth (Quick, Standard, Thorough, Exhaustive)
- `--max-duration` - Maximum duration (e.g., "10m", "1h")
- `--output` - Output file for test report (JSON)

## Autonomous Development Agent

Build features autonomously with mock user testing:

```bash
# Build a feature
nexo demo dev "Add save/load system for player progress" "./MyGame" \
  --acceptance "Player can save, quit, reload, and continue from same point" \
  --persona Average \
  --max-iterations 10

# Fix a bug
nexo demo dev "Fix enemies not respawning after death" "./MyGame" \
  --persona Adversarial \
  --autonomy SemiAutonomous

# Build an API endpoint
nexo demo dev "Create REST API for user registration" "./MyApi" \
  --type DotNetApi \
  --acceptance "Valid returns 201, invalid returns 400" \
  --persona Adversarial
```

### Options

- `--type` - Project type (UnityGame, DotNetApp, ReactApp, etc.)
- `--acceptance` - Acceptance criteria
- `--max-iterations` - Maximum iterations (default: 10)
- `--persona` - Test persona (Novice, Average, PowerUser, Adversarial, Accessibility, Impatient)
- `--autonomy` - Autonomy level (Supervised, SemiAutonomous, FullyAutonomous)
- `--output` - Output file for session report (JSON)

## Examples

### Example 1: Test a Web Application

```bash
nexo demo test "https://my-shop.com" \
  "Test the complete checkout flow - add item to cart, proceed to checkout, verify payment form works" \
  --depth Standard \
  --output checkout-test.json
```

### Example 2: Build a Game Feature

```bash
nexo demo dev \
  "Add a save/load system for player progress including position, inventory, and quest state" \
  "./MyUnityGame" \
  --type UnityGame \
  --acceptance "Player can save game, quit, relaunch, load save, and continue from exact position with all items and quest progress intact" \
  --persona Average \
  --max-iterations 10 \
  --autonomy SemiAutonomous \
  --output dev-session.json
```

### Example 3: Fix a Bug

```bash
nexo demo dev \
  "Fix the bug where enemies don't respawn after player death" \
  "./MyGame" \
  --persona Adversarial \
  --constraints "Don't modify the enemy AI behavior, only respawn logic"
```

## Output Formats

### JSON Output

Use `--format-json` for machine-readable output:

```bash
nexo demo test "https://example.com" "Test login" --format-json > report.json
```

### Human-Readable Output

Default output uses Spectre.Console for beautiful formatting with:
- Progress indicators
- Color-coded results
- Tables and summaries
- Key findings and recommendations
