# Nexo Tools

This directory contains various tools and utilities used in the Nexo project.

## Schema Validator

A command-line tool for validating YAML files against JSON schemas.

### Usage

```bash
dotnet run --project tools/schema-validator/schema-validator.csproj -- --input path/to/file.yaml --schema path/to/schema.json
```

### Options

- `-i, --input`: Input YAML file to validate (required)
- `-s, --schema`: JSON schema file to validate against (required)
- `-o, --output`: Output file for validation results (optional)
- `-v, --verbose`: Enable verbose output

### Example

```bash
# Validate a FeatureSpec file
dotnet run --project tools/schema-validator/schema-validator.csproj -- --input examples/sample-featurespec.yaml --schema policies/schemas/featurespec.schema.json --verbose
```

### Features

- Validates YAML files against JSON schemas
- Checks required fields
- Validates field types (string, array, object, number, boolean)
- Validates string patterns and length constraints
- Provides detailed error messages
- Supports verbose output for debugging

### Building

```bash
dotnet build tools/schema-validator/schema-validator.csproj
```

### Publishing

```bash
dotnet publish tools/schema-validator/schema-validator.csproj -c Release -o ./publish
```