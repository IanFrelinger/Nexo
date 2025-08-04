# Nexo.Feature.Web

A comprehensive web code generation system for React/Vue with WebAssembly performance optimization.

## Overview

The Web feature provides advanced code generation capabilities for modern web frameworks, with a focus on React and Vue.js. It includes WebAssembly optimization strategies for maximum performance and comprehensive template support for various component types.

## Features

### 🚀 Framework Support
- **React** - Functional components, class components, hooks, and more
- **Vue.js** - Composition API, Options API, composables
- **Next.js** - Pages, components, and SSR support
- **Nuxt.js** - Pages, components, and SSR support
- **Angular** - Components and services
- **Svelte** - Components and stores

### 🎯 Component Types
- **Functional Components** - Modern React hooks and Vue composition API
- **Class Components** - Traditional React class and Vue options API
- **Pure Components** - Optimized for performance with memoization
- **Higher-Order Components (HOC)** - Advanced component patterns
- **Custom Hooks/Composables** - Reusable logic
- **Context Providers** - State management components
- **Page Components** - Routing and layout components
- **Layout Components** - Structural components

### ⚡ WebAssembly Optimization
- **Tree Shaking** - Remove unused code
- **Code Splitting** - Dynamic imports for better caching
- **Minification** - Reduce bundle size
- **SIMD Instructions** - Vector operations for performance
- **Threading Support** - Web Workers for heavy computations
- **Memory Optimization** - Efficient memory allocation
- **Performance Analysis** - Detailed metrics and recommendations

### 📦 Bundle Analysis
- **Size Estimation** - Raw, minified, gzipped, and brotli sizes
- **Compression Ratios** - Optimization effectiveness metrics
- **Performance Metrics** - Complexity, memory efficiency, execution efficiency
- **Optimization Suggestions** - Actionable recommendations

## Architecture

### Core Components

#### `IWebCodeGenerator`
Main interface for code generation with support for:
- Multiple frameworks and component types
- WebAssembly optimization integration
- Request validation and error handling

#### `IWebAssemblyOptimizer`
Advanced optimization engine providing:
- Multiple optimization strategies
- Performance analysis
- Bundle size estimation
- Custom optimization flags

#### `IFrameworkTemplateProvider`
Template management system with:
- Framework-specific templates
- Component type variations
- TypeScript, CSS, test, and documentation templates
- Fallback and default templates

### Models

#### `WebCodeGenerationRequest`
Comprehensive request model including:
- Framework and component type selection
- Source code and target path
- Optimization strategy
- Output options (TypeScript, styling, tests, documentation)
- Custom WebAssembly settings

#### `WebCodeGenerationResult`
Detailed result model with:
- Generated code (component, types, styling, tests, docs)
- Performance metrics and bundle sizes
- WebAssembly optimization results
- Generated file paths and warnings

#### `WebAssemblyConfig`
Advanced configuration for optimization:
- Memory allocation settings
- Threading configuration
- SIMD instruction support
- Custom optimization flags

## Usage

### Basic Code Generation

```csharp
var request = new WebCodeGenerationRequest
{
    Framework = WebFrameworkType.React,
    ComponentType = WebComponentType.Functional,
    ComponentName = "UserProfile",
    TargetPath = "/src/components",
    Optimization = WebAssemblyOptimization.Balanced,
    IncludeTypeScript = true,
    IncludeStyling = true,
    IncludeTests = true,
    IncludeDocumentation = true
};

var useCase = new GenerateWebCodeUseCase(logger, codeGenerator, wasmOptimizer);
var result = await useCase.ExecuteAsync(request);
```

### Advanced WebAssembly Configuration

```csharp
var config = new WebAssemblyConfig
{
    Optimization = WebAssemblyOptimization.Aggressive,
    EnableTreeShaking = true,
    EnableCodeSplitting = true,
    EnableMinification = true,
    EnableSourceMaps = false,
    TargetBrowsers = new List<string> { "chrome >= 80", "firefox >= 78" },
    Memory = new WebAssemblyMemoryConfig
    {
        InitialPages = 256, // 16MB
        MaxPages = 2048,    // 128MB
        EnableSharedMemory = true
    },
    Threading = new WebAssemblyThreadingConfig
    {
        EnableThreading = true,
        MaxThreads = 4,
        UseWebWorkers = true
    },
    Simd = new WebAssemblySimdConfig
    {
        EnableSimd = true,
        InstructionSet = "wasm_simd128"
    }
};
```

