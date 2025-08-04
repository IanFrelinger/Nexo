#!/bin/bash
set -e

# 1. Build for all targets
./run-dotnet.sh build -c Debug

# 2. Download xunit.runner.console if not present
if [ ! -d "tools/xunit.runner.console" ]; then
  mkdir -p tools
  ./run-dotnet.sh tool install xunit.runner.console --tool-path tools
fi

# 3. Find all test DLLs for net8.0 and net48
find tests -type f -name "*.Tests.dll" | while read -r dll; do
  if [[ "$dll" == *"net8.0"* ]]; then
    echo "=== Running $dll in .NET 8 Docker ==="
    ./run-dotnet.sh test "$dll" --no-build --logger "console;verbosity=normal"
  fi
  if [[ "$dll" == *"net48"* ]]; then
    echo "=== Running $dll in Mono Docker ==="
    ./run-mono.sh tools/xunit.runner.console/tools/net472/xunit.console.exe "$dll"
  fi
done 