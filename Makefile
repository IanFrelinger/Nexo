.PHONY: build test test-local demo-test demo-dev package-cli

# Build the solution
build:
	dotnet build

# Run tests locally
test:
	dotnet test

# Run tests on all platforms
test-all:
	nexo test --platforms ubuntu alpine debian android ios unity windows

# Run tests on specific platform
test-platform:
	nexo test --platforms $(PLATFORM)

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
