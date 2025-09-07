# Nexo Data Model Refactoring Plan

## Overview

The Nexo codebase has become opaque with redundant data models across multiple layers. This refactoring plan implements a **compositional approach** to data modeling that aligns with the pipeline architecture while reducing redundancy and improving maintainability.

## Current Issues Identified

### 1. Redundant Model Definitions
- **AI Models**: Complex, overlapping models in `DomainLogicModels.cs` (795 lines)
- **Pipeline Models**: Verbose configuration models with repetitive patterns
- **Domain Models**: Scattered across multiple features with inconsistent patterns
- **Value Objects**: Inconsistent implementation patterns across different types

### 2. Opaque Data Flow
- Models don't clearly express their relationships
- Validation logic scattered across multiple layers
- No clear composition patterns for complex data structures
- Inconsistent error handling and result patterns

### 3. Violation of Compositional Principles
- Models are tightly coupled to specific use cases
- No clear separation between data structure and behavior
- Validation rules embedded in models rather than composable
- No standardized way to combine and extend models

## Refactoring Strategy

### Phase 1: Core Compositional Foundation

#### 1.1 Create Base Compositional Interfaces
```csharp
// Core compositional interfaces
public interface IComposable<T>
{
    T Compose(params T[] components);
    bool CanComposeWith(T other);
    IEnumerable<T> Decompose();
}

public interface IValidatable
{
    ValidationResult Validate();
    IEnumerable<ValidationRule> GetValidationRules();
}

public interface IMetadataProvider
{
    IDictionary<string, object> GetMetadata();
    void SetMetadata(string key, object value);
}
```

#### 1.2 Implement Base Value Objects
```csharp
// Base value object with composition support
public abstract class ComposableValueObject<T> : IComposable<T>, IValidatable, IEquatable<T>
{
    protected readonly List<ValidationRule> _validationRules = new();
    protected readonly Dictionary<string, object> _metadata = new();
    
    public abstract T Compose(params T[] components);
    public abstract bool CanComposeWith(T other);
    public abstract IEnumerable<T> Decompose();
    
    public virtual ValidationResult Validate()
    {
        var result = new ValidationResult();
        foreach (var rule in _validationRules)
        {
            if (!rule.Validate(this))
            {
                result.AddError(rule.ErrorMessage);
            }
        }
        return result;
    }
    
    public virtual IEnumerable<ValidationRule> GetValidationRules() => _validationRules;
    public virtual IDictionary<string, object> GetMetadata() => _metadata;
    public virtual void SetMetadata(string key, object value) => _metadata[key] = value;
}
```

#### 1.3 Create Composable Entity Base
```csharp
// Base entity with composition support
public abstract class ComposableEntity<TId, TEntity> : IComposable<TEntity>, IValidatable, IMetadataProvider
    where TId : IEquatable<TId>
    where TEntity : ComposableEntity<TId, TEntity>
{
    public TId Id { get; protected set; }
    protected readonly List<ValidationRule> _validationRules = new();
    protected readonly Dictionary<string, object> _metadata = new();
    
    public abstract TEntity Compose(params TEntity[] components);
    public abstract bool CanComposeWith(TEntity other);
    public abstract IEnumerable<TEntity> Decompose();
    
    public virtual ValidationResult Validate()
    {
        var result = new ValidationResult();
        foreach (var rule in _validationRules)
        {
            if (!rule.Validate(this))
            {
                result.AddError(rule.ErrorMessage);
            }
        }
        return result;
    }
    
    public virtual IEnumerable<ValidationRule> GetValidationRules() => _validationRules;
    public virtual IDictionary<string, object> GetMetadata() => _metadata;
    public virtual void SetMetadata(string key, object value) => _metadata[key] = value;
}
```

### Phase 2: Refactor Value Objects

