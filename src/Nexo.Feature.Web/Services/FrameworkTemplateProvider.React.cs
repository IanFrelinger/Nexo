namespace Nexo.Feature.Web.Services
{
    public partial class FrameworkTemplateProvider
    {
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
    <div className="{{ComponentName}}-container">
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
      <div className="{{ComponentName}}-container">
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
    <div className="{{ComponentName}}-container">
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
    }
}

