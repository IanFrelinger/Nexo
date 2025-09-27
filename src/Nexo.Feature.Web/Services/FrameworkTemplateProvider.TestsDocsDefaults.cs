using Nexo.Feature.Web.Enums;

namespace Nexo.Feature.Web.Services
{
    public partial class FrameworkTemplateProvider
    {
        private string GetReactTestTemplate()
        {
            return @"import React from 'react';
import { render, screen } from '@testing-library/react';
import {{ComponentName}} from './{{ComponentName}}';

describe('{{ComponentName}}', () => {
  it('renders without crashing', () => {
    render(<{{ComponentName}} />);
    expect(screen.getByText('{{ComponentName}}')).toBeInTheDocument();
  });

  it('displays framework information', () => {
    render(<{{ComponentName}} />);
    expect(screen.getByText(/Generated with/)).toBeInTheDocument();
  });
});";
        }

        private string GetVueTestTemplate()
        {
            return @"import { mount } from '@vue/test-utils';
import {{ComponentName}} from './{{ComponentName}}.vue';

describe('{{ComponentName}}', () => {
  it('renders without crashing', () => {
    const wrapper = mount({{ComponentName}});
    expect(wrapper.find('h1').text()).toBe('{{ComponentName}}');
  });

  it('displays framework information', () => {
    const wrapper = mount({{ComponentName}});
    expect(wrapper.text()).toContain('Generated with');
  });
});";
        }

        private string GetReactDocumentationTemplate()
        {
            return @"# {{ComponentName}}

A React component generated with {{Framework}}.

## Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
|      |      |          |             |

## Usage

```tsx
import {{ComponentName}} from './{{ComponentName}}';

function App() {
  return <{{ComponentName}} />;
}
```

## Features

- Generated with {{Framework}}
- TypeScript support
- Optimized for performance
";
        }

        private string GetVueDocumentationTemplate()
        {
            return @"# {{ComponentName}}

A Vue component generated with {{Framework}}.

## Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
|      |      |          |             |

## Usage

```vue
<template>
  <{{ComponentName}} />
</template>

<script setup>
import {{ComponentName}} from './{{ComponentName}}.vue';
</script>
```

## Features

- Generated with {{Framework}}
- TypeScript support
- Composition API
- Optimized for performance
";
        }

        private string GetDefaultTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            return $@"// Default template for {framework} {componentType}
export default function {{ComponentName}}() {{
  return (
    <div>
      <h1>{{ComponentName}}</h1>
      <p>Generated with {framework}</p>
      {{SourceCode}}
    </div>
  );
}}";
        }

        private string GetDefaultTypeScriptTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            return $@"// Default TypeScript template for {framework} {componentType}
export interface {{ComponentName}}Props {{
  // Add your props here
}}";
        }

        private string GetDefaultStylingTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            return $@"/* Default styling template for {framework} {componentType} */
.{{ComponentName}}-container {{
  /* Add your styles here */
}}";
        }

        private string GetDefaultTestTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            return $@"// Default test template for {framework} {componentType}
describe('{{ComponentName}}', () => {{
  it('should render correctly', () => {{
    // Add your tests here
  }});
}});";
        }

        private string GetDefaultDocumentationTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            return $@"# {{ComponentName}}

A {componentType} component generated with {framework}.

## Usage

Add usage instructions here.

## Props

Add props documentation here.

## Features

- Generated with {framework}
- {componentType} component type
";
        }
    }
}