#### 2.1 Create Composable Identity Types
```csharp
// Composable identifier base
public abstract class ComposableId<T> : ComposableValueObject<ComposableId<T>>, IEquatable<ComposableId<T>>
{
    protected readonly Guid _value;
    
    protected ComposableId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ID cannot be empty", nameof(value));
        _value = value;
    }
    
    public static T New() where T : ComposableId<T>, new()
    {
        return new T { _value = Guid.NewGuid() };
    }
    
    public override ComposableId<T> Compose(params ComposableId<T>[] components)
    {
        // For IDs, composition might mean creating a composite key
        return this;
    }
    
    public override bool CanComposeWith(ComposableId<T> other) => true;
    public override IEnumerable<ComposableId<T>> Decompose() => new[] { this };
    
    public static implicit operator Guid(ComposableId<T> id) => id._value;
    public override string ToString() => _value.ToString();
    public bool Equals(ComposableId<T> other) => other?._value == _value;
    public override bool Equals(object obj) => Equals(obj as ComposableId<T>);
    public override int GetHashCode() => _value.GetHashCode();
}
```

#### 2.2 Refactor ProjectId
```csharp
public class ProjectId : ComposableId<ProjectId>
{
    public ProjectId(Guid value) : base(value) { }
    public static ProjectId New() => new ProjectId(Guid.NewGuid());
    public static ProjectId Parse(string value) => new ProjectId(Guid.Parse(value));
    public static bool TryParse(string value, out ProjectId projectId)
    {
        projectId = null;
        if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            projectId = new ProjectId(guid);
            return true;
        }
        return false;
    }
}
```

#### 2.3 Create Composable Name Types
```csharp
public abstract class ComposableName<T> : ComposableValueObject<ComposableName<T>>, IEquatable<ComposableName<T>>
{
    protected readonly string _value;
    protected readonly Regex _validationPattern;
    protected readonly int _minLength;
    protected readonly int _maxLength;
    
    protected ComposableName(string value, Regex pattern, int minLength, int maxLength)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _validationPattern = pattern;
        _minLength = minLength;
        _maxLength = maxLength;
        
        ValidateValue();
    }
    
    private void ValidateValue()
    {
        if (string.IsNullOrWhiteSpace(_value))
            throw new ArgumentException("Name cannot be empty", nameof(_value));
        if (_value.Length < _minLength || _value.Length > _maxLength)
            throw new ArgumentException($"Name must be between {_minLength} and {_maxLength} characters", nameof(_value));
        if (!_validationPattern.IsMatch(_value))
            throw new ArgumentException("Name format is invalid", nameof(_value));
    }
    
    public override ComposableName<T> Compose(params ComposableName<T>[] components)
    {
        // For names, composition might mean concatenation or prefixing
        return this;
    }
    
    public override bool CanComposeWith(ComposableName<T> other) => true;
    public override IEnumerable<ComposableName<T>> Decompose() => new[] { this };
    
    public static implicit operator string(ComposableName<T> name) => name._value;
    public override string ToString() => _value;
    public bool Equals(ComposableName<T> other) => other?._value == _value;
    public override bool Equals(object obj) => Equals(obj as ComposableName<T>);
    public override int GetHashCode() => _value.GetHashCode();
}
```

### Phase 3: Refactor Domain Entities

