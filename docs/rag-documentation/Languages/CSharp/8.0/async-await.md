# Async and Await in C# 8.0

## Overview
The async and await keywords in C# provide a way to write asynchronous code that looks like synchronous code. This pattern is essential for writing responsive applications that don't block the UI thread.

## Basic Syntax

```csharp
public async Task<string> GetDataAsync()
{
    var data = await SomeAsyncOperation();
    return data;
}
```

## Key Concepts

### Async Methods
- Must return `Task`, `Task<T>`, or `ValueTask<T>`
- Cannot return `void` (except for event handlers)
- Must be marked with the `async` keyword

### Await Expression
- Can only be used in async methods
- Suspends execution until the awaited operation completes
- Returns the result of the awaited operation

## Best Practices

### 1. Use ConfigureAwait(false) in Library Code
```csharp
public async Task<string> GetDataAsync()
{
    var data = await SomeAsyncOperation().ConfigureAwait(false);
    return data;
}
```

### 2. Don't Use async void
```csharp
// ❌ Bad
public async void BadMethod()
{
    await SomeAsyncOperation();
}

// ✅ Good
public async Task GoodMethod()
{
    await SomeAsyncOperation();
}

// ✅ Exception: Event handlers
public async void Button_Click(object sender, EventArgs e)
{
    await SomeAsyncOperation();
}
```

### 3. Use Task.Run for CPU-Bound Work
```csharp
public async Task<int> CalculateAsync(int value)
{
    return await Task.Run(() => ExpensiveCalculation(value));
}
```

## Common Patterns

### Fire and Forget
```csharp
public void StartBackgroundWork()
{
    _ = Task.Run(async () =>
    {
        await DoBackgroundWorkAsync();
    });
}
```

### Parallel Execution
```csharp
public async Task<(string, string)> GetDataAsync()
{
    var task1 = GetFirstDataAsync();
    var task2 = GetSecondDataAsync();
    
    await Task.WhenAll(task1, task2);
    
    return (task1.Result, task2.Result);
}
```

### Exception Handling
```csharp
public async Task<string> SafeAsyncOperation()
{
    try
    {
        return await RiskyAsyncOperation();
    }
    catch (HttpRequestException ex)
    {
        // Handle specific exception
        return "Error occurred";
    }
    catch (Exception ex)
    {
        // Handle general exception
        throw;
    }
}
```

## Performance Considerations

### Memory Allocation
- Async methods create state machines that allocate memory
- Use `ValueTask<T>` for high-frequency operations
- Consider object pooling for frequently called async methods

### Threading
- Async methods don't create threads
- They use the thread pool efficiently
- Avoid blocking async methods with `.Result` or `.Wait()`

## Common Pitfalls

### 1. Deadlocks
```csharp
// ❌ Can cause deadlock
public string GetData()
{
    return GetDataAsync().Result;
}

// ✅ Correct approach
public async Task<string> GetDataAsync()
{
    return await GetDataAsync();
}
```

### 2. Exception Swallowing
```csharp
// ❌ Exceptions are swallowed
public async void BadMethod()
{
    try
    {
        await SomeAsyncOperation();
    }
    catch (Exception ex)
    {
        // Exception is lost
    }
}
```

### 3. Synchronous Overhead
```csharp
// ❌ Unnecessary async overhead
public async Task<int> SimpleCalculation(int a, int b)
{
    return await Task.FromResult(a + b);
}

// ✅ Better approach
public Task<int> SimpleCalculation(int a, int b)
{
    return Task.FromResult(a + b);
}
```

## Examples

### Web API Controller
```csharp
[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly IDataService _dataService;

    public DataController(IDataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DataModel>> GetDataAsync(int id)
    {
        try
        {
            var data = await _dataService.GetDataAsync(id);
            if (data == null)
                return NotFound();
            
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error");
        }
    }
}
```

### Database Operations
```csharp
public class UserRepository
{
    private readonly DbContext _context;

    public async Task<User> GetUserAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .ToListAsync();
    }
}
```

## Related Topics
- Task Parallel Library (TPL)
- Cancellation tokens
- Progress reporting
- Async streams (IAsyncEnumerable)
- ConfigureAwait
- ValueTask vs Task
