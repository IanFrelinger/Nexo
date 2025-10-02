UNITY_BIN ?= /Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity
PROJ      ?= $(PWD)/DirectorStudioUnity
ART       ?= $(PWD)

.PHONY: ci playmode smoke diag
ci:
	UNITY_BIN=$(UNITY_BIN) PROJ=$(PROJ) ART=$(ART) ./scripts/ci-verify.sh

playmode:
	UNITY_BIN=$(UNITY_BIN) PROJ=$(PROJ) ART=$(ART) ./scripts/unity-playmode-run.sh

smoke:
	UNITY_BIN=$(UNITY_BIN) PROJ=$(PROJ) ART=$(ART) ./scripts/unity-smoke-fallback.sh

diag:
	UNITY_BIN=$(UNITY_BIN) PROJ=$(PROJ) ART=$(ART) ./scripts/unity-diag.sh