#### 3.1 Create Composable Project Entity
```csharp
public class Project : ComposableEntity<ProjectId, Project>
{
    private readonly List<Agent> _agents = new();
    private readonly List<ProjectComponent> _components = new();
    
    public ProjectName Name { get; private set; }
    public ProjectPath Path { get; private set; }
    public ContainerRuntime Runtime { get; private set; }
    public ProjectStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }
    
    public IReadOnlyList<Agent> Agents => _agents.AsReadOnly();
    public IReadOnlyList<ProjectComponent> Components => _components.AsReadOnly();
    
    public Project(ProjectName name, ProjectPath path, ContainerRuntime runtime)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        
        Id = ProjectId.New();
        Status = ProjectStatus.NotInitialized;
        CreatedAt = DateTimeOffset.UtcNow;
        
        // Add validation rules
        _validationRules.Add(new ValidationRule(
            "Project must have valid name and path",
            () => Name != null && Path != null,
            "Project validation failed"));
    }
    
    public override Project Compose(params Project[] components)
    {
        // Compose projects by merging their components and agents
        var composedProject = new Project(Name, Path, Runtime);
        
        foreach (var component in components)
        {
            foreach (var agent in component.Agents)
            {
                composedProject.AddAgent(agent);
            }
            foreach (var projectComponent in component.Components)
            {
                composedProject.AddComponent(projectComponent);
            }
        }
        
        return composedProject;
    }
    
    public override bool CanComposeWith(Project other) => other != null;
    public override IEnumerable<Project> Decompose() => new[] { this };
    
    public void AddAgent(Agent agent)
    {
        if (agent == null) throw new ArgumentNullException(nameof(agent));
        if (_agents.Any(a => a.Id == agent.Id))
            throw new InvalidOperationException($"Agent with ID '{agent.Id}' already exists in the project.");
        
        _agents.Add(agent);
        UpdateModifiedTime();
    }
    
    public void AddComponent(ProjectComponent component)
    {
        if (component == null) throw new ArgumentNullException(nameof(component));
        _components.Add(component);
        UpdateModifiedTime();
    }
    
    private void UpdateModifiedTime() => ModifiedAt = DateTimeOffset.UtcNow;
}
```

### Phase 4: Create Composable Pipeline Models

#### 4.1 Create Base Pipeline Components
```csharp
public abstract class ComposablePipelineComponent<T> : IComposable<T>, IValidatable, IMetadataProvider
{
    protected readonly string _id;
    protected readonly string _name;
    protected readonly string _description;
    protected readonly List<string> _tags = new();
    protected readonly Dictionary<string, object> _metadata = new();
    protected readonly List<ValidationRule> _validationRules = new();
    
    protected ComposablePipelineComponent(string id, string name, string description)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _description = description ?? throw new ArgumentNullException(nameof(description));
    }
    
    public string Id => _id;
    public string Name => _name;
    public string Description => _description;
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();
    
    public abstract T Compose(params T[] components);
    public abstract bool CanComposeWith(T other);
    public abstract IEnumerable<T> Decompose();
    
    public virtual ValidationResult Validate()
    {
        var result = new ValidationResult();
        foreach (var rule in _validationRules)
        {
            if (!rule.Validate(this))
            {
                result.AddError(rule.ErrorMessage);
            }
        }
        return result;
    }
    
    public virtual IEnumerable<ValidationRule> GetValidationRules() => _validationRules;
    public virtual IDictionary<string, object> GetMetadata() => _metadata;
    public virtual void SetMetadata(string key, object value) => _metadata[key] = value;
    
    public void AddTag(string tag) => _tags.Add(tag);
    public void RemoveTag(string tag) => _tags.Remove(tag);
}
```

