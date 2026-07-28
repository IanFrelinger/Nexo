#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile

files = [
    Path("/home/icfre/cold-box-extractor/proprietary-demo/P16_NestedRelIncludes/core/NestedStream.hpp"),
    Path("/home/icfre/work/poco/common/core/NestedStream.hpp"),
    Path("/home/icfre/work/proprietary-demo/P16_NestedRelIncludes/core/NestedStream.hpp"),
]
for f in files:
    print("==", f)
    if not f.exists():
        print("  MISSING")
        continue
    text = f.read_text()
    print("  kFileHeaderBytes" if "kFileHeaderBytes" in text else "  NO kFileHeaderBytes")
    for line in text.splitlines():
        if "hdr[" in line or "kFileHeaderBytes" in line or "nfields" in line:
            print(" ", line.strip())

# Host compile count
demo = Path("/home/icfre/work/proprietary-demo/P16_NestedRelIncludes")
src = Path(tempfile.gettempdir()) / "count_nested.cpp"
src.write_text(
    f"""
#include "{demo}/core/NestedStream.hpp"
#include <iostream>
int main() {{
  NestedStream s("/home/icfre/work/samples/mock.evtq");
  long v; int n=0;
  while (s.pull(v)) n++;
  std::cout << n << std::endl;
  return 0;
}}
"""
)
cmd = [
    "g++", "-std=c++20",
    f"-I{demo}/core", f"-I{demo}",
    "-o", "/tmp/count_nested", str(src), str(demo / "core/Checksum.cpp"),
]
print("compile", cmd)
p = subprocess.run(cmd, text=True, capture_output=True)
print(p.stderr)
if p.returncode == 0:
    print(subprocess.run(["/tmp/count_nested"], text=True, capture_output=True).stdout)
else:
    print("compile failed", p.returncode)
