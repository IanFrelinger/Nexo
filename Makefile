.PHONY: build test test-local test-cross-platform test-portable test-multi-env test-all-platforms test-all-platforms-ephemeral ci-verify validate-safe review-summary package-cli clean-test-artifacts

# Build the solution
build:
	dotnet build

# Run tests locally (blame-hang-timeout prevents indefinite freeze from hung tests)
# --blame-hang-dump-type none avoids 6GB+ hang dumps that accumulate in TestResults/
test:
	dotnet test --blame-hang-timeout 30s --blame-hang-dump-type none

# Run tests on all target platforms: local + Docker (ubuntu, alpine, debian).
# For native macOS/Windows/Linux use: make test-cross-platform (triggers CI).
test-all-platforms:
	@echo "=== Local (current OS) ==="
	dotnet build -v minimal
	dotnet test --no-build --verbosity minimal --blame-hang-timeout 30s --blame-hang-dump-type none
	@echo "=== Docker: Ubuntu 8.0 ==="
	docker build -f .docker/Dockerfile.test-caching --build-arg DOTNET_VERSION=8.0 -t nexo-test-ubuntu:8.0 .
	mkdir -p test-results
	docker run --rm -v "$$(pwd)/test-results:/workspace/test-results" nexo-test-ubuntu:8.0 \
		bash -c "dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --blame-hang-timeout 60s --filter 'FullyQualifiedName~BaseFrameworkSmokeTests' --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=ubuntu-8.0-base.trx' --results-directory /workspace/test-results"
	@echo "=== Docker: Alpine 8.0 ==="
	docker build -f .docker/Dockerfile.test-caching-alpine --build-arg DOTNET_VERSION=8.0 -t nexo-test-alpine:8.0 .
	docker run --rm -v "$$(pwd)/test-results:/workspace/test-results" nexo-test-alpine:8.0 \
		bash -c "dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --blame-hang-timeout 60s --filter 'FullyQualifiedName~BaseFrameworkSmokeTests' --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=alpine-8.0-base.trx' --results-directory /workspace/test-results"
	@echo "=== Docker: Debian 8.0 ==="
	docker build -f .docker/Dockerfile.test-caching-debian --build-arg DOTNET_VERSION=8.0 -t nexo-test-debian:8.0 .
	docker run --rm -v "$$(pwd)/test-results:/workspace/test-results" nexo-test-debian:8.0 \
		bash -c "dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --blame-hang-timeout 60s --filter 'FullyQualifiedName~BaseFrameworkSmokeTests' --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=debian-8.0-base.trx' --results-directory /workspace/test-results"
	@echo "=== All target platforms (local + ubuntu + alpine + debian) completed ==="

# Ephemeral: run tests in containers with no volume mounts; results discarded when container is removed
test-all-platforms-ephemeral:
	@echo "=== Ephemeral multi-platform tests (no host artifacts) ==="
	dotnet build -v minimal
	dotnet run --project src/Nexo.CLI -- test --platforms ubuntu alpine debian --ephemeral

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

# Linear adaptation tests across all Docker environments
test-adaptation-all-envs:
	dotnet run --project src/Nexo.CLI -- test multi-env --suite adaptation --all

# CI verification: build + checks (C#-driven; replaces scripts/ci-verify.sh)
ci-verify:
	dotnet run --project src/Nexo.CLI -- ci verify

# Safe validation: sequential, minimal memory. Run from external terminal to avoid Cursor memory explosion.
# Equivalent to ci-verify but via shell script; use when ci-verify causes high memory usage.
validate-safe:
	@bash scripts/validate-safe.sh

# Dogfood Block 1: verify observation pipeline watches Nexo's own dev workflow
dogfood-block1:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock1Tests" --no-build -v minimal

# Dogfood Block 2: verify static analyzer runs against Block 1 (Observation) code
dogfood-block2:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock2Tests" --no-build -v minimal

# Dogfood Block 3: adaptation engine decomposes/recompiles Nexo brick
dogfood-block3:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock3Tests" --no-build -v minimal

# Dogfood Block 4: promote Nexo fix via inheritance
dogfood-block4:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock4Tests" --no-build -v minimal

# Dogfood Block 5: autonomy controls on Nexo dev workflow
dogfood-block5:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock5Tests" --no-build -v minimal

# Dogfood Block 6: SelfContextAssembler answers 24h question
dogfood-block6:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock6Tests" --no-build -v minimal

# Dogfood Block 7: Composition engine composes for Nexo problem
dogfood-block7:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock7Tests" --no-build -v minimal

# Dogfood Block 8: Parallel test matrix against Nexo tests
dogfood-block8:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock8Tests" --no-build -v minimal

# Phase D: Composition-driven testing (Block 7–8)
dogfood-block8-composed:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock8ComposedTests" --no-build -v minimal

# Dogfood Block 9: Instance mesh discover/advertise
dogfood-block9:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock9Tests" --no-build -v minimal

# Phase E: Local IPC mesh - two instances share capability
dogfood-block9-ipc:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodBlock9LocalIpcTests" --no-build -v minimal

# Dogfood Blocks 1–6 (Phase C validation)
dogfood-phase-c:
	$(MAKE) dogfood-block1
	$(MAKE) dogfood-block2
	$(MAKE) dogfood-block3
	$(MAKE) dogfood-block4
	$(MAKE) dogfood-block5
	$(MAKE) dogfood-block6

# Dogfood Blocks 7–9 (Phase D+E validation)
dogfood-phase-de:
	$(MAKE) dogfood-block7
	$(MAKE) dogfood-block8
	$(MAKE) dogfood-block9

# Dogfood Phase F: closed-loop improve on Nexo
dogfood-closedloop:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodClosedLoopTests" --no-build -v minimal

# Phase F: Continuous self-improvement loop (changelog, test failure store)
dogfood-phasef:
	dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -v minimal
	dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~DogfoodPhaseFTests" --no-build -v minimal

# All dogfood blocks (1–9) + Phase F closed-loop + Phase F
dogfood-all:
	$(MAKE) dogfood-phase-c
	$(MAKE) dogfood-phase-de
	$(MAKE) dogfood-closedloop
	$(MAKE) dogfood-phasef

# Review summary Markdown from JSON (C#-driven; replaces scripts/review-summary-md.sh)
review-summary:
	dotnet run --project src/Nexo.CLI -- review summary

# Remove test artifacts: hang dumps (~6GB each), .trx, coverage, per-run TestResults dirs.
# Run after tests to reclaim disk space. Safe to run anytime.
clean-test-artifacts:
	@echo "Cleaning test artifacts..."
	@find src -type d -name "TestResults" -exec rm -rf {} + 2>/dev/null; true
	@rm -rf test-results
	@echo "Done."

# Package CLI as single-file executable
package-cli:
	dotnet publish src/Nexo.CLI -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o dist/linux
	dotnet publish src/Nexo.CLI -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o dist/windows
	dotnet publish src/Nexo.CLI -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -o dist/macos

# Pack NuGet library packages (Nexo.Hosting, Nexo.CLI tool)
pack:
	dotnet pack src/Nexo.Hosting/Nexo.Hosting.csproj -c Release -o dist/nuget
	dotnet pack src/Nexo.CLI/Nexo.CLI.csproj -c Release -o dist/nuget

# Build CLI Docker image (linux/amd64 for portability)
docker-cli:
	docker build --platform linux/amd64 -f .docker/Dockerfile.cli -t nexo-cli:latest .

# Generate API docs (requires: dotnet tool install -g docfx)
docs-api:
	dotnet build -c Release
	cd docs/api && docfx