#### 4.2 Create Composable Command
```csharp
public class ComposableCommand : ComposablePipelineComponent<ComposableCommand>
{
    private readonly string _category;
    private readonly CommandPriority _priority;
    private readonly List<string> _dependencies = new();
    private readonly bool _canExecuteInParallel;
    private readonly Func<IPipelineContext, Task<CommandResult>> _executeFunc;
    private readonly Func<IPipelineContext, Task<CommandValidationResult>> _validateFunc;
    
    public ComposableCommand(
        string id, 
        string name, 
        string description,
        string category,
        CommandPriority priority,
        bool canExecuteInParallel,
        Func<IPipelineContext, Task<CommandResult>> executeFunc,
        Func<IPipelineContext, Task<CommandValidationResult>> validateFunc = null)
        : base(id, name, description)
    {
        _category = category ?? throw new ArgumentNullException(nameof(category));
        _priority = priority;
        _canExecuteInParallel = canExecuteInParallel;
        _executeFunc = executeFunc ?? throw new ArgumentNullException(nameof(executeFunc));
        _validateFunc = validateFunc;
    }
    
    public string Category => _category;
    public CommandPriority Priority => _priority;
    public IReadOnlyList<string> Dependencies => _dependencies.AsReadOnly();
    public bool CanExecuteInParallel => _canExecuteInParallel;
    
    public override ComposableCommand Compose(params ComposableCommand[] components)
    {
        // Compose commands by creating a composite command
        var compositeId = $"composite_{Guid.NewGuid()}";
        var compositeName = $"Composite: {string.Join(" + ", components.Select(c => c.Name))}";
        var compositeDescription = $"Composite command combining: {string.Join(", ", components.Select(c => c.Name))}";
        
        return new ComposableCommand(
            compositeId,
            compositeName,
            compositeDescription,
            _category,
            components.Min(c => c.Priority),
            components.All(c => c.CanExecuteInParallel),
            async context =>
            {
                var results = new List<CommandResult>();
                foreach (var command in components)
                {
                    var result = await command.ExecuteAsync(context);
                    results.Add(result);
                    if (!result.IsSuccess)
                        return CommandResult.Failure($"Composite command failed: {result.ErrorMessage}");
                }
                return CommandResult.Success("Composite command completed successfully");
            });
    }
    
    public override bool CanComposeWith(ComposableCommand other) => other != null;
    public override IEnumerable<ComposableCommand> Decompose() => new[] { this };
    
    public async Task<CommandResult> ExecuteAsync(IPipelineContext context)
    {
        return await _executeFunc(context);
    }
    
    public async Task<CommandValidationResult> ValidateAsync(IPipelineContext context)
    {
        if (_validateFunc != null)
            return await _validateFunc(context);
        
        return CommandValidationResult.Success();
    }
    
    public void AddDependency(string dependency) => _dependencies.Add(dependency);
    public void RemoveDependency(string dependency) => _dependencies.Remove(dependency);
}
```

### Phase 5: Create Composable AI Models

#### 5.1 Create Base AI Model Components
```csharp
public abstract class ComposableAIModel<T> : IComposable<T>, IValidatable, IMetadataProvider
{
    protected readonly string _name;
    protected readonly string _description;
    protected readonly Dictionary<string, object> _metadata = new();
    protected readonly List<ValidationRule> _validationRules = new();
    protected readonly double _confidenceScore;
    
    protected ComposableAIModel(string name, string description, double confidenceScore = 0.0)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _description = description ?? throw new ArgumentNullException(nameof(description));
        _confidenceScore = Math.Clamp(confidenceScore, 0.0, 1.0);
    }
    
    public string Name => _name;
    public string Description => _description;
    public double ConfidenceScore => _confidenceScore;
    
    public abstract T Compose(params T[] components);
    public abstract bool CanComposeWith(T other);
    public abstract IEnumerable<T> Decompose();
    
    public virtual ValidationResult Validate()
    {
        var result = new ValidationResult();
        foreach (var rule in _validationRules)
        {
            if (!rule.Validate(this))
            {
                result.AddError(rule.ErrorMessage);
            }
        }
        return result;
    }
    
    public virtual IEnumerable<ValidationRule> GetValidationRules() => _validationRules;
    public virtual IDictionary<string, object> GetMetadata() => _metadata;
    public virtual void SetMetadata(string key, object value) => _metadata[key] = value;
}
```

