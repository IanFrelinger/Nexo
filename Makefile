.PHONY: demo demo-test demo-dev demo-fresh package-cli build test

build:
	dotnet build

test:
	dotnet test

# Run Universal Testing Agent demo
demo-test:
	dotnet run --project src/Nexo.CLI -- demo test "https://example.com" "Test the application"

# Run Autonomous Development Agent demo
demo-dev:
	dotnet run --project src/Nexo.CLI -- demo dev "Add a feature" "./MyProject"

# Build and run demo
demo-fresh: build demo-test

# Package CLI for distribution
package-cli:
	dotnet publish src/Nexo.CLI -c Release -r linux-x64 --self-contained -o ./artifacts/cli-linux
	dotnet publish src/Nexo.CLI -c Release -r win-x64 --self-contained -o ./artifacts/cli-win
	dotnet publish src/Nexo.CLI -c Release -r osx-x64 --self-contained -o ./artifacts/cli-osx
