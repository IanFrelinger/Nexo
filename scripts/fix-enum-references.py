#!/usr/bin/env python3
"""
Script to fix all enum references in the codebase
"""

import os
import re
import glob

def fix_enum_references(file_path):
    """Fix enum references in a single file"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    # Fix namespace references
    content = re.sub(r'using Nexo\.Core\.Application\.Enums;', 'using Nexo.Shared.Values;', content)
    content = re.sub(r'using Nexo\.Core\.Domain\.Enums;', 'using Nexo.Core.Domain.Values;', content)
    content = re.sub(r'using Nexo\.Shared\.Enums;', 'using Nexo.Shared.Values;', content)
    content = re.sub(r'using Nexo\.Infrastructure\.Enums;', 'using Nexo.Infrastructure.Values;', content)
    
    # Fix specific enum type references
    content = re.sub(r'ValidationSeverity', 'ValidationSeverity', content)
    content = re.sub(r'LogLevel', 'LogLevel', content)
    content = re.sub(r'PlatformType', 'PlatformType', content)
    
    # Fix ambiguous TaskStatus references
    content = re.sub(r'TaskStatus\.', 'Values.TaskStatus.', content)
    content = re.sub(r'\(int\)TaskStatus\.', '(int)Values.TaskStatus.', content)
    
    if content != original_content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed references in {file_path}")
        return True
    return False

def main():
    """Main function to process all C# files"""
    
    # Find all C# files
    cs_files = []
    for root, dirs, files in os.walk('src'):
        for file in files:
            if file.endswith('.cs'):
                cs_files.append(os.path.join(root, file))
    
    print(f"Found {len(cs_files)} C# files")
    
    fixed_count = 0
    for cs_file in cs_files:
        try:
            if fix_enum_references(cs_file):
                fixed_count += 1
        except Exception as e:
            print(f"Error processing {cs_file}: {e}")
    
    print(f"Fixed references in {fixed_count} files")

if __name__ == "__main__":
    main()
