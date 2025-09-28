#!/usr/bin/env python3
"""
Script to automatically generate value object classes from existing enums
"""

import os
import re
import glob

def extract_enum_info(file_path):
    """Extract enum information from a C# enum file"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Extract namespace
    namespace_match = re.search(r'namespace\s+([^\s{]+)', content)
    namespace = namespace_match.group(1) if namespace_match else "Unknown"
    
    # Extract enum name
    enum_match = re.search(r'public\s+enum\s+(\w+)', content)
    enum_name = enum_match.group(1) if enum_match else "Unknown"
    
    # Extract enum values with comments
    values = []
    lines = content.split('\n')
    in_enum = False
    
    for line in lines:
        line = line.strip()
        if line.startswith('public enum'):
            in_enum = True
            continue
        elif in_enum and line.startswith('}'):
            break
        elif in_enum and line and not line.startswith('///'):
            # Extract enum value
            value_match = re.search(r'(\w+)(?:\s*=\s*(\d+))?', line)
            if value_match:
                name = value_match.group(1)
                value = value_match.group(2) if value_match.group(2) else str(len(values))
                values.append((name, value))
    
    return namespace, enum_name, values

def generate_value_object(namespace, enum_name, values, output_dir):
    """Generate a value object class from enum information"""
    
    # Convert namespace to values namespace
    if 'Nexo.Shared.Enums' in namespace:
        values_namespace = 'Nexo.Shared.Values'
    elif 'Nexo.Core.Domain.Enums' in namespace:
        values_namespace = 'Nexo.Core.Domain.Values'
    elif 'Nexo.Infrastructure.Enums' in namespace:
        values_namespace = 'Nexo.Infrastructure.Values'
    else:
        values_namespace = namespace.replace('.Enums', '.Values')
    
    # Generate the class content
    class_content = f'''using System;
using System.Collections.Generic;
using System.Linq;

namespace {values_namespace}
{{
    /// <summary>
    /// {enum_name} as a value object
    /// </summary>
    public sealed class {enum_name} : IEquatable<{enum_name}>
    {{
        public string Name {{ get; }}
        public string Description {{ get; }}
        public int Value {{ get; }}

        private {enum_name}(string name, string description, int value)
        {{
            Name = name;
            Description = description;
            Value = value;
        }}

        // Static instances for each {enum_name.lower()}
'''
    
    # Add static instances
    for i, (name, value) in enumerate(values):
        class_content += f'        public static readonly {enum_name} {name} = new("{name}", "Description for {name}", {value});\n'
    
    # Add collection
    class_content += f'''
        // Collection of all {enum_name.lower()}s
        public static readonly IReadOnlyList<{enum_name}> All = new[]
        {{
'''
    for name, _ in values:
        class_content += f'            {name},\n'
    
    class_content += '''        };

        // Factory methods
        public static ''' + enum_name + ''' FromName(string name) => 
            All.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? ''' + values[0][0] + ''';

        public static ''' + enum_name + ''' FromValue(int value) => 
            All.FirstOrDefault(v => v.Value == value) ?? ''' + values[0][0] + ''';

        // Equality and comparison
        public bool Equals(''' + enum_name + '''? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is ''' + enum_name + ''' other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(''' + enum_name + '''? left, ''' + enum_name + '''? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(''' + enum_name + '''? left, ''' + enum_name + '''? right) => 
            !(left == right);
    }
}'''
    
    # Write to file
    output_file = os.path.join(output_dir, f'{enum_name}.cs')
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(class_content)
    
    print(f"Generated {output_file}")

def main():
    """Main function to process all enum files"""
    
    # Find all enum files
    enum_files = []
    for root, dirs, files in os.walk('src'):
        for file in files:
            if file.endswith('.cs') and 'Enum' in file:
                enum_files.append(os.path.join(root, file))
    
    print(f"Found {len(enum_files)} enum files")
    
    # Create output directories
    os.makedirs('src/Nexo.Shared/Values', exist_ok=True)
    os.makedirs('src/Nexo.Core.Domain/Values', exist_ok=True)
    os.makedirs('src/Nexo.Infrastructure/Values', exist_ok=True)
    
    # Process each enum file
    for enum_file in enum_files:
        try:
            namespace, enum_name, values = extract_enum_info(enum_file)
            print(f"Processing {enum_file}: {enum_name} with {len(values)} values")
            
            # Determine output directory
            if 'Nexo.Shared' in namespace:
                output_dir = 'src/Nexo.Shared/Values'
            elif 'Nexo.Core.Domain' in namespace:
                output_dir = 'src/Nexo.Core.Domain/Values'
            elif 'Nexo.Infrastructure' in namespace:
                output_dir = 'src/Nexo.Infrastructure/Values'
            else:
                continue
            
            generate_value_object(namespace, enum_name, values, output_dir)
            
        except Exception as e:
            print(f"Error processing {enum_file}: {e}")

if __name__ == "__main__":
    main()
