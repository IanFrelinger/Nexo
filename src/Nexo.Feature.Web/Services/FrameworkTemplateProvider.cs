using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Enums;
using System.Text;

namespace Nexo.Feature.Web.Services
{
    /// <summary>
    /// Service for providing framework-specific code templates.
    /// </summary>
    public class FrameworkTemplateProvider : IFrameworkTemplateProvider
    {
        private readonly ILogger<FrameworkTemplateProvider> _logger;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _templates;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _typescriptTemplates;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _stylingTemplates;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _testTemplates;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _documentationTemplates;

        public FrameworkTemplateProvider(ILogger<FrameworkTemplateProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _templates = InitializeTemplates();
            _typescriptTemplates = InitializeTypeScriptTemplates();
            _stylingTemplates = InitializeStylingTemplates();
            _testTemplates = InitializeTestTemplates();
            _documentationTemplates = InitializeDocumentationTemplates();
        }

        public string GetTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_templates.TryGetValue(key, out var template))
            {
                return template;
            }

            _logger.LogWarning("Template not found for {Framework} {ComponentType}, using default", framework, componentType);
            return GetDefaultTemplate(framework, componentType);
        }

        public string GetTypeScriptTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_typescriptTemplates.TryGetValue(key, out var template))
            {
                return template;
            }

            return GetDefaultTypeScriptTemplate(framework, componentType);
        }

        public string GetStylingTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_stylingTemplates.TryGetValue(key, out var template))
            {
                return template;
            }

            return GetDefaultStylingTemplate(framework, componentType);
        }

        public string GetTestTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_testTemplates.TryGetValue(key, out var template))
            {
                return template;
            }

            return GetDefaultTestTemplate(framework, componentType);
        }

        public string GetDocumentationTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_documentationTemplates.TryGetValue(key, out var template))
            {
                return template;
            }

            return GetDefaultDocumentationTemplate(framework, componentType);
        }

        public bool TemplateExists(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            return _templates.ContainsKey(key);
        }

        public Dictionary<WebComponentType, bool> GetAvailableTemplates(WebFrameworkType framework)
        {
            var result = new Dictionary<WebComponentType, bool>();
            
            foreach (WebComponentType componentType in Enum.GetValues(typeof(WebComponentType)).Cast<WebComponentType>())
            {
                result[componentType] = TemplateExists(framework, componentType);
            }
            
            return result;
        }

        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // React Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactFunctionalTemplate();
            templates[(WebFrameworkType.React, WebComponentType.Class)] = GetReactClassTemplate();
            templates[(WebFrameworkType.React, WebComponentType.Pure)] = GetReactPureTemplate();
            templates[(WebFrameworkType.React, WebComponentType.Hook)] = GetReactHookTemplate();

            // Vue Templates
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueCompositionTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Class)] = GetVueOptionsTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Pure)] = GetVuePureTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Hook)] = GetVueComposableTemplate();

            // Next.js Templates
            templates[(WebFrameworkType.NextJs, WebComponentType.Page)] = GetNextJsPageTemplate();
            templates[(WebFrameworkType.NextJs, WebComponentType.Functional)] = GetNextJsFunctionalTemplate();

            // Nuxt.js Templates
            templates[(WebFrameworkType.NuxtJs, WebComponentType.Page)] = GetNuxtJsPageTemplate();
            templates[(WebFrameworkType.NuxtJs, WebComponentType.Functional)] = GetNuxtJsFunctionalTemplate();

            return templates;
        }

        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeTypeScriptTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // React TypeScript Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactTypeScriptTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueTypeScriptTemplate();

            return templates;
        }

        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeStylingTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // React CSS Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactCSSTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueSCSSTemplate();

            return templates;
        }

        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeTestTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // React Test Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactTestTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueTestTemplate();

            return templates;
        }

        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeDocumentationTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // Documentation Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactDocumentationTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueDocumentationTemplate();

            return templates;
        }

        // React Templates
        private string GetReactFunctionalTemplate()
        {
            return @"import React, { useState, useEffect } from 'react';

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
    <div className=""{{ComponentName}}-container"">
      <h1>{{ComponentName}}</h1>
      <p>Generated with {{Framework}}</p>
      {{SourceCode}}
    </div>
  );
}";
        }

        private string GetReactClassTemplate()
        {
            return @"import React, { Component } from 'react';

interface {{ComponentName}}Props {
  // Add your props here
}

interface {{ComponentName}}State {
  // Add your state here
}

export default class {{ComponentName}} extends Component<{{ComponentName}}Props, {{ComponentName}}State> {
  constructor(props: {{ComponentName}}Props) {
    super(props);
    this.state = {
      // Initialize state
    };
  }

  componentDidMount() {
    // Component initialization logic
  }

  handleClick = () => {
    // Handle click events
  };

  render() {
    return (
      <div className=""{{ComponentName}}-container"">
        <h1>{{ComponentName}}</h1>
        <p>Generated with {{Framework}}</p>
        {{SourceCode}}
      </div>
    );
  }
}";
        }

        private string GetReactPureTemplate()
        {
            return @"import React, { memo } from 'react';

interface {{ComponentName}}Props {
  // Add your props here
}

const {{ComponentName}} = memo<{{ComponentName}}Props>(({ }) => {
  return (
    <div className=""{{ComponentName}}-container"">
      <h1>{{ComponentName}}</h1>
      <p>Generated with {{Framework}} (Pure Component)</p>
      {{SourceCode}}
    </div>
  );
});

{{ComponentName}}.displayName = '{{ComponentName}}';

export default {{ComponentName}};";
        }

        private string GetReactHookTemplate()
        {
            return @"import { useState, useEffect, useCallback, useMemo } from 'react';

export function use{{ComponentName}}() {
  const [state, setState] = useState<string>('');

  const updateState = useCallback((newState: string) => {
    setState(newState);
  }, []);

  const computedValue = useMemo(() => {
    return state.toUpperCase();
  }, [state]);

  useEffect(() => {
    // Hook initialization logic
  }, []);

  return {
    state,
    updateState,
    computedValue
  };
}";
        }

        // Vue Templates
        private string GetVueCompositionTemplate()
        {
            return @"<template>
  <div class=""{{ComponentName}}-container"">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}}</p>
    {{SourceCode}}
  </div>
