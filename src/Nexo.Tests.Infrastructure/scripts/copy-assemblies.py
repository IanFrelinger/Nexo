#!/usr/bin/env python3
"""Parse deps.json and copy all required assemblies to output directory."""
import json
import os
import sys
import shutil

def main():
    if len(sys.argv) < 3:
        print("Usage: copy-assemblies.py <deps.json> <output-dir> [nuget-packages-root]")
        sys.exit(1)
    
    deps_json_path = sys.argv[1]
    output_dir = sys.argv[2]
    nuget_root = sys.argv[3] if len(sys.argv) > 3 else os.path.expanduser("~/.nuget/packages")
    
    if not os.path.exists(deps_json_path):
        print(f"deps.json not found: {deps_json_path}")
        sys.exit(1)
    
    with open(deps_json_path, 'r') as f:
        deps = json.load(f)
    
    targets = deps.get('targets', {})
    runtime = targets.get('.NETCoreApp,Version=v8.0', {})
    
    copied = 0
    failed = 0
    
    for lib_name, lib_data in runtime.items():
        if '/' not in lib_name:
            continue
        
        runtime_paths = lib_data.get('runtime', {})
        for path, info in runtime_paths.items():
            if not path.endswith('.dll'):
                continue
            
            # Extract package name and version
            pkg_name, pkg_version = lib_name.split('/')
            pkg_name_lower = pkg_name.lower()
            
            # Build source path
            lib_path = path.replace('/', os.sep)
            source_path = os.path.join(nuget_root, pkg_name_lower, pkg_version, lib_path)
            
            # Build destination path
            dll_name = os.path.basename(path)
            dest_path = os.path.join(output_dir, dll_name)
            
            # Copy if source exists
            if os.path.exists(source_path):
                try:
                    shutil.copy2(source_path, dest_path)
                    copied += 1
                except Exception as e:
                    print(f"Failed to copy {source_path}: {e}", file=sys.stderr)
                    failed += 1
            else:
                # Don't fail on missing assemblies - some might be in different locations
                pass
    
    print(f"Copied {copied} assemblies, {failed} failures")
    return 0 if failed == 0 else 1

if __name__ == '__main__':
    sys.exit(main())