#### 5.2 Create Composable Domain Entity
```csharp
public class ComposableDomainEntity : ComposableAIModel<ComposableDomainEntity>
{
    private readonly List<EntityProperty> _properties = new();
    private readonly List<EntityMethod> _methods = new();
    private readonly List<string> _dependencies = new();
    private readonly List<BusinessRule> _invariants = new();
    private readonly EntityType _type;
    private readonly bool _isAggregateRoot;
    private readonly string _generatedCode;
    
    public ComposableDomainEntity(
        string name,
        string description,
        EntityType type,
        bool isAggregateRoot = false,
        double confidenceScore = 0.0)
        : base(name, description, confidenceScore)
    {
        _type = type;
        _isAggregateRoot = isAggregateRoot;
    }
    
    public IReadOnlyList<EntityProperty> Properties => _properties.AsReadOnly();
    public IReadOnlyList<EntityMethod> Methods => _methods.AsReadOnly();
    public IReadOnlyList<string> Dependencies => _dependencies.AsReadOnly();
    public IReadOnlyList<BusinessRule> Invariants => _invariants.AsReadOnly();
    public EntityType Type => _type;
    public bool IsAggregateRoot => _isAggregateRoot;
    public string GeneratedCode => _generatedCode;
    
    public override ComposableDomainEntity Compose(params ComposableDomainEntity[] components)
    {
        var composedEntity = new ComposableDomainEntity(
            $"Composite_{string.Join("_", components.Select(c => c.Name))}",
            $"Composite entity combining: {string.Join(", ", components.Select(c => c.Name))}",
            EntityType.Composite,
            components.Any(c => c.IsAggregateRoot),
            components.Average(c => c.ConfidenceScore));
        
        // Merge properties, methods, dependencies, and invariants
        foreach (var component in components)
        {
            composedEntity._properties.AddRange(component.Properties);
            composedEntity._methods.AddRange(component.Methods);
            composedEntity._dependencies.AddRange(component.Dependencies);
            composedEntity._invariants.AddRange(component.Invariants);
        }
        
        return composedEntity;
    }
    
    public override bool CanComposeWith(ComposableDomainEntity other) => other != null;
    public override IEnumerable<ComposableDomainEntity> Decompose() => new[] { this };
    
    public void AddProperty(EntityProperty property) => _properties.Add(property);
    public void AddMethod(EntityMethod method) => _methods.Add(method);
    public void AddDependency(string dependency) => _dependencies.Add(dependency);
    public void AddInvariant(BusinessRule invariant) => _invariants.Add(invariant);
    public void SetGeneratedCode(string code) => _generatedCode = code;
}
```

### Phase 6: Create Validation Framework

#### 6.1 Create Composable Validation Rules
```csharp
public class ValidationRule : IComposable<ValidationRule>
{
    private readonly string _name;
    private readonly string _description;
    private readonly ValidationType _type;
    private readonly string _expression;
    private readonly string _errorMessage;
    private readonly ValidationSeverity _severity;
    private readonly Func<object, bool> _validationFunc;
    
    public ValidationRule(
        string name,
        string description,
        ValidationType type,
        string expression,
        string errorMessage,
        ValidationSeverity severity,
        Func<object, bool> validationFunc = null)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _description = description ?? throw new ArgumentNullException(nameof(description));
        _type = type;
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        _errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        _severity = severity;
        _validationFunc = validationFunc;
    }
    
    public string Name => _name;
    public string Description => _description;
    public ValidationType Type => _type;
    public string Expression => _expression;
    public string ErrorMessage => _errorMessage;
    public ValidationSeverity Severity => _severity;
    
    public bool Validate(object target)
    {
        if (_validationFunc != null)
            return _validationFunc(target);
        
        // Default validation logic based on expression
        return true; // Placeholder
    }
    
    public ValidationRule Compose(params ValidationRule[] components)
    {
        var compositeName = $"Composite_{string.Join("_", components.Select(c => c.Name))}";
        var compositeDescription = $"Composite rule combining: {string.Join(", ", components.Select(c => c.Name))}";
        var compositeExpression = string.Join(" AND ", components.Select(c => c.Expression));
        var compositeErrorMessage = string.Join("; ", components.Select(c => c.ErrorMessage));
        var compositeSeverity = components.Max(c => c.Severity);
        
        return new ValidationRule(
            compositeName,
            compositeDescription,
            ValidationType.Composite,
            compositeExpression,
            compositeErrorMessage,
            compositeSeverity,
            target => components.All(c => c.Validate(target)));
    }
    
    public bool CanComposeWith(ValidationRule other) => other != null;
    public IEnumerable<ValidationRule> Decompose() => new[] { this };
}
```