</template>

<script setup lang=""ts"">
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
</style>";
        }

        private string GetVueOptionsTemplate()
        {
            return @"<template>
  <div class=""{{ComponentName}}-container"">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}}</p>
    {{SourceCode}}
  </div>
</template>

<script lang=""ts"">
import { defineComponent } from 'vue';

export default defineComponent({
  name: '{{ComponentName}}',
  props: {
    // Add your props here
  },
  data() {
    return {
      state: ''
    };
  },
  computed: {
    computedValue(): string {
      return this.state.toUpperCase();
    }
  },
  methods: {
    handleClick() {
      // Handle click events
    }
  },
  mounted() {
    // Component initialization logic
  }
});
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>";
        }

        private string GetVuePureTemplate()
        {
            return @"<template>
  <div class=""{{ComponentName}}-container"">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}} (Pure Component)</p>
    {{SourceCode}}
  </div>
</template>

<script setup lang=""ts"">
import { defineProps } from 'vue';

// Props only - no internal state
interface Props {
  // Add your props here
}

defineProps<Props>();
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>";
        }

        private string GetVueComposableTemplate()
        {
            return @"import { ref, computed } from 'vue';

export function use{{ComponentName}}() {
  const state = ref<string>('');

  const computedValue = computed(() => {
    return state.value.toUpperCase();
  });

  const updateState = (newState: string) => {
    state.value = newState;
  };

  return {
    state,
    computedValue,
    updateState
  };
}";
        }

        // Next.js Templates
        private string GetNextJsPageTemplate()
        {
            return @"import { NextPage } from 'next';
import Head from 'next/head';

interface {{ComponentName}}Props {
  // Add your props here
}

const {{ComponentName}}: NextPage<{{ComponentName}}Props> = ({ }) => {
  return (
    <>
      <Head>
        <title>{{ComponentName}}</title>
        <meta name=""description"" content=""Generated with {{Framework}}"" />
      </Head>
      <div className=""{{ComponentName}}-container"">
        <h1>{{ComponentName}}</h1>
        <p>Generated with {{Framework}}</p>
        {{SourceCode}}
      </div>
    </>
  );
};

export default {{ComponentName}};";
        }

        private string GetNextJsFunctionalTemplate()
        {
            return @"import React from 'react';

interface {{ComponentName}}Props {
  // Add your props here
}

export default function {{ComponentName}}({ }: {{ComponentName}}Props) {
  return (
    <div className=""{{ComponentName}}-container"">
      <h1>{{ComponentName}}</h1>
      <p>Generated with {{Framework}}</p>
      {{SourceCode}}
    </div>
  );
}";
        }

        // Nuxt.js Templates
        private string GetNuxtJsPageTemplate()
        {
            return @"<template>
  <div class=""{{ComponentName}}-container"">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}}</p>
    {{SourceCode}}
  </div>
</template>

<script setup lang=""ts"">
definePageMeta({
  title: '{{ComponentName}}',
  description: 'Generated with {{Framework}}'
});

// Page logic here
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>";
        }

        private string GetNuxtJsFunctionalTemplate()
        {
            return @"<template>
  <div class=""{{ComponentName}}-container"">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}}</p>
    {{SourceCode}}
  </div>
</template>

<script setup lang=""ts"">
// Component logic here
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>";
        }

        // TypeScript Templates
        private string GetReactTypeScriptTemplate()
        {
            return @"export interface {{ComponentName}}Props {
  // Add your props here
}

export interface {{ComponentName}}State {
  // Add your state here
}

export type {{ComponentName}}Ref = React.RefObject<HTMLDivElement>;";
        }

        private string GetVueTypeScriptTemplate()
        {
            return @"export interface {{ComponentName}}Props {
  // Add your props here
}

export interface {{ComponentName}}Emits {
  // Add your emits here
}";
        }

        // Styling Templates
        private string GetReactCSSTemplate()
        {
            return @".{{ComponentName}}-container {
  padding: 1rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  background-color: #f9f9f9;
}

.{{ComponentName}}-container h1 {
  color: #333;
  margin-bottom: 0.5rem;
}

.{{ComponentName}}-container p {
  color: #666;
  font-size: 0.9rem;
}";
        }

        private string GetVueSCSSTemplate()
        {
            return @".{{ComponentName}}-container {
  padding: 1rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  background-color: #f9f9f9;

  h1 {
    color: #333;
    margin-bottom: 0.5rem;
  }

  p {
    color: #666;
    font-size: 0.9rem;
  }
}";
        }

        // Test Templates
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

        // Documentation Templates
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

        // Default Templates
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