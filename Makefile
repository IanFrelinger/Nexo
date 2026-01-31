.PHONY: build test test-local test-cross-platform test-portable test-multi-env ci-verify review-summary demo-test demo-dev package-cli

# Build the solution
build:
	dotnet build

# Run tests locally
test:
	dotnet test

# Run tests on all platforms (C#-driven; works on Windows, macOS, Linux, mobile)
test-all:
	dotnet run --project src/Nexo.CLI -- test --platforms ubuntu alpine debian android ios unity windows

# Run tests on specific platform
test-platform:
	dotnet run --project src/Nexo.CLI -- test --platforms $(PLATFORM)

# Trigger cross-platform tests in CI (Mac, Windows, Linux from one place)
# Requires: gh auth login. Usage: make test-cross-platform [SCOPE=smoke|persistence|full]
test-cross-platform:
	gh workflow run "Cross-Platform Tests" --ref main -f scope=$${SCOPE:-smoke}

# Portable tests: C#-driven (replaces scripts/portable-test.sh). Works on Windows, macOS, Linux, mobile.
# Usage: make test-portable [SCOPE=persistence|smoke|all]. Use --list to see targets: dotnet run --project src/Nexo.CLI -- test portable --list
test-portable:
	dotnet run --project src/Nexo.CLI -- test portable --scope $${SCOPE:-persistence}

# Multi-env framework/caching/persistence tests (C#-driven; replaces test-framework-multi-env.sh etc.)
test-multi-env:
	dotnet run --project src/Nexo.CLI -- test multi-env --suite framework --all

# CI verification: build + checks (C#-driven; replaces scripts/ci-verify.sh)
ci-verify:
	dotnet run --project src/Nexo.CLI -- ci verify

# Review summary Markdown from JSON (C#-driven; replaces scripts/review-summary-md.sh)
review-summary:
	dotnet run --project src/Nexo.CLI -- review summary

# CLI demos
demo-test:
	nexo demo test \
		--target "https://httpbin.org/html" \
		--goal "Verify page structure and content" \
		--depth quick

demo-dev:
	nexo demo dev \
		--project ./examples/sample-project \
		--task "Add input validation" \
		--max-iterations 3 \
		--autonomy supervised

# Build and run demo
demo-fresh: build demo-test

# Package CLI as single-file executable
package-cli:
	dotnet publish src/Nexo.CLI -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o dist/linux
	dotnet publish src/Nexo.CLI -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o dist/windows
	dotnet publish src/Nexo.CLI -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -o dist/macos
