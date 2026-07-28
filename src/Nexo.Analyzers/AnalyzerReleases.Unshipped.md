; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NEXO0001 | Nexo.BrickContract | Warning | BrickInterfaceDriftAnalyzer: brick reads an input key its BrickInterface does not declare.
NEXO0002 | Nexo.BrickContract | Warning | BrickInterfaceDriftAnalyzer: brick writes an output key its BrickInterface does not declare.
