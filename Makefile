UNITY_BIN ?= /Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity
PROJ      ?= $(PWD)/DirectorStudioUnity
ART       ?= $(PWD)

.PHONY: ci playmode smoke diag demo demo-offline demo-quick demo-fresh package-demo build

build:
	dotnet build

ci:
	UNITY_BIN=$(UNITY_BIN) PROJ=$(PROJ) ART=$(ART) ./scripts/ci-verify.sh

playmode:
	UNITY_BIN=$(UNITY_BIN) PROJ=$(PROJ) ART=$(ART) ./scripts/unity-playmode-run.sh

smoke:
	UNITY_BIN=$(UNITY_BIN) PROJ=$(PROJ) ART=$(ART) ./scripts/unity-smoke-fallback.sh

diag:
	UNITY_BIN=$(UNITY_BIN) PROJ=$(PROJ) ART=$(ART) ./scripts/unity-diag.sh

# Run the interactive demo
demo:
	dotnet run --project src/Nexo.Demo.Visual -- --interactive

# Run demo in offline mode
demo-offline:
	dotnet run --project src/Nexo.Demo.Visual -- --interactive --offline

# Run quick non-interactive demo
demo-quick:
	dotnet run --project src/Nexo.Demo.Visual

# Build and run demo
demo-fresh: build demo

# Package demo for distribution
package-demo:
	dotnet publish src/Nexo.Demo.Visual -c Release -r linux-x64 --self-contained -o ./artifacts/demo-linux
	dotnet publish src/Nexo.Demo.Visual -c Release -r win-x64 --self-contained -o ./artifacts/demo-win
	dotnet publish src/Nexo.Demo.Visual -c Release -r osx-x64 --self-contained -o ./artifacts/demo-osx
