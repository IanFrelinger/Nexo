.PHONY: build build-portable test test-caching test-caching-all demo-test demo-dev demo-fresh package-cli

build:
	dotnet build

build-portable:
	bash scripts/build-portable.sh

test:
	dotnet test

# Geospatial smoke tests (includes caching)
test-geospatial-smoke:
	dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj --filter "FullyQualifiedName~GeospatialE2ESmokeTests"

test-geospatial-smoke-all:
	bash scripts/test-caching-multi-env.sh --all

# Visual validation tests
test-visual-validation:
	dotnet test src/Nexo.Tests.GeospatialVisual/Nexo.Tests.GeospatialVisual.csproj --filter "FullyQualifiedName~GeospatialVisualValidationTests"

test-visual-validation-all:
	bash scripts/test-visual-validation-multi-env.sh --all

# Framework test coverage and stress testing
test-framework-coverage:
	bash scripts/test-framework-coverage.sh

test-framework-stress:
	bash scripts/test-framework-stress.sh

test-framework-all:
	bash scripts/test-framework-multi-env.sh --all

test-framework-env:
	bash scripts/test-framework-multi-env.sh --env $(ENV)

# CLI demos
demo-test:
	dotnet run --project src/Nexo.CLI -- demo test \
		--target "https://httpbin.org/html" \
		--goal "Verify page structure and content" \
		--depth quick

demo-dev:
	dotnet run --project src/Nexo.CLI -- demo dev \
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

# Run specific agent demos
demo-test-game:
	dotnet run --project src/Nexo.CLI -- demo test \
		--target "./examples/SampleGame/SampleGame.exe" \
		--goal "Play through tutorial, find bugs" \
		--persona adversarial \
		--depth thorough

demo-test-api:
	dotnet run --project src/Nexo.CLI -- demo test \
		--target "api://https://jsonplaceholder.typicode.com" \
		--goal "Test CRUD operations on /posts endpoint" \
		--depth standard