### Performance Analysis

```csharp
var optimizer = new WebAssemblyOptimizer(logger);
var analysis = await optimizer.AnalyzePerformanceAsync(sourceCode);

foreach (var metric in analysis.PerformanceMetrics)
{
    Console.WriteLine($"{metric.Key}: {metric.Value}");
}

foreach (var recommendation in analysis.PerformanceRecommendations)
{
    Console.WriteLine($"Recommendation: {recommendation}");
}
```

## Templates

### React Functional Component
```tsx
import React, { useState, useEffect } from 'react';

interface {{ComponentName}}Props {
  // Add your props here
}

export default function {{ComponentName}}({ }: {{ComponentName}}Props) {
  const [state, setState] = useState<string>('');

  useEffect(() => {
    // Component initialization logic
  }, []);

  const handleClick = () => {
    // Handle click events
  };

  return (
    <div className="{{ComponentName}}-container">
      <h1>{{ComponentName}}</h1>
      <p>Generated with {{Framework}}</p>
      {{SourceCode}}
    </div>
  );
}
```

### Vue Composition API
```vue
<template>
  <div class="{{ComponentName}}-container">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}}</p>
    {{SourceCode}}
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';

// Props
interface Props {
  // Add your props here
}

const props = defineProps<Props>();

// Reactive state
const state = ref<string>('');

// Computed properties
const computedValue = computed(() => {
  return state.value.toUpperCase();
});

// Methods
const handleClick = () => {
  // Handle click events
};

// Lifecycle
onMounted(() => {
  // Component initialization logic
});
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>
```

## Optimization Strategies

### None
- No optimization applied
- Development mode with source maps
- Full debugging capabilities

### Basic
- Tree shaking enabled
- Basic minification
- Standard performance optimizations

### Aggressive
- Maximum optimization
- SIMD instructions
- Threading support
- Shared memory
- Advanced code splitting

### Size
- Focus on bundle size reduction
- Aggressive tree shaking
- Maximum minification
- Size-optimized algorithms

### Balanced
- Optimal balance of performance and size
- Moderate optimizations
- Good for production use

### Custom
- User-defined optimization flags
- Flexible configuration
- Tailored to specific requirements

## Performance Metrics

The system provides comprehensive performance analysis:

- **Complexity** - Cyclomatic complexity score (1-10)
- **Memory Efficiency** - Memory usage patterns (0-1)
- **Execution Efficiency** - Algorithm efficiency (0-1)
- **Bundle Efficiency** - Bundle size optimization (0-1)

## Bundle Analysis

Detailed bundle size information:

- **Raw Size** - Original uncompressed size
- **Minified Size** - After minification
- **Gzipped Size** - After gzip compression
- **Brotli Size** - After brotli compression
- **Compression Ratios** - Effectiveness metrics

## Testing

The feature includes comprehensive test coverage:

```bash
dotnet test tests/Nexo.Feature.Web.Tests/Nexo.Feature.Web.Tests.csproj
```

Tests cover:
- Enum validation
- Model initialization
- Code generation workflows
- WebAssembly optimization
- Template provider functionality
- Use case execution
- Error handling

## Dependencies

- **Microsoft.Extensions.DependencyInjection.Abstractions** - DI support
- **Microsoft.Extensions.Logging.Abstractions** - Logging
- **System.Text.Json** - JSON serialization
- **Microsoft.CodeAnalysis.CSharp** - Code analysis
- **Microsoft.CodeAnalysis.VisualBasic** - VB.NET support

## Integration

The Web feature integrates with the broader Nexo ecosystem:

- **Pipeline Architecture** - Command-based execution
- **Core Domain** - Shared domain models
- **Core Application** - Application services
- **Shared** - Common utilities and enums

## Future Enhancements

- **Additional Frameworks** - Support for more web frameworks
- **Advanced Templates** - More sophisticated component patterns
- **Real-time Optimization** - Live optimization during development
- **Bundle Visualization** - Interactive bundle analysis tools
- **Performance Monitoring** - Runtime performance tracking
- **Custom Template Engine** - User-defined template system

## Contributing

When contributing to the Web feature:

1. Follow the existing architecture patterns
2. Add comprehensive tests for new functionality
3. Update documentation for new features
4. Ensure all optimizations are configurable
5. Maintain backward compatibility
6. Follow the pipeline architecture principles

## License

This feature is part of the Nexo project and follows the same licensing terms. 