### Phase 7: Implementation Steps

#### Step 1: Create Base Compositional Infrastructure
1. Create `src/Nexo.Core.Domain/Composition/` directory
2. Implement base interfaces and abstract classes
3. Create validation framework
4. Add unit tests for compositional behavior

#### Step 2: Refactor Value Objects
1. Update existing value objects to inherit from compositional base
2. Implement composition logic for each value object type
3. Add validation rules to value objects
4. Update tests to verify compositional behavior

#### Step 3: Refactor Domain Entities
1. Update Project entity to use compositional approach
2. Create composable Agent entity
3. Implement composition logic for entities
4. Add validation rules and metadata support

#### Step 4: Refactor Pipeline Models
1. Create composable command, behavior, and aggregator models
2. Implement composition logic for pipeline components
3. Add validation and metadata support
4. Update pipeline execution engine to use new models

#### Step 5: Refactor AI Models
1. Create composable AI model base classes
2. Refactor existing AI models to use compositional approach
3. Implement composition logic for AI models
4. Add validation and confidence scoring

#### Step 6: Update Tests
1. Update existing tests to use new compositional models
2. Add tests for composition behavior
3. Add tests for validation rules
4. Add integration tests for composed models

#### Step 7: Update Documentation
1. Update architecture documentation
2. Add examples of model composition
3. Document validation framework usage
4. Update API documentation

## Benefits of This Approach

### 1. Reduced Redundancy
- Common patterns extracted into base classes
- Validation logic centralized and reusable
- Metadata handling standardized
- Consistent error handling patterns

### 2. Improved Composability
- Models can be combined in flexible ways
- Clear composition contracts
- Validation rules can be composed
- Metadata can be merged

### 3. Enhanced Maintainability
- Clear separation of concerns
- Consistent patterns across the codebase
- Easier to extend and modify
- Better testability

### 4. Better Alignment with Pipeline Architecture
- Models support pipeline composition patterns
- Clear integration with command/behavior/aggregator model
- Support for dynamic model composition
- Better resource management integration

## Migration Strategy

### Phase 1: Foundation (Week 1)
- Create base compositional infrastructure
- Implement validation framework
- Add comprehensive tests

### Phase 2: Value Objects (Week 2)
- Refactor existing value objects
- Update tests and documentation
- Verify backward compatibility

### Phase 3: Domain Entities (Week 3)
- Refactor Project and Agent entities
- Update related services
- Add composition examples

### Phase 4: Pipeline Models (Week 4)
- Refactor pipeline configuration models
- Update execution engine
- Add composition support

### Phase 5: AI Models (Week 5)
- Refactor AI domain models
- Update AI services
- Add confidence scoring

### Phase 6: Integration (Week 6)
- Update all dependent code
- Comprehensive testing
- Performance validation
- Documentation updates

## Success Criteria

1. **Reduced Code Duplication**: At least 40% reduction in redundant model code
2. **Improved Testability**: All models have comprehensive composition tests
3. **Enhanced Flexibility**: Models can be composed in at least 3 different ways
4. **Better Performance**: No performance regression in model operations
5. **Clearer Architecture**: Documentation clearly explains compositional patterns
6. **Backward Compatibility**: Existing code continues to work without changes

## Risk Mitigation

1. **Incremental Migration**: Each phase builds on the previous one
2. **Comprehensive Testing**: Extensive test coverage for all changes
3. **Backward Compatibility**: Maintain existing interfaces during transition
4. **Performance Monitoring**: Track performance impact throughout migration
5. **Documentation**: Clear documentation for all new patterns
6. **Code Reviews**: Thorough review process for all changes 